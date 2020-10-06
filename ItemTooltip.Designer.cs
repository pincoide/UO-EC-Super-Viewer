namespace UO_EC_Super_Viewer
{
    partial class ItemTooltip
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose( bool disposing )
        {
            if ( disposing && ( components != null ) )
            {
                components.Dispose();
            }
            base.Dispose( disposing );
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.imgCC = new System.Windows.Forms.PictureBox();
            this.imgKR = new System.Windows.Forms.PictureBox();
            this.pnlItemData = new System.Windows.Forms.Panel();
            this.lblItemData = new System.Windows.Forms.Label();
            ((System.ComponentModel.ISupportInitialize)(this.imgCC)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.imgKR)).BeginInit();
            this.pnlItemData.SuspendLayout();
            this.SuspendLayout();
            // 
            // imgCC
            // 
            this.imgCC.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.imgCC.Location = new System.Drawing.Point(0, 0);
            this.imgCC.Name = "imgCC";
            this.imgCC.Size = new System.Drawing.Size(100, 450);
            this.imgCC.SizeMode = System.Windows.Forms.PictureBoxSizeMode.AutoSize;
            this.imgCC.TabIndex = 0;
            this.imgCC.TabStop = false;
            // 
            // imgKR
            // 
            this.imgKR.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.imgKR.Location = new System.Drawing.Point(100, 0);
            this.imgKR.Name = "imgKR";
            this.imgKR.Size = new System.Drawing.Size(100, 450);
            this.imgKR.SizeMode = System.Windows.Forms.PictureBoxSizeMode.AutoSize;
            this.imgKR.TabIndex = 1;
            this.imgKR.TabStop = false;
            // 
            // pnlItemData
            // 
            this.pnlItemData.AutoSize = true;
            this.pnlItemData.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
            this.pnlItemData.Controls.Add(this.lblItemData);
            this.pnlItemData.Location = new System.Drawing.Point(226, 72);
            this.pnlItemData.Name = "pnlItemData";
            this.pnlItemData.Size = new System.Drawing.Size(38, 13);
            this.pnlItemData.TabIndex = 2;
            // 
            // lblItemData
            // 
            this.lblItemData.AutoSize = true;
            this.lblItemData.Location = new System.Drawing.Point(0, 0);
            this.lblItemData.Name = "lblItemData";
            this.lblItemData.Size = new System.Drawing.Size(35, 13);
            this.lblItemData.TabIndex = 0;
            this.lblItemData.Text = "label1";
            // 
            // ItemTooltip
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
            this.ClientSize = new System.Drawing.Size(600, 383);
            this.Controls.Add(this.pnlItemData);
            this.Controls.Add(this.imgKR);
            this.Controls.Add(this.imgCC);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.None;
            this.Name = "ItemTooltip";
            this.ShowInTaskbar = false;
            this.Text = "ItemTooltip";
            this.TopMost = true;
            this.VisibleChanged += new System.EventHandler(this.ItemTooltip_VisibleChanged);
            ((System.ComponentModel.ISupportInitialize)(this.imgCC)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.imgKR)).EndInit();
            this.pnlItemData.ResumeLayout(false);
            this.pnlItemData.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.PictureBox imgCC;
        private System.Windows.Forms.PictureBox imgKR;
        private System.Windows.Forms.Panel pnlItemData;
        private System.Windows.Forms.Label lblItemData;
    }
}