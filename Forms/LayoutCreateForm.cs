using System;
using System.Threading;
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

            this.StartPosition = FormStartPosition.CenterScreen;

            TextBox_Overlap.KeyPress += new KeyPressEventHandler(BaseFunction.BaseFormClass.TbKeyInteger);

            TextBox_Overlap.Text = Settings.Default.LC_Overlap.ToString();

            TextBox_NewLayoutName.Text = Settings.Default.LC_LayoutName;

            Combo_Layouts.Items.AddRange(ActionClass.LayoutNames.ToArray());
            if (Combo_Layouts.Items.Count > 0) Combo_Layouts.SelectedIndex = 0;

            Check_LOCreate.Checked = Settings.Default.LC_LOCreate;
            Check_OnLine.Checked = Settings.Default.LC_OnLine;
        }

        private void Button_Ok_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrEmpty(TextBox_NewLayoutName.Text.Trim()))
            {
                MessageBox.Show("Не выбрано название новых листов");
                return;
            }

            ActionClass.LNCName = TextBox_NewLayoutName.Text.Trim();

            ActionClass.Action = Action.NLC;

            while (ActionClass.Action == Action.NLC) Thread.Sleep(100);

            if (!ActionClass.LNC)
            {
                MessageBox.Show("Введено некорректное название новых листов");
                return;
            }

            Settings.Default.LC_LayoutName = TextBox_NewLayoutName.Text.Trim();

            if (double.TryParse(TextBox_Overlap.Text, out double result))
            {
                if (result > 99 || result < 0)
                {
                    MessageBox.Show("Зона перекрытия должна быть в интервале 0 - 99 %");
                    return;
                }
            }
            else
            {
                MessageBox.Show("Введена некорректная зона перекрытия");
                return;
            }

            Settings.Default.LC_Overlap = result;
            Settings.Default.LC_OnLine = Check_OnLine.Checked;
            Settings.Default.LC_LOCreate = Check_LOCreate.Checked;
            Settings.Default.Save();

            ActionClass.Result = Combo_Layouts.Text;
            DialogResult = DialogResult.OK;
            Close();
        }

        private void Button_Cancel_Click(object sender, EventArgs e)
        {
            Close();
        }
    }
}
