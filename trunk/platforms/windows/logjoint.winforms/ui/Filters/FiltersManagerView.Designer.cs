namespace LogJoint.UI
{
    partial class FiltersManagerView
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
            subscription?.Dispose();
            filterDialog?.Dispose();
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
            this.components = new System.ComponentModel.Container();
            this.flowLayoutPanel1 = new System.Windows.Forms.FlowLayoutPanel();
            this.enableFilteringCheckBox = new System.Windows.Forms.CheckBox();
            this.addFilterButton = new System.Windows.Forms.Button();
            this.deleteFilterButton = new System.Windows.Forms.Button();
            this.moveFilterUpButton = new System.Windows.Forms.Button();
            this.moveFilterDownButton = new System.Windows.Forms.Button();
            this.prevButton = new System.Windows.Forms.Button();
            this.nextButton = new System.Windows.Forms.Button();
            this.toolTip1 = new System.Windows.Forms.ToolTip(this.components);
            this.editButton = new System.Windows.Forms.Button();
            this.filtersListView = new LogJoint.UI.FiltersListView();
            this.flowLayoutPanel1.SuspendLayout();
            this.SuspendLayout();
            // 
            // flowLayoutPanel1
            // 
            this.flowLayoutPanel1.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
            | System.Windows.Forms.AnchorStyles.Right)));
            this.flowLayoutPanel1.Controls.Add(this.enableFilteringCheckBox);
            this.flowLayoutPanel1.Controls.Add(this.addFilterButton);
            this.flowLayoutPanel1.Controls.Add(this.deleteFilterButton);
            this.flowLayoutPanel1.Controls.Add(this.moveFilterUpButton);
            this.flowLayoutPanel1.Controls.Add(this.moveFilterDownButton);
            this.flowLayoutPanel1.Controls.Add(this.editButton);
            this.flowLayoutPanel1.Controls.Add(this.prevButton);
            this.flowLayoutPanel1.Controls.Add(this.nextButton);
            this.flowLayoutPanel1.Location = new System.Drawing.Point(0, 0);
            this.flowLayoutPanel1.Margin = new System.Windows.Forms.Padding(0);
            this.flowLayoutPanel1.Name = "flowLayoutPanel1";
            this.flowLayoutPanel1.Size = new System.Drawing.Size(975, 36);
            this.flowLayoutPanel1.TabIndex = 10;
            // 
            // enableFilteringCheckBox
            // 
            this.enableFilteringCheckBox.AutoSize = true;
            this.enableFilteringCheckBox.Checked = true;
            this.enableFilteringCheckBox.CheckState = System.Windows.Forms.CheckState.Checked;
            this.enableFilteringCheckBox.Location = new System.Drawing.Point(4, 6);
            this.enableFilteringCheckBox.Margin = new System.Windows.Forms.Padding(4, 6, 4, 4);
            this.enableFilteringCheckBox.MinimumSize = new System.Drawing.Size(160, 0);
            this.enableFilteringCheckBox.Name = "enableFilteringCheckBox";
            this.enableFilteringCheckBox.Size = new System.Drawing.Size(160, 21);
            this.enableFilteringCheckBox.TabIndex = 1;
            this.enableFilteringCheckBox.Text = "Enable";
            this.enableFilteringCheckBox.UseVisualStyleBackColor = true;
            this.enableFilteringCheckBox.CheckedChanged += new System.EventHandler(this.enableFilteringCheckBox_CheckedChanged);
            // 
            // addFilterButton
            // 
            this.addFilterButton.Location = new System.Drawing.Point(170, 3);
            this.addFilterButton.Margin = new System.Windows.Forms.Padding(2, 3, 2, 2);
            this.addFilterButton.Name = "addFilterButton";
            this.addFilterButton.Size = new System.Drawing.Size(94, 29);
            this.addFilterButton.TabIndex = 2;
            this.addFilterButton.Text = "Add...";
            this.addFilterButton.UseVisualStyleBackColor = true;
            this.addFilterButton.Click += new System.EventHandler(this.addFilterButton_Click);
            // 
            // deleteFilterButton
            // 
            this.deleteFilterButton.Enabled = false;
            this.deleteFilterButton.Location = new System.Drawing.Point(268, 3);
            this.deleteFilterButton.Margin = new System.Windows.Forms.Padding(2, 3, 2, 2);
            this.deleteFilterButton.Name = "deleteFilterButton";
            this.deleteFilterButton.Size = new System.Drawing.Size(94, 29);
            this.deleteFilterButton.TabIndex = 3;
            this.deleteFilterButton.Text = "Remove";
            this.deleteFilterButton.UseVisualStyleBackColor = true;
            this.deleteFilterButton.Click += new System.EventHandler(this.deleteFilterButton_Click);
            // 
            // moveFilterUpButton
            // 
            this.moveFilterUpButton.Enabled = false;
            this.moveFilterUpButton.Location = new System.Drawing.Point(366, 3);
            this.moveFilterUpButton.Margin = new System.Windows.Forms.Padding(2, 3, 2, 2);
            this.moveFilterUpButton.Name = "moveFilterUpButton";
            this.moveFilterUpButton.Size = new System.Drawing.Size(94, 29);
            this.moveFilterUpButton.TabIndex = 4;
            this.moveFilterUpButton.Text = "Move Up";
            this.moveFilterUpButton.UseVisualStyleBackColor = true;
            this.moveFilterUpButton.Click += new System.EventHandler(this.moveFilterUpButton_Click);
            // 
            // moveFilterDownButton
            // 
            this.moveFilterDownButton.Enabled = false;
            this.moveFilterDownButton.Location = new System.Drawing.Point(464, 3);
            this.moveFilterDownButton.Margin = new System.Windows.Forms.Padding(2, 3, 2, 2);
            this.moveFilterDownButton.Name = "moveFilterDownButton";
            this.moveFilterDownButton.Size = new System.Drawing.Size(94, 29);
            this.moveFilterDownButton.TabIndex = 5;
            this.moveFilterDownButton.Text = "Move Down";
            this.moveFilterDownButton.UseVisualStyleBackColor = true;
            this.moveFilterDownButton.Click += new System.EventHandler(this.moveFilterDownButton_Click);
            // 
            // prevButton
            // 
            this.prevButton.Location = new System.Drawing.Point(677, 3);
            this.prevButton.Margin = new System.Windows.Forms.Padding(19, 3, 2, 2);
            this.prevButton.Name = "prevButton";
            this.prevButton.Size = new System.Drawing.Size(94, 29);
            this.prevButton.TabIndex = 7;
            this.prevButton.Text = "<< Prev";
            this.prevButton.UseVisualStyleBackColor = true;
            this.prevButton.Click += new System.EventHandler(this.prevButton_Click);
            // 
            // nextButton
            // 
            this.nextButton.Location = new System.Drawing.Point(775, 3);
            this.nextButton.Margin = new System.Windows.Forms.Padding(2, 3, 2, 2);
            this.nextButton.Name = "nextButton";
            this.nextButton.Size = new System.Drawing.Size(94, 29);
            this.nextButton.TabIndex = 8;
            this.nextButton.Text = "Next >>";
            this.nextButton.UseVisualStyleBackColor = true;
            this.nextButton.Click += new System.EventHandler(this.nextButton_Click);
            // 
            // editButton
            // 
            this.editButton.Location = new System.Drawing.Point(562, 3);
            this.editButton.Margin = new System.Windows.Forms.Padding(2, 3, 2, 2);
            this.editButton.Name = "editButton";
            this.editButton.Size = new System.Drawing.Size(94, 29);
            this.editButton.TabIndex = 6;
            this.editButton.Text = "Edit...";
            this.editButton.UseVisualStyleBackColor = true;
            this.editButton.Click += new System.EventHandler(this.editButton_Click);
            // 
            // filtersListView
            // 
            this.filtersListView.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom)
            | System.Windows.Forms.AnchorStyles.Left)
            | System.Windows.Forms.AnchorStyles.Right)));
            this.filtersListView.Font = new System.Drawing.Font("Tahoma", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(204)));
            this.filtersListView.Location = new System.Drawing.Point(0, 36);
            this.filtersListView.Margin = new System.Windows.Forms.Padding(0);
            this.filtersListView.Name = "filtersListView";
            this.filtersListView.Size = new System.Drawing.Size(991, 60);
            this.filtersListView.TabIndex = 20;
            // 
            // FiltersManagerView
            // 
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Inherit;
            this.Controls.Add(this.filtersListView);
            this.Controls.Add(this.flowLayoutPanel1);
            this.Font = new System.Drawing.Font("Tahoma", 8.25F);
            this.Margin = new System.Windows.Forms.Padding(0);
            this.Name = "FiltersManagerView";
            this.Size = new System.Drawing.Size(991, 96);
            this.flowLayoutPanel1.ResumeLayout(false);
            this.flowLayoutPanel1.PerformLayout();
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.FlowLayoutPanel flowLayoutPanel1;
        private System.Windows.Forms.CheckBox enableFilteringCheckBox;
        private System.Windows.Forms.Button addFilterButton;
        private System.Windows.Forms.Button deleteFilterButton;
        private System.Windows.Forms.Button moveFilterUpButton;
        private System.Windows.Forms.Button moveFilterDownButton;
        internal UI.FiltersListView filtersListView;
        private System.Windows.Forms.Button prevButton;
        private System.Windows.Forms.Button nextButton;
        private System.Windows.Forms.Button editButton;
        private System.Windows.Forms.ToolTip toolTip1;
    }
}
