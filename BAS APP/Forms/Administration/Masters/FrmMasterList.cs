using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace BAS_APP.Forms.Administration.Masters
{
    public partial class FrmMasterList : Form
    {
        public FrmMasterList()
        {
            InitializeComponent();
        }

        private void FrmMasterList_Load(object sender, EventArgs e)
        {
            // TODO: This line of code loads data into the 'bASDBDataSet.Master' table. You can move, or remove it, as needed.
            this.masterTableAdapter.Fill(this.bASDBDataSet.Master);
            // TODO: This line of code loads data into the 'bASDBDataSet.Master' table. You can move, or remove it, as needed.
            this.masterTableAdapter.Fill(this.bASDBDataSet.Master);

        }
    }
}
