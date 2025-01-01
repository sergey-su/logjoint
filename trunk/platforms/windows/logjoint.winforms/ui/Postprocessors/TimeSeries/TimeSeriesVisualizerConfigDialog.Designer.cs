namespace LogJoint.UI.Postprocessing.TimeSeriesVisualizer
{
    partial class TimeSeriesVisualizerConfigDialog
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
            this.label1 = new System.Windows.Forms.Label();
            this.collapseAllLinkLabel = new System.Windows.Forms.LinkLabel();
            this.uncheckAllLinkLabel = new System.Windows.Forms.LinkLabel();
            this.treeView = new System.Windows.Forms.MixedCheckBoxesTreeView();
            this.label3 = new System.Windows.Forms.Label();
            this.colorComboBox = new System.Windows.Forms.ComboBox();
            this.markerComboBox = new System.Windows.Forms.ComboBox();
            this.drawLineCheckBox = new System.Windows.Forms.CheckBox();
            this.label4 = new System.Windows.Forms.Label();
            this.descriptionLabel = new System.Windows.Forms.TextBox();
            this.SuspendLayout();
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(13, 13);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(155, 17);
            this.label1.TabIndex = 1;
            this.label1.Text = "Select objects to display";
            // 
            // collapseAllLinkLabel
            // 
            this.collapseAllLinkLabel.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.collapseAllLinkLabel.AutoSize = true;
            this.collapseAllLinkLabel.Location = new System.Drawing.Point(358, 15);
            this.collapseAllLinkLabel.Name = "collapseAllLinkLabel";
            this.collapseAllLinkLabel.Size = new System.Drawing.Size(70, 17);
            this.collapseAllLinkLabel.TabIndex = 2;
            this.collapseAllLinkLabel.TabStop = true;
            this.collapseAllLinkLabel.Text = "collapse all";
            this.collapseAllLinkLabel.LinkClicked += new System.Windows.Forms.LinkLabelLinkClickedEventHandler(this.collapseAllLinkLabel_LinkClicked);
            // 
            // uncheckAllLinkLabel
            // 
            this.uncheckAllLinkLabel.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.uncheckAllLinkLabel.AutoSize = true;
            this.uncheckAllLinkLabel.Location = new System.Drawing.Point(263, 15);
            this.uncheckAllLinkLabel.Name = "uncheckAllLinkLabel";
            this.uncheckAllLinkLabel.Size = new System.Drawing.Size(75, 17);
            this.uncheckAllLinkLabel.TabIndex = 2;
            this.uncheckAllLinkLabel.TabStop = true;
            this.uncheckAllLinkLabel.Text = "uncheck all";
            this.uncheckAllLinkLabel.LinkClicked += new System.Windows.Forms.LinkLabelLinkClickedEventHandler(this.uncheckAllLinkLabel_LinkClicked);
            // 
            // treeView
            // 
            this.treeView.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom)
            | System.Windows.Forms.AnchorStyles.Left)
            | System.Windows.Forms.AnchorStyles.Right)));
            this.treeView.CheckBoxes = true;
            this.treeView.FullRowSelect = true;
            this.treeView.HideSelection = false;
            this.treeView.Location = new System.Drawing.Point(12, 43);
            this.treeView.Name = "treeView";
            this.treeView.Size = new System.Drawing.Size(416, 416);
            this.treeView.TabIndex = 0;
            this.treeView.AfterCheck += new System.Windows.Forms.TreeViewEventHandler(this.treeView_AfterCheck);
            this.treeView.AfterSelect += new System.Windows.Forms.TreeViewEventHandler(this.treeView_AfterSelect);
            // 
            // label3
            // 
            this.label3.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.label3.AutoSize = true;
            this.label3.Location = new System.Drawing.Point(13, 521);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(40, 17);
            this.label3.TabIndex = 4;
            this.label3.Text = "Color";
            // 
            // colorComboBox
            // 
            this.colorComboBox.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.colorComboBox.DrawMode = System.Windows.Forms.DrawMode.OwnerDrawFixed;
            this.colorComboBox.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.colorComboBox.Enabled = false;
            this.colorComboBox.FormattingEnabled = true;
            this.colorComboBox.Location = new System.Drawing.Point(62, 518);
            this.colorComboBox.Name = "colorComboBox";
            this.colorComboBox.Size = new System.Drawing.Size(88, 25);
            this.colorComboBox.TabIndex = 5;
            this.colorComboBox.DrawItem += new System.Windows.Forms.DrawItemEventHandler(this.colorComboBox_DrawItem);
            this.colorComboBox.SelectedIndexChanged += new System.EventHandler(this.colorComboBox_SelectedIndexChanged);
            // 
            // markerComboBox
            // 
            this.markerComboBox.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.markerComboBox.DrawMode = System.Windows.Forms.DrawMode.OwnerDrawFixed;
            this.markerComboBox.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.markerComboBox.Enabled = false;
            this.markerComboBox.FormattingEnabled = true;
            this.markerComboBox.Location = new System.Drawing.Point(222, 518);
            this.markerComboBox.Name = "markerComboBox";
            this.markerComboBox.Size = new System.Drawing.Size(88, 25);
            this.markerComboBox.TabIndex = 7;
            this.markerComboBox.DrawItem += new System.Windows.Forms.DrawItemEventHandler(this.markerComboBox_DrawItem);
            this.markerComboBox.SelectedIndexChanged += new System.EventHandler(this.markerComboBox_SelectedIndexChanged);
            // 
            // drawLineCheckBox
            // 
            this.drawLineCheckBox.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.drawLineCheckBox.Enabled = false;
            this.drawLineCheckBox.Location = new System.Drawing.Point(330, 518);
            this.drawLineCheckBox.Name = "drawLineCheckBox";
            this.drawLineCheckBox.Size = new System.Drawing.Size(88, 25);
            this.drawLineCheckBox.TabIndex = 8;
            this.drawLineCheckBox.AutoSize = true;
            this.drawLineCheckBox.Text = "Draw line";
            this.drawLineCheckBox.CheckedChanged += new System.EventHandler(this.drawLineCheckBox_Checked);
            // 
            // label4
            // 
            this.label4.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.label4.AutoSize = true;
            this.label4.Location = new System.Drawing.Point(164, 521);
            this.label4.Name = "label4";
            this.label4.Size = new System.Drawing.Size(49, 17);
            this.label4.TabIndex = 6;
            this.label4.Text = "Marker";
            // 
            // descriptionLabel
            // 
            this.descriptionLabel.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)
            | System.Windows.Forms.AnchorStyles.Right)));
            this.descriptionLabel.Location = new System.Drawing.Point(12, 464);
            this.descriptionLabel.Multiline = true;
            this.descriptionLabel.Name = "descriptionLabel";
            this.descriptionLabel.ReadOnly = true;
            this.descriptionLabel.Size = new System.Drawing.Size(416, 43);
            this.descriptionLabel.TabIndex = 8;
            // 
            // TimeSeriesVisualizerConfigDialog
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(120F, 120F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Dpi;
            this.ClientSize = new System.Drawing.Size(440, 552);
            this.Controls.Add(this.descriptionLabel);
            this.Controls.Add(this.markerComboBox);
            this.Controls.Add(this.label4);
            this.Controls.Add(this.colorComboBox);
            this.Controls.Add(this.label3);
            this.Controls.Add(this.uncheckAllLinkLabel);
            this.Controls.Add(this.collapseAllLinkLabel);
            this.Controls.Add(this.label1);
            this.Controls.Add(this.treeView);
            this.Controls.Add(this.drawLineCheckBox);
            this.Font = new System.Drawing.Font("Tahoma", 8.25F);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.SizableToolWindow;
            this.Name = "TimeSeriesVisualizerConfigDialog";
            this.Text = "Time Series Config";
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.MixedCheckBoxesTreeView treeView;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.LinkLabel collapseAllLinkLabel;
        private System.Windows.Forms.LinkLabel uncheckAllLinkLabel;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.ComboBox colorComboBox;
        private System.Windows.Forms.ComboBox markerComboBox;
        private System.Windows.Forms.CheckBox drawLineCheckBox;
        private System.Windows.Forms.Label label4;
        private System.Windows.Forms.TextBox descriptionLabel;
    }
}