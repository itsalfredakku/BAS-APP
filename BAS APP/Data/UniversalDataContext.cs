using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Linq;
using System.Data.OleDb;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;
using PropertyAttributes = System.Reflection.PropertyAttributes;

namespace BAS_APP.Data.Access
{
    public class UniversalDataContext
    {
        private readonly string connectionString;
        private readonly Dictionary<string, Type> tableTypes;

        public UniversalDataContext(string connectionString)
        {
            this.connectionString = connectionString;
            this.tableTypes = new Dictionary<string, Type>();
            FetchTableTypes();
        }

        private void FetchTableTypes()
        {
            using (var connection = new OleDbConnection(connectionString))
            {
                connection.Open();
                DataTable schema = connection.GetSchema("Tables");
                DataContext dataContext = new DataContext(connection);

                foreach (DataRow row in schema.Rows)
                {
                    string tableName = row["TABLE_NAME"].ToString();
                    Type entityType = CreateTableType(tableName, dataContext);
                    tableTypes[tableName] = entityType;
                }
            }
        }


        private Type CreateTableType(string tableName, DataContext dataContext)
        {
            // Define a dynamic entity type with table columns as properties
            TypeBuilder typeBuilder = GetTypeBuilder(tableName);
            PropertyInfo[] properties = GetTableColumns(tableName);

            foreach (PropertyInfo property in properties)
            {
                // Add properties to the dynamic type
                PropertyBuilder propertyBuilder = typeBuilder.DefineProperty(
                    property.Name,
                    PropertyAttributes.None,
                    property.PropertyType,
                    null
                );
            }

            Type entityType = typeBuilder.CreateType();
            dataContext.Mapping.GetTable(entityType);
            return entityType;
        }


        private TypeBuilder GetTypeBuilder(string tableName)
        {
            string typeName = $"{tableName}Entity";
            AssemblyName assemblyName = new AssemblyName(typeName);
            AssemblyBuilder assemblyBuilder = AssemblyBuilder.DefineDynamicAssembly(assemblyName, AssemblyBuilderAccess.Run);
            ModuleBuilder moduleBuilder = assemblyBuilder.DefineDynamicModule("MainModule");
            TypeBuilder typeBuilder = moduleBuilder.DefineType(typeName, TypeAttributes.Public);

            return typeBuilder;
        }

        private PropertyInfo[] GetTableColumns(string tableName)
        {
            using (var connection = new OleDbConnection(connectionString))
            {
                connection.Open();
                DataTable schema = connection.GetSchema("Columns", new[] { null, null, tableName });

                var properties = schema.AsEnumerable()
                    .Select(row => new
                    {
                        ColumnName = row.Field<string>("COLUMN_NAME"),
                        DataTypeName = row["DATA_TYPE"].ToString()
                    })
                    .Select(column => new
                    {
                        PropertyName = column.ColumnName,
                        PropertyType = GetPropertyType(column.DataTypeName)
                    })
                    .Select(prop => CreateProperty(prop.PropertyName, prop.PropertyType))
                    .ToArray();

                return properties;
            }
        }

        private Type GetPropertyType(string dataTypeName)
        {
            // Map the data type name to the corresponding .NET type
            switch (dataTypeName.ToLower())
            {
                case "int":
                case "integer":
                    return typeof(int);
                case "decimal":
                case "numeric":
                    return typeof(decimal);
                case "datetime":
                    return typeof(DateTime);
                case "string":
                case "text":
                    return typeof(string);
                // Handle other data types as needed
                default:
                    return typeof(object);
            }
        }

        private PropertyInfo CreateProperty(string propertyName, Type propertyType)
        {
            TypeBuilder typeBuilder = GetTypeBuilder("TempTable");
            FieldBuilder fieldBuilder = typeBuilder.DefineField($"_{propertyName}", propertyType, FieldAttributes.Private);

            PropertyBuilder propertyBuilder = typeBuilder.DefineProperty(
                propertyName,
                PropertyAttributes.None,
                propertyType,
                null
            );

            MethodBuilder getMethodBuilder = typeBuilder.DefineMethod(
                $"get_{propertyName}",
                MethodAttributes.Public | MethodAttributes.SpecialName | MethodAttributes.HideBySig,
                propertyType,
                Type.EmptyTypes
            );

            ILGenerator getIL = getMethodBuilder.GetILGenerator();
            getIL.Emit(OpCodes.Ldarg_0);
            getIL.Emit(OpCodes.Ldfld, fieldBuilder);
            getIL.Emit(OpCodes.Ret);

            MethodBuilder setMethodBuilder = typeBuilder.DefineMethod(
                $"set_{propertyName}",
                MethodAttributes.Public | MethodAttributes.SpecialName | MethodAttributes.HideBySig,
                null,
                new[] { propertyType }
            );

            ILGenerator setIL = setMethodBuilder.GetILGenerator();
            setIL.Emit(OpCodes.Ldarg_0);
            setIL.Emit(OpCodes.Ldarg_1);
            setIL.Emit(OpCodes.Stfld, fieldBuilder);
            setIL.Emit(OpCodes.Ret);

            propertyBuilder.SetGetMethod(getMethodBuilder);
            propertyBuilder.SetSetMethod(setMethodBuilder);

            ConstructorInfo ctor = typeof(CompilerGeneratedAttribute).GetConstructor(Type.EmptyTypes);
            CustomAttributeBuilder attributeBuilder = new CustomAttributeBuilder(ctor, new object[] { });
            propertyBuilder.SetCustomAttribute(attributeBuilder);

            return propertyBuilder;
        }

        public IQueryable<object> GetTable(string tableName)
        {
            if (tableTypes.ContainsKey(tableName))
            {
                Type entityType = tableTypes[tableName];
                using (var connection = new OleDbConnection(connectionString))
                {
                    connection.Open();
                    DataContext dataContext = new DataContext(connection);

                    // Ensure the entityType is mapped as a table in the DataContext
                    if (!dataContext.Mapping.GetTables().Any(t => t.RowType.Type == entityType))
                    {
                        throw new InvalidOperationException($"The type '{entityType.Name}' is not mapped as a table.");
                    }

                    MethodInfo getTableMethod = typeof(DataContext)
                        .GetMethod("GetTable")
                        .MakeGenericMethod(entityType);

                    IQueryable<object> table = (IQueryable<object>)getTableMethod.Invoke(dataContext, null);

                    return table;
                }
            }
            else
            {
                throw new ArgumentException($"Table '{tableName}' does not exist.");
            }
        }
    }
}
