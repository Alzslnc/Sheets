using System;
using System.Collections.Generic;
using System.Windows.Forms;

namespace Sheets
{
    public partial class SheetsCreateForm : Form
    {
        private readonly IniData IniData;
        public SheetsCreateForm(IniData data)
        {
            InitializeComponent();
            IniData = data;
            SetStartData();          
        }
        private void SetStartData()
        {
            #region viewports
            List<string> strings = new List<string>();
            foreach (ViewportLayer v in IniData.ViewportLayersClass.ViewportLayers)
            {
                if (!strings.Contains(v.Name)) strings.Add(v.Name);
            }
            Combo_Layers.Items.AddRange(strings.ToArray());
            if (Combo_Layers.Items.Count > 0) Combo_Layers.SelectedIndex = 0;
            
            SetViewportNumber();
            #endregion

            #region settings
            TextBox_Scale.Text = Settings.Default.Scale.ToString();
            TextBox_TextHeight.Text = Settings.Default.TextHeight.ToString();
            TextBox_Prefix.Text = Settings.Default.Prefix.ToString();
            TextBox_BlockScale.Text = Settings.Default.BlockScale.ToString();
            Check_NoBlock.Checked = Settings.Default.NoBlock;
            Check_ScaleExist.Checked = Settings.Default.ScaleExist;
            Check_SelfNumberColor.Checked = Settings.Default.SelfNumberColor;

            if (Settings.Default.RadioAttribute)
            {
                if (Check_NoBlock.Checked) Radio_Manual.Checked = true;
                else Radio_Attribute.Checked = true;
            }
            else if (Settings.Default.RadioLayer) Radio_Layer.Checked = true;
            else if (Settings.Default.RadioList) Radio_List.Checked = true;
            else if (Settings.Default.RadioManual) Radio_Manual.Checked = true;
            else if (Settings.Default.RadioNone) Radio_None.Checked = true;
            #endregion

            #region attributes
            if (IniData.BlockReferenceDataClass != null)
            {
                List<string> blocks = new List<string>();
                foreach (BlockReferenceData data in IniData.BlockReferenceDataClass.BlockReferenceDatas)
                {
                    blocks.Add(data.Name);
                }
                Combo_BlockReference.Items.AddRange(blocks.ToArray());
                if (Combo_BlockReference.Items.Count > 0)
                {
                    if (Combo_BlockReference.Items.Contains(Settings.Default.LastBlock)) Combo_BlockReference.SelectedItem = Settings.Default.LastBlock;
                    SetTags();                    
                }                                
            }
            #endregion

            CheckRadio();
        }
        private void SetViewportNumber()
        { 
            ViewportLayer v = IniData.ViewportLayersClass.GetViewportLayer(Combo_Layers.Text);
            if (v != null)
            {
                Label_Number.Text = v.Ids.Count.ToString();
            }
        }
        private void SetTags()
        {
            BlockReferenceData data = IniData.BlockReferenceDataClass.GetBlockReferenceData(Combo_BlockReference.Text);
            if (data != null) Combo_AttributeTag.Items.AddRange(data.Tags.ToArray());
            if (Combo_AttributeTag.Items.Contains(Settings.Default.LastTag)) Combo_AttributeTag.SelectedItem = Settings.Default.LastTag;
            else if (Combo_AttributeTag.Items.Count > 0) Combo_AttributeTag.SelectedIndex = 0;
        }
        private void CheckRadio()
        {
            if (Check_NoBlock.Checked)
            { 
                Radio_Attribute.Enabled = false;
                Check_UsePrefix.Enabled = false;
                Label_Block.Enabled = false;
                Label_Attribute.Enabled = false;
                Combo_BlockReference.Enabled = false;
                Combo_AttributeTag.Enabled = false;            
            } 
        }

        #region clicks       
        private void Combo_BlockReference_SelectedIndexChanged(object sender, EventArgs e)
        {
            Settings.Default.LastBlock = Combo_BlockReference.Text;
            Settings.Default.Save();
            SetTags();
        }
        private void Combo_AttributeTag_SelectedIndexChanged(object sender, EventArgs e)
        {
            Settings.Default.LastTag = Combo_AttributeTag.Text;
            Settings.Default.Save();
        }
        private void Button_Ok_Click(object sender, EventArgs e)
        {
            if (double.TryParse(TextBox_Scale.Text, out double result))
            {
                if (result <= 0)
                {
                    MessageBox.Show("масштаб должен быть больше нуля");
                    return;
                }
            }
            else
            {
                MessageBox.Show("введен некорректный масштаб");
                return;
            }            
            if (double.TryParse(TextBox_TextHeight.Text, out double result2))
            {
                if (result2 <= 0)
                {
                    MessageBox.Show("высота текста должена быть больше нуля");
                    return;
                }
            }
            else
            {
                MessageBox.Show("введена некорректная высота текста");
                return;
            }
            if (double.TryParse(TextBox_BlockScale.Text, out double result3))
            {
                if (result2 <= 0)
                {
                    MessageBox.Show("масштаб блока должен быть больше нуля");
                    return;
                }
            }
            else
            {
                MessageBox.Show("введен некорректный масштаб блока");
                return;
            }

            Settings.Default.Scale = result;
            Settings.Default.TextHeight = result2;
            Settings.Default.BlockScale = result3;
            Settings.Default.Prefix = TextBox_Prefix.Text;
            Settings.Default.ScaleExist = Check_ScaleExist.Checked;

            IniData.Layer = Combo_Layers.Text;
            if (IniData.BlockReferenceDataClass != null)
            {
                IniData.Block = Combo_BlockReference.Text;
                IniData.AttributeTag = Combo_AttributeTag.Text;            
            }

            if (Radio_Attribute.Checked)
            {
                IniData.PrefixType = PrefixType.Attribute;
                Settings.Default.RadioAttribute = Radio_Attribute.Checked;
                Settings.Default.RadioLayer = !Radio_Attribute.Checked;
                Settings.Default.RadioManual = !Radio_Attribute.Checked;
                Settings.Default.RadioList = !Radio_Attribute.Checked;
                Settings.Default.RadioNone = !Radio_Attribute.Checked;
            }
            else if (Radio_Layer.Checked)
            {
                IniData.PrefixType = PrefixType.Layer;
                Settings.Default.RadioLayer = Radio_Layer.Checked;
                Settings.Default.RadioAttribute = !Radio_Layer.Checked;          
                Settings.Default.RadioManual = !Radio_Layer.Checked;
                Settings.Default.RadioList = !Radio_Layer.Checked;
                Settings.Default.RadioNone = !Radio_Layer.Checked;
            }
            else if (Radio_List.Checked)
            {
                IniData.PrefixType = PrefixType.List;
                Settings.Default.RadioList = Radio_List.Checked;
                Settings.Default.RadioLayer = !Radio_List.Checked;
                Settings.Default.RadioAttribute = !Radio_List.Checked;
                Settings.Default.RadioManual = !Radio_List.Checked;            
                Settings.Default.RadioNone = !Radio_List.Checked;
            }
            else if (Radio_None.Checked)
            {
                IniData.PrefixType = PrefixType.none;
                Settings.Default.RadioNone = Radio_None.Checked;
                Settings.Default.RadioList = !Radio_None.Checked;
                Settings.Default.RadioLayer = !Radio_None.Checked;
                Settings.Default.RadioAttribute = !Radio_None.Checked;
                Settings.Default.RadioManual = !Radio_None.Checked;         
            }
            else if (Radio_Manual.Checked)
            {
                IniData.PrefixType = PrefixType.Manual;
                Settings.Default.RadioManual = Radio_Manual.Checked;            
                Settings.Default.RadioNone = !Radio_Manual.Checked;
                Settings.Default.RadioList = !Radio_Manual.Checked;
                Settings.Default.RadioLayer = !Radio_Manual.Checked;
                Settings.Default.RadioAttribute = !Radio_Manual.Checked;               
            }

            Settings.Default.NoBlock = Check_NoBlock.Checked;
            Settings.Default.SelfNumberColor = Check_SelfNumberColor.Checked;
            Settings.Default.Save();
            DialogResult = DialogResult.OK;
            Close();
        }
        private void Button_Cancel_Click(object sender, EventArgs e)
        {
            DialogResult= DialogResult.Cancel; 
            Close();
        }
        private void Combo_Layers_SelectedIndexChanged(object sender, EventArgs e)
        {
            SetViewportNumber();
        }
        private void Radio_List_CheckedChanged(object sender, EventArgs e)
        {

        }
        private void Radio_Layer_CheckedChanged(object sender, EventArgs e)
        {

        }
        private void Radio_Attribute_CheckedChanged(object sender, EventArgs e)
        {

        }
        private void Radio_Manual_CheckedChanged(object sender, EventArgs e)
        {

        }
        private void Radio_None_CheckedChanged(object sender, EventArgs e)
        {

        }
        #endregion

      
    }
}
