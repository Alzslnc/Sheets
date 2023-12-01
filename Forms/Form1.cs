using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Sheets
{
    public partial class Form1 : Form
    {
        private IniData IniData;

        public Form1(IniData data)
        {
            InitializeComponent();
            IniData = data;
            Combo_Layers.Items.AddRange(data.Layers.ToArray());
            if (Combo_Layers.Items.Count > 0) Combo_Layers.SelectedIndex = 0;
            else
            { 
                DialogResult = DialogResult.Cancel;
                Close();
            }
        }

        private void Button_Ok_Click(object sender, EventArgs e)
        {
            IniData.Layer = Combo_Layers.Text;
            DialogResult = DialogResult.OK;
            Close();
        }

        private void Button_Cancel_Click(object sender, EventArgs e)
        {
            DialogResult= DialogResult.Cancel; 
            Close();
        }
    }
}
