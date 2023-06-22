using BAS_APP.Data;
using BAS_APP.Data.Access;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace BAS_APP.Forms
{
    public partial class FrmApp : Form
    {
        UniversalDataContext universalDataContext;
        public FrmApp()
        {
            InitializeComponent();
        }

        private void FrmApp_Load(object sender, EventArgs e)
        {
            universalDataContext = new UniversalDataContext(PrepareConnectionString("C:\\Users\\aksbj\\source\\repos\\BAS APP\\BAS APP\\Data\\BASDB.accdb"));
            IQueryable<object> statusTable = universalDataContext.GetTable("Status");

            foreach (var elem in statusTable)
            {
                // Perform your action with each element here
                // For example, you can access properties of the element dynamically using reflection
                PropertyInfo[] properties = elem.GetType().GetProperties();
                foreach (var property in properties)
                {
                    var value = property.GetValue(elem);
                    Console.WriteLine($"{property.Name}: {value}");
                }
            }
        }
        private string PrepareConnectionString(string accdbPath)
        {
            return $"Provider=Microsoft.ACE.OLEDB.12.0;Data Source={accdbPath};Persist Security Info=True";
        }
    }
}
