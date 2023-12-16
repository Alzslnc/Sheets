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
            this.SuspendLayout();
            // 
            // Combo_Layouts
            // 
            this.Combo_Layouts.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.Combo_Layouts.FormattingEnabled = true;
            this.Combo_Layouts.Location = new System.Drawing.Point(12, 38);
            this.Combo_Layouts.Name = "Combo_Layouts";
            this.Combo_Layouts.Size = new System.Drawing.Size(286, 24);
            this.Combo_Layouts.TabIndex = 0;
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(9, 9);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(296, 16);
            this.label1.TabIndex = 1;
            this.label1.Text = "Выберите лист с базовым видовым экраном";
            // 
            // Button_Ok
            // 
            this.Button_Ok.Location = new System.Drawing.Point(12, 82);
            this.Button_Ok.Name = "Button_Ok";
            this.Button_Ok.Size = new System.Drawing.Size(137, 28);
            this.Button_Ok.TabIndex = 2;
            this.Button_Ok.Text = "Выбрать";
            this.Button_Ok.UseVisualStyleBackColor = true;
            this.Button_Ok.Click += new System.EventHandler(this.Button_Ok_Click);
            // 
            // Button_Cancel
            // 
            this.Button_Cancel.Location = new System.Drawing.Point(161, 82);
            this.Button_Cancel.Name = "Button_Cancel";
            this.Button_Cancel.Size = new System.Drawing.Size(137, 28);
            this.Button_Cancel.TabIndex = 3;
            this.Button_Cancel.Text = "Отменить";
            this.Button_Cancel.UseVisualStyleBackColor = true;
            // 
            // LayoutCreateForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(8F, 16F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(310, 125);
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
    }
}