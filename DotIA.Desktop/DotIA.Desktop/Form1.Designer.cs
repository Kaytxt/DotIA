namespace DotIA.Desktop
{
    partial class Form1
    {
        /// <summary>
        ///  Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        ///  Clean up any resources being used.
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
        ///  Required method for Designer support - do not modify
        ///  the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            textEmail = new TextBox();
            textSenha = new TextBox();
            SuspendLayout();
            // 
            // textEmail
            // 
            textEmail.Location = new Point(0, 0);
            textEmail.Name = "textEmail";
            textEmail.Size = new Size(100, 23);
            textEmail.TabIndex = 0;
            // 
            // textSenha
            // 
            textSenha.Location = new Point(0, 0);
            textSenha.Name = "textSenha";
            textSenha.Size = new Size(100, 23);
            textSenha.TabIndex = 1;
            // 
            // Form1
            // 
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(800, 450);
            Controls.Add(textSenha);
            Controls.Add(textEmail);
            Name = "Form1";
            Text = "Form1";
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion

        private TextBox textEmail;
        private TextBox textSenha;
    }
}
