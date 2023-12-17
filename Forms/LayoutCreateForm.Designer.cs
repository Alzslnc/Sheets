namespace Sheets
{
    partial class LayoutCreateForm
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
            this.Combo_Layouts = new System.Windows.Forms.ComboBox();
            this.label1 = new System.Windows.Forms.Label();
            this.Button_Ok = new System.Windows.Forms.Button();
            this.Button_Cancel = new System.Windows.Forms.Button();
            this.TextBox_Overlap = new System.Windows.Forms.TextBox();
            this.label2 = new System.Windows.Forms.Label();
            this.label3 = new System.Windows.Forms.Label();
            this.TextBox_NewLayoutName = new System.Windows.Forms.TextBox();
            this.SuspendLayout();
            // 
            // Combo_Layouts
            // 
            this.Combo_Layouts.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.Combo_Layouts.FormattingEnabled = true;
            this.Combo_Layouts.Location = new System.Drawing.Point(62, 69);
            this.Combo_Layouts.Name = "Combo_Layouts";
            this.Combo_Layouts.Size = new System.Drawing.Size(286, 24);
            this.Combo_Layouts.TabIndex = 0;
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(59, 40);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(296, 16);
            this.label1.TabIndex = 1;
            this.label1.Text = "Выберите лист с базовым видовым экраном";
            // 
            // Button_Ok
            // 
            this.Button_Ok.Location = new System.Drawing.Point(62, 192);
            this.Button_Ok.Name = "Button_Ok";
            this.Button_Ok.Size = new System.Drawing.Size(137, 28);
            this.Button_Ok.TabIndex = 2;
            this.Button_Ok.Text = "Выбрать";
            this.Button_Ok.UseVisualStyleBackColor = true;
            this.Button_Ok.Click += new System.EventHandler(this.Button_Ok_Click);
            // 
            // Button_Cancel
            // 
            this.Button_Cancel.Location = new System.Drawing.Point(211, 192);
            this.Button_Cancel.Name = "Button_Cancel";
            this.Button_Cancel.Size = new System.Drawing.Size(137, 28);
            this.Button_Cancel.TabIndex = 3;
            this.Button_Cancel.Text = "Отменить";
            this.Button_Cancel.UseVisualStyleBackColor = true;
            // 
            // TextBox_Overlap
            // 
            this.TextBox_Overlap.Location = new System.Drawing.Point(62, 114);
            this.TextBox_Overlap.Name = "TextBox_Overlap";
            this.TextBox_Overlap.Size = new System.Drawing.Size(67, 22);
            this.TextBox_Overlap.TabIndex = 4;
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(135, 117);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(190, 16);
            this.label2.TabIndex = 5;
            this.label2.Text = "Зона перекрытия листов(%)";
            // 
            // label3
            // 
            this.label3.AutoSize = true;
            this.label3.Location = new System.Drawing.Point(184, 155);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(164, 16);
            this.label3.TabIndex = 7;
            this.label3.Text = "Название новых листов";
            // 
            // TextBox_NewLayoutName
            // 
            this.TextBox_NewLayoutName.Location = new System.Drawing.Point(62, 152);
            this.TextBox_NewLayoutName.Name = "TextBox_NewLayoutName";
            this.TextBox_NewLayoutName.Size = new System.Drawing.Size(116, 22);
            this.TextBox_NewLayoutName.TabIndex = 6;
            // 
            // LayoutCreateForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(8F, 16F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(403, 244);
            this.Controls.Add(this.label3);
            this.Controls.Add(this.TextBox_NewLayoutName);
            this.Controls.Add(this.label2);
            this.Controls.Add(this.TextBox_Overlap);
            this.Controls.Add(this.Button_Cancel);
            this.Controls.Add(this.Button_Ok);
            this.Controls.Add(this.label1);
            this.Controls.Add(this.Combo_Layouts);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.None;
            this.Name = "LayoutCreateForm";
            this.Text = "LayoutCreateForm";
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.ComboBox Combo_Layouts;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.Button Button_Ok;
        private System.Windows.Forms.Button Button_Cancel;
        private System.Windows.Forms.TextBox TextBox_Overlap;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.TextBox TextBox_NewLayoutName;
    }
}