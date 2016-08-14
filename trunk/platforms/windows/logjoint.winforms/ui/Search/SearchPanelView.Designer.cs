namespace LogJoint.UI
{
	partial class SearchPanelView
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
			this.searchInSearchResultsCheckBox = new System.Windows.Forms.CheckBox();
			this.panel3 = new System.Windows.Forms.Panel();
			this.searchMessageTypeCheckBox0 = new System.Windows.Forms.CheckBox();
			this.searchMessageTypeCheckBox3 = new System.Windows.Forms.CheckBox();
			this.searchMessageTypeCheckBox1 = new System.Windows.Forms.CheckBox();
			this.searchMessageTypeCheckBox2 = new System.Windows.Forms.CheckBox();
			this.fromCurrentPositionCheckBox = new System.Windows.Forms.CheckBox();
			this.searchNextMessageRadioButton = new System.Windows.Forms.RadioButton();
			this.searchAllOccurencesRadioButton = new System.Windows.Forms.RadioButton();
			this.searchWithinCurrentThreadCheckbox = new System.Windows.Forms.CheckBox();
			this.doSearchButton = new System.Windows.Forms.Button();
			this.regExpCheckBox = new System.Windows.Forms.CheckBox();
			this.searchUpCheckbox = new System.Windows.Forms.CheckBox();
			this.wholeWordCheckbox = new System.Windows.Forms.CheckBox();
			this.matchCaseCheckbox = new System.Windows.Forms.CheckBox();
			this.searchTextBox = new LogJoint.UI.SearchTextBox();
			this.panel3.SuspendLayout();
			this.SuspendLayout();
			// 
			// searchInSearchResultsCheckBox
			// 
			this.searchInSearchResultsCheckBox.AutoSize = true;
			this.searchInSearchResultsCheckBox.Enabled = false;
			this.searchInSearchResultsCheckBox.Location = new System.Drawing.Point(394, 81);
			this.searchInSearchResultsCheckBox.Margin = new System.Windows.Forms.Padding(2);
			this.searchInSearchResultsCheckBox.Name = "searchInSearchResultsCheckBox";
			this.searchInSearchResultsCheckBox.Size = new System.Drawing.Size(134, 21);
			this.searchInSearchResultsCheckBox.TabIndex = 51;
			this.searchInSearchResultsCheckBox.Text = "In search results";
			this.searchInSearchResultsCheckBox.UseVisualStyleBackColor = true;
			// 
			// panel3
			// 
			this.panel3.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left)));
			this.panel3.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
			this.panel3.Controls.Add(this.searchMessageTypeCheckBox0);
			this.panel3.Controls.Add(this.searchMessageTypeCheckBox3);
			this.panel3.Controls.Add(this.searchMessageTypeCheckBox1);
			this.panel3.Controls.Add(this.searchMessageTypeCheckBox2);
			this.panel3.Location = new System.Drawing.Point(210, 31);
			this.panel3.Margin = new System.Windows.Forms.Padding(0);
			this.panel3.Name = "panel3";
			this.panel3.Size = new System.Drawing.Size(156, 98);
			this.panel3.TabIndex = 47;
			// 
			// searchMessageTypeCheckBox0
			// 
			this.searchMessageTypeCheckBox0.AutoSize = true;
			this.searchMessageTypeCheckBox0.Checked = true;
			this.searchMessageTypeCheckBox0.CheckState = System.Windows.Forms.CheckState.Checked;
			this.searchMessageTypeCheckBox0.Location = new System.Drawing.Point(6, 2);
			this.searchMessageTypeCheckBox0.Margin = new System.Windows.Forms.Padding(0);
			this.searchMessageTypeCheckBox0.Name = "searchMessageTypeCheckBox0";
			this.searchMessageTypeCheckBox0.Size = new System.Drawing.Size(69, 21);
			this.searchMessageTypeCheckBox0.TabIndex = 30;
			this.searchMessageTypeCheckBox0.Text = "Errors";
			this.searchMessageTypeCheckBox0.UseVisualStyleBackColor = true;
			// 
			// searchMessageTypeCheckBox3
			// 
			this.searchMessageTypeCheckBox3.AutoSize = true;
			this.searchMessageTypeCheckBox3.Checked = true;
			this.searchMessageTypeCheckBox3.CheckState = System.Windows.Forms.CheckState.Checked;
			this.searchMessageTypeCheckBox3.Location = new System.Drawing.Point(6, 65);
			this.searchMessageTypeCheckBox3.Margin = new System.Windows.Forms.Padding(0);
			this.searchMessageTypeCheckBox3.Name = "searchMessageTypeCheckBox3";
			this.searchMessageTypeCheckBox3.Size = new System.Drawing.Size(77, 21);
			this.searchMessageTypeCheckBox3.TabIndex = 33;
			this.searchMessageTypeCheckBox3.Text = "Frames";
			this.searchMessageTypeCheckBox3.UseVisualStyleBackColor = true;
			// 
			// searchMessageTypeCheckBox1
			// 
			this.searchMessageTypeCheckBox1.AutoSize = true;
			this.searchMessageTypeCheckBox1.Checked = true;
			this.searchMessageTypeCheckBox1.CheckState = System.Windows.Forms.CheckState.Checked;
			this.searchMessageTypeCheckBox1.Location = new System.Drawing.Point(6, 23);
			this.searchMessageTypeCheckBox1.Margin = new System.Windows.Forms.Padding(0);
			this.searchMessageTypeCheckBox1.Name = "searchMessageTypeCheckBox1";
			this.searchMessageTypeCheckBox1.Size = new System.Drawing.Size(90, 21);
			this.searchMessageTypeCheckBox1.TabIndex = 31;
			this.searchMessageTypeCheckBox1.Text = "Warnings";
			this.searchMessageTypeCheckBox1.UseVisualStyleBackColor = true;
			// 
			// searchMessageTypeCheckBox2
			// 
			this.searchMessageTypeCheckBox2.AutoSize = true;
			this.searchMessageTypeCheckBox2.Checked = true;
			this.searchMessageTypeCheckBox2.CheckState = System.Windows.Forms.CheckState.Checked;
			this.searchMessageTypeCheckBox2.Location = new System.Drawing.Point(6, 44);
			this.searchMessageTypeCheckBox2.Margin = new System.Windows.Forms.Padding(0);
			this.searchMessageTypeCheckBox2.Name = "searchMessageTypeCheckBox2";
			this.searchMessageTypeCheckBox2.Size = new System.Drawing.Size(60, 21);
			this.searchMessageTypeCheckBox2.TabIndex = 32;
			this.searchMessageTypeCheckBox2.Text = "Infos";
			this.searchMessageTypeCheckBox2.UseVisualStyleBackColor = true;
			// 
			// fromCurrentPositionCheckBox
			// 
			this.fromCurrentPositionCheckBox.AutoSize = true;
			this.fromCurrentPositionCheckBox.Location = new System.Drawing.Point(566, 58);
			this.fromCurrentPositionCheckBox.Margin = new System.Windows.Forms.Padding(2);
			this.fromCurrentPositionCheckBox.Name = "fromCurrentPositionCheckBox";
			this.fromCurrentPositionCheckBox.Size = new System.Drawing.Size(167, 21);
			this.fromCurrentPositionCheckBox.TabIndex = 53;
			this.fromCurrentPositionCheckBox.Text = "From current position";
			this.fromCurrentPositionCheckBox.UseVisualStyleBackColor = true;
			this.fromCurrentPositionCheckBox.Visible = true;
			// 
			// searchNextMessageRadioButton
			// 
			this.searchNextMessageRadioButton.AutoSize = true;
			this.searchNextMessageRadioButton.Location = new System.Drawing.Point(373, 34);
			this.searchNextMessageRadioButton.Margin = new System.Windows.Forms.Padding(4);
			this.searchNextMessageRadioButton.Name = "searchNextMessageRadioButton";
			this.searchNextMessageRadioButton.Size = new System.Drawing.Size(116, 21);
			this.searchNextMessageRadioButton.TabIndex = 48;
			this.searchNextMessageRadioButton.Text = "Quick search:";
			this.searchNextMessageRadioButton.UseVisualStyleBackColor = true;
			this.searchNextMessageRadioButton.CheckedChanged += new System.EventHandler(this.searchModeRadioButton_CheckedChanged);
			// 
			// searchAllOccurencesRadioButton
			// 
			this.searchAllOccurencesRadioButton.AutoSize = true;
			this.searchAllOccurencesRadioButton.Checked = true;
			this.searchAllOccurencesRadioButton.Location = new System.Drawing.Point(542, 34);
			this.searchAllOccurencesRadioButton.Margin = new System.Windows.Forms.Padding(4);
			this.searchAllOccurencesRadioButton.Name = "searchAllOccurencesRadioButton";
			this.searchAllOccurencesRadioButton.Size = new System.Drawing.Size(173, 21);
			this.searchAllOccurencesRadioButton.TabIndex = 52;
			this.searchAllOccurencesRadioButton.TabStop = true;
			this.searchAllOccurencesRadioButton.Text = "Search all occurences:";
			this.searchAllOccurencesRadioButton.UseVisualStyleBackColor = true;
			this.searchAllOccurencesRadioButton.CheckedChanged += new System.EventHandler(this.searchModeRadioButton_CheckedChanged);
			// 
			// searchWithinCurrentThreadCheckbox
			// 
			this.searchWithinCurrentThreadCheckbox.AutoSize = true;
			this.searchWithinCurrentThreadCheckbox.Location = new System.Drawing.Point(4, 98);
			this.searchWithinCurrentThreadCheckbox.Margin = new System.Windows.Forms.Padding(2);
			this.searchWithinCurrentThreadCheckbox.Name = "searchWithinCurrentThreadCheckbox";
			this.searchWithinCurrentThreadCheckbox.Size = new System.Drawing.Size(208, 21);
			this.searchWithinCurrentThreadCheckbox.TabIndex = 46;
			this.searchWithinCurrentThreadCheckbox.Text = "Search within current thread";
			this.searchWithinCurrentThreadCheckbox.UseVisualStyleBackColor = true;
			// 
			// doSearchButton
			// 
			this.doSearchButton.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
			this.doSearchButton.Location = new System.Drawing.Point(724, 1);
			this.doSearchButton.Margin = new System.Windows.Forms.Padding(2);
			this.doSearchButton.Name = "doSearchButton";
			this.doSearchButton.Size = new System.Drawing.Size(64, 28);
			this.doSearchButton.TabIndex = 60;
			this.doSearchButton.Text = "Find";
			this.doSearchButton.UseVisualStyleBackColor = true;
			this.doSearchButton.Click += new System.EventHandler(this.doSearchButton_Click);
			// 
			// regExpCheckBox
			// 
			this.regExpCheckBox.AutoSize = true;
			this.regExpCheckBox.Location = new System.Drawing.Point(4, 76);
			this.regExpCheckBox.Margin = new System.Windows.Forms.Padding(2);
			this.regExpCheckBox.Name = "regExpCheckBox";
			this.regExpCheckBox.Size = new System.Drawing.Size(78, 21);
			this.regExpCheckBox.TabIndex = 45;
			this.regExpCheckBox.Text = "Regexp";
			this.regExpCheckBox.UseVisualStyleBackColor = true;
			// 
			// searchUpCheckbox
			// 
			this.searchUpCheckbox.AutoSize = true;
			this.searchUpCheckbox.Location = new System.Drawing.Point(394, 58);
			this.searchUpCheckbox.Margin = new System.Windows.Forms.Padding(2);
			this.searchUpCheckbox.Name = "searchUpCheckbox";
			this.searchUpCheckbox.Size = new System.Drawing.Size(95, 21);
			this.searchUpCheckbox.TabIndex = 49;
			this.searchUpCheckbox.Text = "Search up";
			this.searchUpCheckbox.UseVisualStyleBackColor = true;
			// 
			// wholeWordCheckbox
			// 
			this.wholeWordCheckbox.AutoSize = true;
			this.wholeWordCheckbox.Location = new System.Drawing.Point(4, 54);
			this.wholeWordCheckbox.Margin = new System.Windows.Forms.Padding(2);
			this.wholeWordCheckbox.Name = "wholeWordCheckbox";
			this.wholeWordCheckbox.Size = new System.Drawing.Size(104, 21);
			this.wholeWordCheckbox.TabIndex = 44;
			this.wholeWordCheckbox.Text = "Whole word";
			this.wholeWordCheckbox.UseVisualStyleBackColor = true;
			// 
			// matchCaseCheckbox
			// 
			this.matchCaseCheckbox.AutoSize = true;
			this.matchCaseCheckbox.Location = new System.Drawing.Point(4, 31);
			this.matchCaseCheckbox.Margin = new System.Windows.Forms.Padding(2);
			this.matchCaseCheckbox.Name = "matchCaseCheckbox";
			this.matchCaseCheckbox.Size = new System.Drawing.Size(102, 21);
			this.matchCaseCheckbox.TabIndex = 43;
			this.matchCaseCheckbox.Text = "Match case";
			this.matchCaseCheckbox.UseVisualStyleBackColor = true;
			// 
			// searchTextBox
			// 
			this.searchTextBox.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
			this.searchTextBox.DrawMode = System.Windows.Forms.DrawMode.OwnerDrawFixed;
			this.searchTextBox.FormattingEnabled = true;
			this.searchTextBox.Location = new System.Drawing.Point(4, 3);
			this.searchTextBox.Margin = new System.Windows.Forms.Padding(2);
			this.searchTextBox.Name = "searchTextBox";
			this.searchTextBox.Size = new System.Drawing.Size(718, 23);
			this.searchTextBox.TabIndex = 42;
			this.searchTextBox.DrawItem += new System.Windows.Forms.DrawItemEventHandler(this.searchTextBox_DrawItem);
			this.searchTextBox.SelectedIndexChanged += new System.EventHandler(this.searchTextBox_SelectedIndexChanged);
			// 
			// SearchPanelView
			// 
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Inherit;
			this.Controls.Add(this.searchInSearchResultsCheckBox);
			this.Controls.Add(this.panel3);
			this.Controls.Add(this.fromCurrentPositionCheckBox);
			this.Controls.Add(this.searchNextMessageRadioButton);
			this.Controls.Add(this.searchAllOccurencesRadioButton);
			this.Controls.Add(this.searchWithinCurrentThreadCheckbox);
			this.Controls.Add(this.doSearchButton);
			this.Controls.Add(this.regExpCheckBox);
			this.Controls.Add(this.searchUpCheckbox);
			this.Controls.Add(this.wholeWordCheckbox);
			this.Controls.Add(this.matchCaseCheckbox);
			this.Controls.Add(this.searchTextBox);
			this.Name = "SearchPanelView";
			this.Size = new System.Drawing.Size(790, 129);
			this.panel3.ResumeLayout(false);
			this.panel3.PerformLayout();
			this.ResumeLayout(false);
			this.PerformLayout();

		}

		#endregion

		private System.Windows.Forms.CheckBox searchInSearchResultsCheckBox;
		private System.Windows.Forms.Panel panel3;
		private System.Windows.Forms.CheckBox searchMessageTypeCheckBox0;
		private System.Windows.Forms.CheckBox searchMessageTypeCheckBox3;
		private System.Windows.Forms.CheckBox searchMessageTypeCheckBox1;
		private System.Windows.Forms.CheckBox searchMessageTypeCheckBox2;
		private System.Windows.Forms.CheckBox fromCurrentPositionCheckBox;
		private System.Windows.Forms.RadioButton searchNextMessageRadioButton;
		private System.Windows.Forms.RadioButton searchAllOccurencesRadioButton;
		private System.Windows.Forms.CheckBox searchWithinCurrentThreadCheckbox;
		private System.Windows.Forms.Button doSearchButton;
		private System.Windows.Forms.CheckBox regExpCheckBox;
		private System.Windows.Forms.CheckBox searchUpCheckbox;
		private System.Windows.Forms.CheckBox wholeWordCheckbox;
		private System.Windows.Forms.CheckBox matchCaseCheckbox;
		public SearchTextBox searchTextBox;
	}
}
