namespace Sheets
{
    partial class SheetsCreateForm
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.Combo_Layers = new System.Windows.Forms.ComboBox();
            this.Button_Ok = new System.Windows.Forms.Button();
            this.Button_Cancel = new System.Windows.Forms.Button();
            this.Label_Number = new System.Windows.Forms.Label();
            this.TextBox_Prefix = new System.Windows.Forms.TextBox();
            this.label1 = new System.Windows.Forms.Label();
            this.label2 = new System.Windows.Forms.Label();
            this.groupBox1 = new System.Windows.Forms.GroupBox();
            this.Radio_None = new System.Windows.Forms.RadioButton();
            this.Label_Block = new System.Windows.Forms.Label();
            this.Label_Attribute = new System.Windows.Forms.Label();
            this.Check_UsePrefix = new System.Windows.Forms.CheckBox();
            this.Combo_AttributeTag = new System.Windows.Forms.ComboBox();
            this.Combo_BlockReference = new System.Windows.Forms.ComboBox();
            this.Radio_Attribute = new System.Windows.Forms.RadioButton();
            this.Radio_Manual = new System.Windows.Forms.RadioButton();
            this.Radio_Layer = new System.Windows.Forms.RadioButton();
            this.Radio_List = new System.Windows.Forms.RadioButton();
            this.TextBox_Scale = new System.Windows.Forms.TextBox();
            this.label6 = new System.Windows.Forms.Label();
            this.label7 = new System.Windows.Forms.Label();
            this.TextBox_TextHeight = new System.Windows.Forms.TextBox();
            this.Check_NoBlock = new System.Windows.Forms.CheckBox();
            this.label4 = new System.Windows.Forms.Label();
            this.TextBox_BlockScale = new System.Windows.Forms.TextBox();
            this.Check_ScaleExist = new System.Windows.Forms.CheckBox();
            this.Check_SelfNumberColor = new System.Windows.Forms.CheckBox();
            this.Button_Delete = new System.Windows.Forms.Button();
            this.groupBox1.SuspendLayout();
            this.SuspendLayout();
            // 
            // Combo_Layers
            // 
            this.Combo_Layers.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.Combo_Layers.FormattingEnabled = true;
            this.Combo_Layers.Location = new System.Drawing.Point(12, 38);
            this.Combo_Layers.Name = "Combo_Layers";
            this.Combo_Layers.Size = new System.Drawing.Size(325, 24);
            this.Combo_Layers.TabIndex = 0;
            this.Combo_Layers.SelectedIndexChanged += new System.EventHandler(this.Combo_Layers_SelectedIndexChanged);
            // 
            // Button_Ok
            // 
            this.Button_Ok.Location = new System.Drawing.Point(10, 371);
            this.Button_Ok.Name = "Button_Ok";
            this.Button_Ok.Size = new System.Drawing.Size(148, 50);
            this.Button_Ok.TabIndex = 1;
            this.Button_Ok.Text = "Ok";
            this.Button_Ok.UseVisualStyleBackColor = true;
            this.Button_Ok.Click += new System.EventHandler(this.Button_Ok_Click);
            // 
            // Button_Cancel
            // 
            this.Button_Cancel.Location = new System.Drawing.Point(318, 371);
            this.Button_Cancel.Name = "Button_Cancel";
            this.Button_Cancel.Size = new System.Drawing.Size(148, 50);
            this.Button_Cancel.TabIndex = 2;
            this.Button_Cancel.Text = "Cancel";
            this.Button_Cancel.UseVisualStyleBackColor = true;
            this.Button_Cancel.Click += new System.EventHandler(this.Button_Cancel_Click);
            // 
            // Label_Number
            // 
            this.Label_Number.Location = new System.Drawing.Point(387, 38);
            this.Label_Number.Name = "Label_Number";
            this.Label_Number.Size = new System.Drawing.Size(56, 24);
            this.Label_Number.TabIndex = 3;
            this.Label_Number.Text = "0";
            this.Label_Number.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // TextBox_Prefix
            // 
            this.TextBox_Prefix.Location = new System.Drawing.Point(99, 97);
            this.TextBox_Prefix.Name = "TextBox_Prefix";
            this.TextBox_Prefix.Size = new System.Drawing.Size(185, 22);
            this.TextBox_Prefix.TabIndex = 4;
            this.TextBox_Prefix.Text = "Лист ";
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(12, 9);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(186, 16);
            this.label1.TabIndex = 5;
            this.label1.Text = "Слои с видовыми экранами";
            this.label1.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(355, 9);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(117, 16);
            this.label2.TabIndex = 6;
            this.label2.Text = "Экранов на слое";
            this.label2.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // groupBox1
            // 
            this.groupBox1.Controls.Add(this.Radio_None);
            this.groupBox1.Controls.Add(this.Label_Block);
            this.groupBox1.Controls.Add(this.Label_Attribute);
            this.groupBox1.Controls.Add(this.Check_UsePrefix);
            this.groupBox1.Controls.Add(this.Combo_AttributeTag);
            this.groupBox1.Controls.Add(this.Combo_BlockReference);
            this.groupBox1.Controls.Add(this.Radio_Attribute);
            this.groupBox1.Controls.Add(this.Radio_Manual);
            this.groupBox1.Controls.Add(this.Radio_Layer);
            this.groupBox1.Controls.Add(this.Radio_List);
            this.groupBox1.Controls.Add(this.TextBox_Prefix);
            this.groupBox1.Location = new System.Drawing.Point(11, 163);
            this.groupBox1.Name = "groupBox1";
            this.groupBox1.Size = new System.Drawing.Size(457, 202);
            this.groupBox1.TabIndex = 8;
            this.groupBox1.TabStop = false;
            this.groupBox1.Text = "Нумерация экранов (префикс)";
            // 
            // Radio_None
            // 
            this.Radio_None.AutoSize = true;
            this.Radio_None.Location = new System.Drawing.Point(6, 21);
            this.Radio_None.Name = "Radio_None";
            this.Radio_None.Size = new System.Drawing.Size(121, 20);
            this.Radio_None.TabIndex = 17;
            this.Radio_None.Text = "Без префикса";
            this.Radio_None.UseVisualStyleBackColor = true;
            this.Radio_None.CheckedChanged += new System.EventHandler(this.Radio_None_CheckedChanged);
            // 
            // Label_Block
            // 
            this.Label_Block.AutoSize = true;
            this.Label_Block.Location = new System.Drawing.Point(197, 134);
            this.Label_Block.Name = "Label_Block";
            this.Label_Block.Size = new System.Drawing.Size(39, 16);
            this.Label_Block.TabIndex = 16;
            this.Label_Block.Text = "Блок";
            this.Label_Block.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // Label_Attribute
            // 
            this.Label_Attribute.AutoSize = true;
            this.Label_Attribute.Location = new System.Drawing.Point(322, 134);
            this.Label_Attribute.Name = "Label_Attribute";
            this.Label_Attribute.Size = new System.Drawing.Size(62, 16);
            this.Label_Attribute.TabIndex = 15;
            this.Label_Attribute.Text = "Атрибут";
            this.Label_Attribute.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // Check_UsePrefix
            // 
            this.Check_UsePrefix.AutoSize = true;
            this.Check_UsePrefix.Checked = true;
            this.Check_UsePrefix.CheckState = System.Windows.Forms.CheckState.Checked;
            this.Check_UsePrefix.Location = new System.Drawing.Point(6, 162);
            this.Check_UsePrefix.Name = "Check_UsePrefix";
            this.Check_UsePrefix.Size = new System.Drawing.Size(188, 20);
            this.Check_UsePrefix.TabIndex = 14;
            this.Check_UsePrefix.Text = "использовать префикс?";
            this.Check_UsePrefix.UseVisualStyleBackColor = true;
            // 
            // Combo_AttributeTag
            // 
            this.Combo_AttributeTag.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.Combo_AttributeTag.FormattingEnabled = true;
            this.Combo_AttributeTag.Location = new System.Drawing.Point(325, 158);
            this.Combo_AttributeTag.Name = "Combo_AttributeTag";
            this.Combo_AttributeTag.Size = new System.Drawing.Size(115, 24);
            this.Combo_AttributeTag.TabIndex = 13;
            this.Combo_AttributeTag.SelectedIndexChanged += new System.EventHandler(this.Combo_AttributeTag_SelectedIndexChanged);
            // 
            // Combo_BlockReference
            // 
            this.Combo_BlockReference.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.Combo_BlockReference.FormattingEnabled = true;
            this.Combo_BlockReference.Location = new System.Drawing.Point(200, 158);
            this.Combo_BlockReference.Name = "Combo_BlockReference";
            this.Combo_BlockReference.Size = new System.Drawing.Size(119, 24);
            this.Combo_BlockReference.TabIndex = 9;
            this.Combo_BlockReference.SelectedIndexChanged += new System.EventHandler(this.Combo_BlockReference_SelectedIndexChanged);
            // 
            // Radio_Attribute
            // 
            this.Radio_Attribute.AutoSize = true;
            this.Radio_Attribute.Location = new System.Drawing.Point(6, 139);
            this.Radio_Attribute.Name = "Radio_Attribute";
            this.Radio_Attribute.Size = new System.Drawing.Size(151, 20);
            this.Radio_Attribute.TabIndex = 12;
            this.Radio_Attribute.Text = "из атрибута блока";
            this.Radio_Attribute.UseVisualStyleBackColor = true;
            this.Radio_Attribute.CheckedChanged += new System.EventHandler(this.Radio_Attribute_CheckedChanged);
            // 
            // Radio_Manual
            // 
            this.Radio_Manual.AutoSize = true;
            this.Radio_Manual.Location = new System.Drawing.Point(6, 99);
            this.Radio_Manual.Name = "Radio_Manual";
            this.Radio_Manual.Size = new System.Drawing.Size(87, 20);
            this.Radio_Manual.TabIndex = 11;
            this.Radio_Manual.Text = "Вручную";
            this.Radio_Manual.UseVisualStyleBackColor = true;
            this.Radio_Manual.CheckedChanged += new System.EventHandler(this.Radio_Manual_CheckedChanged);
            // 
            // Radio_Layer
            // 
            this.Radio_Layer.AutoSize = true;
            this.Radio_Layer.Location = new System.Drawing.Point(6, 73);
            this.Radio_Layer.Name = "Radio_Layer";
            this.Radio_Layer.Size = new System.Drawing.Size(127, 20);
            this.Radio_Layer.TabIndex = 10;
            this.Radio_Layer.Text = "Название слоя";
            this.Radio_Layer.UseVisualStyleBackColor = true;
            this.Radio_Layer.CheckedChanged += new System.EventHandler(this.Radio_Layer_CheckedChanged);
            // 
            // Radio_List
            // 
            this.Radio_List.AutoSize = true;
            this.Radio_List.Location = new System.Drawing.Point(6, 47);
            this.Radio_List.Name = "Radio_List";
            this.Radio_List.Size = new System.Drawing.Size(135, 20);
            this.Radio_List.TabIndex = 9;
            this.Radio_List.Text = "Название листа";
            this.Radio_List.UseVisualStyleBackColor = true;
            this.Radio_List.CheckedChanged += new System.EventHandler(this.Radio_List_CheckedChanged);
            // 
            // TextBox_Scale
            // 
            this.TextBox_Scale.Location = new System.Drawing.Point(12, 72);
            this.TextBox_Scale.Name = "TextBox_Scale";
            this.TextBox_Scale.Size = new System.Drawing.Size(57, 22);
            this.TextBox_Scale.TabIndex = 17;
            this.TextBox_Scale.Text = "1";
            // 
            // label6
            // 
            this.label6.AutoSize = true;
            this.label6.Location = new System.Drawing.Point(75, 75);
            this.label6.Name = "label6";
            this.label6.Size = new System.Drawing.Size(137, 16);
            this.label6.TabIndex = 17;
            this.label6.Text = "Масштаб штриховки";
            this.label6.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // label7
            // 
            this.label7.AutoSize = true;
            this.label7.Location = new System.Drawing.Point(75, 103);
            this.label7.Name = "label7";
            this.label7.Size = new System.Drawing.Size(102, 16);
            this.label7.TabIndex = 18;
            this.label7.Text = "Высота текста";
            this.label7.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // TextBox_TextHeight
            // 
            this.TextBox_TextHeight.Location = new System.Drawing.Point(12, 100);
            this.TextBox_TextHeight.Name = "TextBox_TextHeight";
            this.TextBox_TextHeight.Size = new System.Drawing.Size(57, 22);
            this.TextBox_TextHeight.TabIndex = 19;
            this.TextBox_TextHeight.Text = "10";
            // 
            // Check_NoBlock
            // 
            this.Check_NoBlock.AutoSize = true;
            this.Check_NoBlock.Location = new System.Drawing.Point(271, 72);
            this.Check_NoBlock.Name = "Check_NoBlock";
            this.Check_NoBlock.Size = new System.Drawing.Size(188, 36);
            this.Check_NoBlock.TabIndex = 18;
            this.Check_NoBlock.Text = "Не использовать блоки\r\n(Требуется перезапуск)";
            this.Check_NoBlock.UseVisualStyleBackColor = true;
            // 
            // label4
            // 
            this.label4.AutoSize = true;
            this.label4.Location = new System.Drawing.Point(74, 131);
            this.label4.Name = "label4";
            this.label4.Size = new System.Drawing.Size(115, 16);
            this.label4.TabIndex = 21;
            this.label4.Text = "Масштаб блоков";
            this.label4.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // TextBox_BlockScale
            // 
            this.TextBox_BlockScale.Location = new System.Drawing.Point(11, 128);
            this.TextBox_BlockScale.Name = "TextBox_BlockScale";
            this.TextBox_BlockScale.Size = new System.Drawing.Size(57, 22);
            this.TextBox_BlockScale.TabIndex = 22;
            this.TextBox_BlockScale.Text = "0.1";
            // 
            // Check_ScaleExist
            // 
            this.Check_ScaleExist.AutoSize = true;
            this.Check_ScaleExist.Location = new System.Drawing.Point(211, 111);
            this.Check_ScaleExist.Name = "Check_ScaleExist";
            this.Check_ScaleExist.Size = new System.Drawing.Size(248, 20);
            this.Check_ScaleExist.TabIndex = 23;
            this.Check_ScaleExist.Text = "Масштабировать существующие?";
            this.Check_ScaleExist.UseVisualStyleBackColor = true;
            // 
            // Check_SelfNumberColor
            // 
            this.Check_SelfNumberColor.AutoSize = true;
            this.Check_SelfNumberColor.Location = new System.Drawing.Point(211, 137);
            this.Check_SelfNumberColor.Name = "Check_SelfNumberColor";
            this.Check_SelfNumberColor.Size = new System.Drawing.Size(239, 20);
            this.Check_SelfNumberColor.TabIndex = 24;
            this.Check_SelfNumberColor.Text = "Свой номер по цвету штриховки";
            this.Check_SelfNumberColor.UseVisualStyleBackColor = true;
            // 
            // Button_Delete
            // 
            this.Button_Delete.Location = new System.Drawing.Point(164, 371);
            this.Button_Delete.Name = "Button_Delete";
            this.Button_Delete.Size = new System.Drawing.Size(148, 50);
            this.Button_Delete.TabIndex = 25;
            this.Button_Delete.Text = "Delete";
            this.Button_Delete.UseVisualStyleBackColor = true;
            this.Button_Delete.Click += new System.EventHandler(this.Button_Delete_Click);
            // 
            // SheetsCreateForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(8F, 16F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(478, 449);
            this.Controls.Add(this.Button_Delete);
            this.Controls.Add(this.Check_SelfNumberColor);
            this.Controls.Add(this.Check_ScaleExist);
            this.Controls.Add(this.label4);
            this.Controls.Add(this.TextBox_BlockScale);
            this.Controls.Add(this.Check_NoBlock);
            this.Controls.Add(this.label7);
            this.Controls.Add(this.TextBox_TextHeight);
            this.Controls.Add(this.label6);
            this.Controls.Add(this.TextBox_Scale);
            this.Controls.Add(this.groupBox1);
            this.Controls.Add(this.label2);
            this.Controls.Add(this.label1);
            this.Controls.Add(this.Label_Number);
            this.Controls.Add(this.Button_Cancel);
            this.Controls.Add(this.Button_Ok);
            this.Controls.Add(this.Combo_Layers);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.Fixed3D;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.MinimumSize = new System.Drawing.Size(500, 500);
            this.Name = "SheetsCreateForm";
            this.Text = "Form1";
            this.groupBox1.ResumeLayout(false);
            this.groupBox1.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.ComboBox Combo_Layers;
        private System.Windows.Forms.Button Button_Ok;
        private System.Windows.Forms.Button Button_Cancel;
        private System.Windows.Forms.Label Label_Number;
        private System.Windows.Forms.TextBox TextBox_Prefix;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.GroupBox groupBox1;
        private System.Windows.Forms.RadioButton Radio_Attribute;
        private System.Windows.Forms.RadioButton Radio_Manual;
        private System.Windows.Forms.RadioButton Radio_Layer;
        private System.Windows.Forms.RadioButton Radio_List;
        private System.Windows.Forms.ComboBox Combo_AttributeTag;
        private System.Windows.Forms.ComboBox Combo_BlockReference;
        private System.Windows.Forms.Label Label_Block;
        private System.Windows.Forms.Label Label_Attribute;
        private System.Windows.Forms.CheckBox Check_UsePrefix;
        private System.Windows.Forms.TextBox TextBox_Scale;
        private System.Windows.Forms.Label label6;
        private System.Windows.Forms.Label label7;
        private System.Windows.Forms.TextBox TextBox_TextHeight;
        private System.Windows.Forms.RadioButton Radio_None;
        private System.Windows.Forms.CheckBox Check_NoBlock;
        private System.Windows.Forms.Label label4;
        private System.Windows.Forms.TextBox TextBox_BlockScale;
        private System.Windows.Forms.CheckBox Check_ScaleExist;
        private System.Windows.Forms.CheckBox Check_SelfNumberColor;
        private System.Windows.Forms.Button Button_Delete;
    }
}