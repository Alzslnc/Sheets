using System;
using System.Windows.Forms;

namespace Sheets
{
    public partial class LayoutCreateForm : Form
    {
        readonly ActionClass ActionClass;

        public LayoutCreateForm(ActionClass actionClass)
        {
            InitializeComponent();            

            ActionClass = actionClass;

            Combo_Layouts.Items.AddRange(ActionClass.LayoutNames.ToArray());
            if (Combo_Layouts.Items.Count > 0) Combo_Layouts.SelectedIndex = 0;
        }

        private void Button_Ok_Click(object sender, EventArgs e)
        {
            ActionClass.Result = Combo_Layouts.Text;
            DialogResult = DialogResult.OK;
            Close();
        }
    }
}
