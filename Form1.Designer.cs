namespace UpdateAPILite
{
    partial class Updater
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(Updater));
            StatusProgressuint = new ProgressBar();
            UpdateStatus = new Label();
            x = new Label();
            StatusProgressText = new Label();
            SuspendLayout();
            // 
            // StatusProgressuint
            // 
            StatusProgressuint.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            StatusProgressuint.Location = new Point(12, 88);
            StatusProgressuint.Name = "StatusProgressuint";
            StatusProgressuint.Size = new Size(515, 20);
            StatusProgressuint.TabIndex = 1;
            // 
            // UpdateStatus
            // 
            UpdateStatus.AutoSize = true;
            UpdateStatus.Font = new Font("Microsoft YaHei UI", 10F);
            UpdateStatus.Location = new Point(12, 20);
            UpdateStatus.Name = "UpdateStatus";
            UpdateStatus.Size = new Size(129, 23);
            UpdateStatus.TabIndex = 2;
            UpdateStatus.Text = "正在下载更新包";
            // 
            // x
            // 
            x.AutoSize = true;
            x.Font = new Font("Microsoft YaHei UI", 14F);
            x.Location = new Point(8, 45);
            x.Name = "x";
            x.Size = new Size(92, 31);
            x.TabIndex = 3;
            x.Text = "已完成:";
            // 
            // StatusProgressText
            // 
            StatusProgressText.AutoSize = true;
            StatusProgressText.Font = new Font("Microsoft YaHei UI", 14F);
            StatusProgressText.Location = new Point(97, 46);
            StatusProgressText.Name = "StatusProgressText";
            StatusProgressText.Size = new Size(49, 31);
            StatusProgressText.TabIndex = 4;
            StatusProgressText.Text = "0%";
            // 
            // Updater
            // 
            AutoScaleDimensions = new SizeF(9F, 20F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(539, 128);
            Controls.Add(StatusProgressText);
            Controls.Add(x);
            Controls.Add(UpdateStatus);
            Controls.Add(StatusProgressuint);
            Icon = (Icon)resources.GetObject("$this.Icon");
            MaximizeBox = false;
            MaximumSize = new Size(557, 175);
            MinimumSize = new Size(557, 175);
            Name = "Updater";
            StartPosition = FormStartPosition.CenterScreen;
            Text = "版本更新";
            Load += Updater_Load;
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion
        private ProgressBar StatusProgressuint;
        private Label UpdateStatus;
        private Label x;
        private Label StatusProgressText;
    }
}
