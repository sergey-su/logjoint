namespace LogJoint.UI
{
    partial class EverythingFilteredOutMessage
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

        #region Component Designer generated code

        /// <summary> 
        /// Required method for Designer support - do not modify 
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.label1 = new System.Windows.Forms.Label();
            this.SearchUpLinkLabel = new System.Windows.Forms.LinkLabel();
            this.SearchDownLinkLabel = new System.Windows.Forms.LinkLabel();
            this.FiltersLinkLabel = new System.Windows.Forms.LinkLabel();
            this.tableLayoutPanel1 = new System.Windows.Forms.TableLayoutPanel();
            this.tableLayoutPanel1.SuspendLayout();
            this.SuspendLayout();
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(3, 0);
            this.label1.Name = "label1";
            this.tableLayoutPanel1.SetRowSpan(this.label1, 3);
            this.label1.Size = new System.Drawing.Size(218, 60);
            this.label1.TabIndex = 0;
            this.label1.Text = "All loaded messages have been filtered out \r\nby current display filters. Change f" +
                "ilters \r\nsettings or search for messages that match \r\ncurrent filters.\r\n \r\n";
            // 
            // SearchUpLinkLabel
            // 
            this.SearchUpLinkLabel.AutoSize = true;
            this.SearchUpLinkLabel.Location = new System.Drawing.Point(240, 20);
            this.SearchUpLinkLabel.Name = "SearchUpLinkLabel";
            this.SearchUpLinkLabel.Size = new System.Drawing.Size(66, 13);
            this.SearchUpLinkLabel.TabIndex = 1;
            this.SearchUpLinkLabel.TabStop = true;
            this.SearchUpLinkLabel.Text = "˄ Search up";
            // 
            // SearchDownLinkLabel
            // 
            this.SearchDownLinkLabel.AutoSize = true;
            this.SearchDownLinkLabel.Location = new System.Drawing.Point(240, 40);
            this.SearchDownLinkLabel.Name = "SearchDownLinkLabel";
            this.SearchDownLinkLabel.Size = new System.Drawing.Size(80, 13);
            this.SearchDownLinkLabel.TabIndex = 2;
            this.SearchDownLinkLabel.TabStop = true;
            this.SearchDownLinkLabel.Text = "˅ Search down";
            // 
            // FiltersLinkLabel
            // 
            this.FiltersLinkLabel.AutoSize = true;
            this.FiltersLinkLabel.Location = new System.Drawing.Point(240, 0);
            this.FiltersLinkLabel.Name = "FiltersLinkLabel";
            this.FiltersLinkLabel.Size = new System.Drawing.Size(74, 13);
            this.FiltersLinkLabel.TabIndex = 1;
            this.FiltersLinkLabel.TabStop = true;
            this.FiltersLinkLabel.Text = "Change filters";
            // 
            // tableLayoutPanel1
            // 
            this.tableLayoutPanel1.ColumnCount = 2;
            this.tableLayoutPanel1.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.tableLayoutPanel1.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle());
            this.tableLayoutPanel1.Controls.Add(this.label1, 0, 0);
            this.tableLayoutPanel1.Controls.Add(this.SearchDownLinkLabel, 1, 2);
            this.tableLayoutPanel1.Controls.Add(this.FiltersLinkLabel, 1, 0);
            this.tableLayoutPanel1.Controls.Add(this.SearchUpLinkLabel, 1, 1);
            this.tableLayoutPanel1.Location = new System.Drawing.Point(27, 37);
            this.tableLayoutPanel1.Name = "tableLayoutPanel1";
            this.tableLayoutPanel1.RowCount = 4;
            this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 20F));
            this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 20F));
            this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 20F));
            this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle());
            this.tableLayoutPanel1.Size = new System.Drawing.Size(323, 85);
            this.tableLayoutPanel1.TabIndex = 3;
            // 
            // EverythingFilteredOutMessage
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(96F, 96F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Dpi;
            this.Controls.Add(this.tableLayoutPanel1);
            this.Font = new System.Drawing.Font("Tahoma", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(204)));
            this.Name = "EverythingFilteredOutMessage";
            this.Size = new System.Drawing.Size(418, 188);
            this.Resize += new System.EventHandler(this.EverythingFilteredOutMessage_Resize);
            this.tableLayoutPanel1.ResumeLayout(false);
            this.tableLayoutPanel1.PerformLayout();
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.TableLayoutPanel tableLayoutPanel1;
        public System.Windows.Forms.LinkLabel SearchUpLinkLabel;
        public System.Windows.Forms.LinkLabel SearchDownLinkLabel;
        public System.Windows.Forms.LinkLabel FiltersLinkLabel;
    }
}
