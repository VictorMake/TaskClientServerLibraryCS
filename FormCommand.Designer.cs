namespace TaskClientServerLibrary
{
    partial class FormCommand
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
            this.components = new System.ComponentModel.Container();
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(FormCommand));
            this.TabControlCommandShassis = new System.Windows.Forms.TabControl();
            this.ImageList1 = new System.Windows.Forms.ImageList(this.components);
            this.ToolTip1 = new System.Windows.Forms.ToolTip(this.components);
            this.SuspendLayout();
            // 
            // TabControlCommandShassis
            // 
            this.TabControlCommandShassis.Dock = System.Windows.Forms.DockStyle.Fill;
            this.TabControlCommandShassis.ImageList = this.ImageList1;
            this.TabControlCommandShassis.Location = new System.Drawing.Point(0, 0);
            this.TabControlCommandShassis.Name = "TabControlCommandShassis";
            this.TabControlCommandShassis.SelectedIndex = 0;
            this.TabControlCommandShassis.Size = new System.Drawing.Size(800, 661);
            this.TabControlCommandShassis.TabIndex = 2;
            // 
            // ImageList1
            // 
            this.ImageList1.ImageStream = ((System.Windows.Forms.ImageListStreamer)(resources.GetObject("ImageList1.ImageStream")));
            this.ImageList1.TransparentColor = System.Drawing.Color.Transparent;
            this.ImageList1.Images.SetKeyName(0, "netcenter_22.ico");
            this.ImageList1.Images.SetKeyName(1, "netcenter_23.ico");
            this.ImageList1.Images.SetKeyName(2, "signal-1.png");
            this.ImageList1.Images.SetKeyName(3, "");
            this.ImageList1.Images.SetKeyName(4, "gg_connecting.png");
            // 
            // FormCommand
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(800, 661);
            this.Controls.Add(this.TabControlCommandShassis);
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.MinimumSize = new System.Drawing.Size(700, 700);
            this.Name = "FormCommand";
            this.Tag = "Обмен командами управления";
            this.Text = "Обмен командами управления";
            this.FormClosed += new System.Windows.Forms.FormClosedEventHandler(this.FormCommand_FormClosed);
            this.Load += new System.EventHandler(this.FormCommand_Load);
            this.ResumeLayout(false);

        }

        #endregion

        internal System.Windows.Forms.TabControl TabControlCommandShassis;
        internal System.Windows.Forms.ImageList ImageList1;
        internal System.Windows.Forms.ToolTip ToolTip1;
    }
}