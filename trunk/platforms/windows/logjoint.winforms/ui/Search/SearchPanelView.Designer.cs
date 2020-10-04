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
				subscription?.Dispose();
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
			this.fromCurrentPositionCheckBox = new System.Windows.Forms.CheckBox();
			this.searchNextMessageRadioButton = new System.Windows.Forms.RadioButton();
			this.searchAllOccurencesRadioButton = new System.Windows.Forms.RadioButton();
			this.searchWithinCurrentThreadCheckbox = new System.Windows.Forms.CheckBox();
			this.doSearchButton = new System.Windows.Forms.Button();
			this.regExpCheckBox = new System.Windows.Forms.CheckBox();
			this.searchUpCheckbox = new System.Windows.Forms.CheckBox();
			this.wholeWordCheckbox = new System.Windows.Forms.CheckBox();
			this.matchCaseCheckbox = new System.Windows.Forms.CheckBox();
			this.searchTextBox = new LogJoint.UI.QuickSearchTextBox.BorderedQuickSearchTextBox();
			this.searchWithinCurrentLogCheckBox = new System.Windows.Forms.CheckBox();
			this.label1 = new System.Windows.Forms.Label();
			this.currentSuggestionLinkLabel = new System.Windows.Forms.LinkLabel();
			this.SuspendLayout();
			// 
			// searchInSearchResultsCheckBox
			// 
			this.searchInSearchResultsCheckBox.AutoSize = true;
			this.searchInSearchResultsCheckBox.Enabled = false;
			this.searchInSearchResultsCheckBox.Location = new System.Drawing.Point(321, 77);
			this.searchInSearchResultsCheckBox.Margin = new System.Windows.Forms.Padding(2);
			this.searchInSearchResultsCheckBox.Name = "searchInSearchResultsCheckBox";
			this.searchInSearchResultsCheckBox.Size = new System.Drawing.Size(134, 21);
			this.searchInSearchResultsCheckBox.TabIndex = 50;
			this.searchInSearchResultsCheckBox.Text = "In search results";
			this.searchInSearchResultsCheckBox.UseVisualStyleBackColor = true;
			this.searchInSearchResultsCheckBox.CheckedChanged += new System.EventHandler(this.checkableControlCheckedChanged);
			// 
			// fromCurrentPositionCheckBox
			// 
			this.fromCurrentPositionCheckBox.AutoSize = true;
			this.fromCurrentPositionCheckBox.Location = new System.Drawing.Point(493, 54);
			this.fromCurrentPositionCheckBox.Margin = new System.Windows.Forms.Padding(2);
			this.fromCurrentPositionCheckBox.Name = "fromCurrentPositionCheckBox";
			this.fromCurrentPositionCheckBox.Size = new System.Drawing.Size(164, 21);
			this.fromCurrentPositionCheckBox.TabIndex = 52;
			this.fromCurrentPositionCheckBox.Text = "From current position";
			this.fromCurrentPositionCheckBox.UseVisualStyleBackColor = true;
			this.fromCurrentPositionCheckBox.CheckedChanged += new System.EventHandler(this.checkableControlCheckedChanged);
			// 
			// searchNextMessageRadioButton
			// 
			this.searchNextMessageRadioButton.AutoSize = true;
			this.searchNextMessageRadioButton.Location = new System.Drawing.Point(300, 30);
			this.searchNextMessageRadioButton.Margin = new System.Windows.Forms.Padding(4);
			this.searchNextMessageRadioButton.Name = "searchNextMessageRadioButton";
			this.searchNextMessageRadioButton.Size = new System.Drawing.Size(116, 21);
			this.searchNextMessageRadioButton.TabIndex = 48;
			this.searchNextMessageRadioButton.Text = "Quick search:";
			this.searchNextMessageRadioButton.UseVisualStyleBackColor = true;
			this.searchNextMessageRadioButton.CheckedChanged += new System.EventHandler(this.checkableControlCheckedChanged);
			// 
			// searchAllOccurencesRadioButton
			// 
			this.searchAllOccurencesRadioButton.AutoSize = true;
			this.searchAllOccurencesRadioButton.Checked = true;
			this.searchAllOccurencesRadioButton.Location = new System.Drawing.Point(469, 30);
			this.searchAllOccurencesRadioButton.Margin = new System.Windows.Forms.Padding(4);
			this.searchAllOccurencesRadioButton.Name = "searchAllOccurencesRadioButton";
			this.searchAllOccurencesRadioButton.Size = new System.Drawing.Size(173, 21);
			this.searchAllOccurencesRadioButton.TabIndex = 51;
			this.searchAllOccurencesRadioButton.TabStop = true;
			this.searchAllOccurencesRadioButton.Text = "Search all occurences:";
			this.searchAllOccurencesRadioButton.UseVisualStyleBackColor = true;
			this.searchAllOccurencesRadioButton.CheckedChanged += new System.EventHandler(this.checkableControlCheckedChanged);
			// 
			// searchWithinCurrentThreadCheckbox
			// 
			this.searchWithinCurrentThreadCheckbox.AutoSize = true;
			this.searchWithinCurrentThreadCheckbox.Location = new System.Drawing.Point(166, 54);
			this.searchWithinCurrentThreadCheckbox.Margin = new System.Windows.Forms.Padding(2);
			this.searchWithinCurrentThreadCheckbox.Name = "searchWithinCurrentThreadCheckbox";
			this.searchWithinCurrentThreadCheckbox.Size = new System.Drawing.Size(120, 21);
			this.searchWithinCurrentThreadCheckbox.TabIndex = 46;
			this.searchWithinCurrentThreadCheckbox.Text = "current thread";
			this.searchWithinCurrentThreadCheckbox.UseVisualStyleBackColor = true;
			this.searchWithinCurrentThreadCheckbox.CheckedChanged += new System.EventHandler(this.checkableControlCheckedChanged);
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
			this.regExpCheckBox.CheckedChanged += new System.EventHandler(this.checkableControlCheckedChanged);
			// 
			// searchUpCheckbox
			// 
			this.searchUpCheckbox.AutoSize = true;
			this.searchUpCheckbox.Location = new System.Drawing.Point(321, 54);
			this.searchUpCheckbox.Margin = new System.Windows.Forms.Padding(2);
			this.searchUpCheckbox.Name = "searchUpCheckbox";
			this.searchUpCheckbox.Size = new System.Drawing.Size(95, 21);
			this.searchUpCheckbox.TabIndex = 49;
			this.searchUpCheckbox.Text = "Search up";
			this.searchUpCheckbox.UseVisualStyleBackColor = true;
			this.searchUpCheckbox.CheckedChanged += new System.EventHandler(this.checkableControlCheckedChanged);
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
			this.wholeWordCheckbox.CheckedChanged += new System.EventHandler(this.checkableControlCheckedChanged);
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
			this.matchCaseCheckbox.CheckedChanged += new System.EventHandler(this.checkableControlCheckedChanged);
			// 
			// searchTextBox
			// 
			this.searchTextBox.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
			this.searchTextBox.Location = new System.Drawing.Point(4, 3);
			this.searchTextBox.Margin = new System.Windows.Forms.Padding(2);
			this.searchTextBox.Name = "searchTextBox";
			this.searchTextBox.Size = new System.Drawing.Size(718, 23);
			this.searchTextBox.TabIndex = 42;
			// 
			// searchWithinCurrentLogCheckBox
			// 
			this.searchWithinCurrentLogCheckBox.AutoSize = true;
			this.searchWithinCurrentLogCheckBox.Location = new System.Drawing.Point(166, 77);
			this.searchWithinCurrentLogCheckBox.Margin = new System.Windows.Forms.Padding(2);
			this.searchWithinCurrentLogCheckBox.Name = "searchWithinCurrentLogCheckBox";
			this.searchWithinCurrentLogCheckBox.Size = new System.Drawing.Size(98, 21);
			this.searchWithinCurrentLogCheckBox.TabIndex = 47;
			this.searchWithinCurrentLogCheckBox.Text = "current log";
			this.searchWithinCurrentLogCheckBox.UseVisualStyleBackColor = true;
			this.searchWithinCurrentLogCheckBox.CheckedChanged += new System.EventHandler(this.checkableControlCheckedChanged);
			// 
			// label1
			// 
			this.label1.AutoSize = true;
			this.label1.Location = new System.Drawing.Point(148, 32);
			this.label1.Name = "label1";
			this.label1.Size = new System.Drawing.Size(96, 17);
			this.label1.TabIndex = 62;
			this.label1.Text = "Search within:";
			// 
			// currentSuggestionLinkLabel
			// 
			this.currentSuggestionLinkLabel.AutoSize = true;
			this.currentSuggestionLinkLabel.Location = new System.Drawing.Point(690, 30);
			this.currentSuggestionLinkLabel.Name = "currentSuggestionLinkLabel";
			this.currentSuggestionLinkLabel.Size = new System.Drawing.Size(96, 17);
			this.currentSuggestionLinkLabel.TabIndex = 67;
			this.currentSuggestionLinkLabel.Visible = false;
			this.currentSuggestionLinkLabel.Text = "";
			this.currentSuggestionLinkLabel.LinkClicked += new System.Windows.Forms.LinkLabelLinkClickedEventHandler(this.currentSuggestionLinkLabel_LinkClicked);
			// 
			// SearchPanelView
			// 
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Inherit;
			this.Controls.Add(this.label1);
			this.Controls.Add(this.searchWithinCurrentLogCheckBox);
			this.Controls.Add(this.searchInSearchResultsCheckBox);
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
			this.Controls.Add(this.currentSuggestionLinkLabel);
			this.Name = "SearchPanelView";
			this.Size = new System.Drawing.Size(790, 136);
			this.ResumeLayout(false);
			this.PerformLayout();

		}

		#endregion

		private System.Windows.Forms.CheckBox searchInSearchResultsCheckBox;
		private System.Windows.Forms.CheckBox fromCurrentPositionCheckBox;
		private System.Windows.Forms.RadioButton searchNextMessageRadioButton;
		private System.Windows.Forms.RadioButton searchAllOccurencesRadioButton;
		private System.Windows.Forms.CheckBox searchWithinCurrentThreadCheckbox;
		private System.Windows.Forms.Button doSearchButton;
		private System.Windows.Forms.CheckBox regExpCheckBox;
		private System.Windows.Forms.CheckBox searchUpCheckbox;
		private System.Windows.Forms.CheckBox wholeWordCheckbox;
		private System.Windows.Forms.CheckBox matchCaseCheckbox;
		public LogJoint.UI.QuickSearchTextBox.BorderedQuickSearchTextBox searchTextBox;
		private System.Windows.Forms.CheckBox searchWithinCurrentLogCheckBox;
		private System.Windows.Forms.Label label1;
		private System.Windows.Forms.LinkLabel currentSuggestionLinkLabel;
	}
}
