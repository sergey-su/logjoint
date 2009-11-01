namespace LogJoint.UI
{
	partial class FilterDialog
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
			this.tabControl1 = new System.Windows.Forms.TabControl();
			this.tabPage3 = new System.Windows.Forms.TabPage();
			this.regExpCheckBox = new System.Windows.Forms.CheckBox();
			this.wholeWordCheckbox = new System.Windows.Forms.CheckBox();
			this.label2 = new System.Windows.Forms.Label();
			this.matchCaseCheckbox = new System.Windows.Forms.CheckBox();
			this.templateTextBox = new System.Windows.Forms.TextBox();
			this.tabPage1 = new System.Windows.Forms.TabPage();
			this.panel3 = new System.Windows.Forms.Panel();
			this.threadsCheckedListBox = new System.Windows.Forms.CheckedListBox();
			this.label3 = new System.Windows.Forms.Label();
			this.tabPage2 = new System.Windows.Forms.TabPage();
			this.panel1 = new System.Windows.Forms.Panel();
			this.messagesTypesCheckedListBox = new System.Windows.Forms.CheckedListBox();
			this.label4 = new System.Windows.Forms.Label();
			this.okButton = new System.Windows.Forms.Button();
			this.cancelButton = new System.Windows.Forms.Button();
			this.tableLayoutPanel1 = new System.Windows.Forms.TableLayoutPanel();
			this.label5 = new System.Windows.Forms.Label();
			this.actionComboBox = new System.Windows.Forms.ComboBox();
			this.nameTextBox = new System.Windows.Forms.TextBox();
			this.panel2 = new System.Windows.Forms.Panel();
			this.label6 = new System.Windows.Forms.Label();
			this.enabledCheckBox = new System.Windows.Forms.CheckBox();
			this.matchFrameContentCheckBox = new System.Windows.Forms.CheckBox();
			this.tabControl1.SuspendLayout();
			this.tabPage3.SuspendLayout();
			this.tabPage1.SuspendLayout();
			this.panel3.SuspendLayout();
			this.tabPage2.SuspendLayout();
			this.panel1.SuspendLayout();
			this.tableLayoutPanel1.SuspendLayout();
			this.panel2.SuspendLayout();
			this.SuspendLayout();
			// 
			// label1
			// 
			this.label1.AutoSize = true;
			this.label1.Dock = System.Windows.Forms.DockStyle.Fill;
			this.label1.Location = new System.Drawing.Point(6, 3);
			this.label1.Name = "label1";
			this.label1.Size = new System.Drawing.Size(251, 25);
			this.label1.TabIndex = 2;
			this.label1.Text = "Rule name:";
			this.label1.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
			// 
			// tabControl1
			// 
			this.tabControl1.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom)
						| System.Windows.Forms.AnchorStyles.Left)
						| System.Windows.Forms.AnchorStyles.Right)));
			this.tableLayoutPanel1.SetColumnSpan(this.tabControl1, 3);
			this.tabControl1.Controls.Add(this.tabPage3);
			this.tabControl1.Controls.Add(this.tabPage1);
			this.tabControl1.Controls.Add(this.tabPage2);
			this.tabControl1.Location = new System.Drawing.Point(6, 81);
			this.tabControl1.Margin = new System.Windows.Forms.Padding(3, 6, 3, 3);
			this.tabControl1.Name = "tabControl1";
			this.tabControl1.SelectedIndex = 0;
			this.tabControl1.Size = new System.Drawing.Size(541, 241);
			this.tabControl1.TabIndex = 3;
			// 
			// tabPage3
			// 
			this.tabPage3.Controls.Add(this.regExpCheckBox);
			this.tabPage3.Controls.Add(this.wholeWordCheckbox);
			this.tabPage3.Controls.Add(this.label2);
			this.tabPage3.Controls.Add(this.matchCaseCheckbox);
			this.tabPage3.Controls.Add(this.templateTextBox);
			this.tabPage3.Location = new System.Drawing.Point(4, 22);
			this.tabPage3.Name = "tabPage3";
			this.tabPage3.Padding = new System.Windows.Forms.Padding(3);
			this.tabPage3.Size = new System.Drawing.Size(533, 215);
			this.tabPage3.TabIndex = 2;
			this.tabPage3.Text = "Text criterian";
			this.tabPage3.UseVisualStyleBackColor = true;
			// 
			// regExpCheckBox
			// 
			this.regExpCheckBox.AutoSize = true;
			this.regExpCheckBox.Location = new System.Drawing.Point(244, 56);
			this.regExpCheckBox.Margin = new System.Windows.Forms.Padding(2);
			this.regExpCheckBox.Name = "regExpCheckBox";
			this.regExpCheckBox.Size = new System.Drawing.Size(63, 17);
			this.regExpCheckBox.TabIndex = 27;
			this.regExpCheckBox.Text = "Regexp";
			this.regExpCheckBox.UseVisualStyleBackColor = true;
			// 
			// wholeWordCheckbox
			// 
			this.wholeWordCheckbox.AutoSize = true;
			this.wholeWordCheckbox.Location = new System.Drawing.Point(130, 56);
			this.wholeWordCheckbox.Margin = new System.Windows.Forms.Padding(2);
			this.wholeWordCheckbox.Name = "wholeWordCheckbox";
			this.wholeWordCheckbox.Size = new System.Drawing.Size(83, 17);
			this.wholeWordCheckbox.TabIndex = 25;
			this.wholeWordCheckbox.Text = "Whole word";
			this.wholeWordCheckbox.UseVisualStyleBackColor = true;
			// 
			// label2
			// 
			this.label2.AutoSize = true;
			this.label2.Location = new System.Drawing.Point(5, 6);
			this.label2.Name = "label2";
			this.label2.Size = new System.Drawing.Size(198, 13);
			this.label2.TabIndex = 2;
			this.label2.Text = "Match the messages containig this text:";
			// 
			// matchCaseCheckbox
			// 
			this.matchCaseCheckbox.AutoSize = true;
			this.matchCaseCheckbox.Location = new System.Drawing.Point(19, 56);
			this.matchCaseCheckbox.Margin = new System.Windows.Forms.Padding(2);
			this.matchCaseCheckbox.Name = "matchCaseCheckbox";
			this.matchCaseCheckbox.Size = new System.Drawing.Size(80, 17);
			this.matchCaseCheckbox.TabIndex = 24;
			this.matchCaseCheckbox.Text = "Match case";
			this.matchCaseCheckbox.UseVisualStyleBackColor = true;
			// 
			// templateTextBox
			// 
			this.templateTextBox.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
						| System.Windows.Forms.AnchorStyles.Right)));
			this.templateTextBox.Location = new System.Drawing.Point(9, 26);
			this.templateTextBox.Name = "templateTextBox";
			this.templateTextBox.Size = new System.Drawing.Size(510, 21);
			this.templateTextBox.TabIndex = 3;
			// 
			// tabPage1
			// 
			this.tabPage1.Controls.Add(this.panel3);
			this.tabPage1.Controls.Add(this.label3);
			this.tabPage1.Location = new System.Drawing.Point(4, 22);
			this.tabPage1.Name = "tabPage1";
			this.tabPage1.Padding = new System.Windows.Forms.Padding(3);
			this.tabPage1.Size = new System.Drawing.Size(533, 215);
			this.tabPage1.TabIndex = 0;
			this.tabPage1.Text = "Threads criterian";
			this.tabPage1.UseVisualStyleBackColor = true;
			// 
			// panel3
			// 
			this.panel3.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom)
						| System.Windows.Forms.AnchorStyles.Left)
						| System.Windows.Forms.AnchorStyles.Right)));
			this.panel3.Controls.Add(this.threadsCheckedListBox);
			this.panel3.Location = new System.Drawing.Point(-1, 26);
			this.panel3.Name = "panel3";
			this.panel3.Padding = new System.Windows.Forms.Padding(4);
			this.panel3.Size = new System.Drawing.Size(534, 187);
			this.panel3.TabIndex = 8;
			// 
			// threadsCheckedListBox
			// 
			this.threadsCheckedListBox.CheckOnClick = true;
			this.threadsCheckedListBox.Dock = System.Windows.Forms.DockStyle.Fill;
			this.threadsCheckedListBox.FormattingEnabled = true;
			this.threadsCheckedListBox.HorizontalExtent = 1000;
			this.threadsCheckedListBox.HorizontalScrollbar = true;
			this.threadsCheckedListBox.IntegralHeight = false;
			this.threadsCheckedListBox.Location = new System.Drawing.Point(4, 4);
			this.threadsCheckedListBox.Name = "threadsCheckedListBox";
			this.threadsCheckedListBox.Size = new System.Drawing.Size(526, 179);
			this.threadsCheckedListBox.TabIndex = 7;
			this.threadsCheckedListBox.ItemCheck += new System.Windows.Forms.ItemCheckEventHandler(this.threadsCheckedListBox_ItemCheck);
			// 
			// label3
			// 
			this.label3.AutoSize = true;
			this.label3.Location = new System.Drawing.Point(5, 6);
			this.label3.Name = "label3";
			this.label3.Size = new System.Drawing.Size(204, 13);
			this.label3.TabIndex = 2;
			this.label3.Text = "Match the messages from these threads:";
			// 
			// tabPage2
			// 
			this.tabPage2.Controls.Add(this.matchFrameContentCheckBox);
			this.tabPage2.Controls.Add(this.panel1);
			this.tabPage2.Controls.Add(this.label4);
			this.tabPage2.Location = new System.Drawing.Point(4, 22);
			this.tabPage2.Name = "tabPage2";
			this.tabPage2.Padding = new System.Windows.Forms.Padding(3);
			this.tabPage2.Size = new System.Drawing.Size(533, 215);
			this.tabPage2.TabIndex = 1;
			this.tabPage2.Text = "Types criterian";
			this.tabPage2.UseVisualStyleBackColor = true;
			// 
			// panel1
			// 
			this.panel1.Controls.Add(this.messagesTypesCheckedListBox);
			this.panel1.Location = new System.Drawing.Point(8, 24);
			this.panel1.Name = "panel1";
			this.panel1.Size = new System.Drawing.Size(214, 103);
			this.panel1.TabIndex = 4;
			// 
			// messagesTypesCheckedListBox
			// 
			this.messagesTypesCheckedListBox.CheckOnClick = true;
			this.messagesTypesCheckedListBox.Dock = System.Windows.Forms.DockStyle.Fill;
			this.messagesTypesCheckedListBox.FormattingEnabled = true;
			this.messagesTypesCheckedListBox.IntegralHeight = false;
			this.messagesTypesCheckedListBox.Items.AddRange(new object[] {
            "Errors",
            "Warnings",
            "Infos",
            "Frames"});
			this.messagesTypesCheckedListBox.Location = new System.Drawing.Point(0, 0);
			this.messagesTypesCheckedListBox.Name = "messagesTypesCheckedListBox";
			this.messagesTypesCheckedListBox.Size = new System.Drawing.Size(214, 103);
			this.messagesTypesCheckedListBox.TabIndex = 0;
			this.messagesTypesCheckedListBox.ItemCheck += new System.Windows.Forms.ItemCheckEventHandler(this.messagesTypesCheckedListBox_ItemCheck);
			// 
			// label4
			// 
			this.label4.AutoSize = true;
			this.label4.Location = new System.Drawing.Point(5, 6);
			this.label4.Name = "label4";
			this.label4.Size = new System.Drawing.Size(182, 13);
			this.label4.TabIndex = 3;
			this.label4.Text = "Match the messages of these types:";
			// 
			// okButton
			// 
			this.okButton.DialogResult = System.Windows.Forms.DialogResult.OK;
			this.okButton.Location = new System.Drawing.Point(0, 3);
			this.okButton.Name = "okButton";
			this.okButton.Size = new System.Drawing.Size(75, 23);
			this.okButton.TabIndex = 4;
			this.okButton.Text = "OK";
			this.okButton.UseVisualStyleBackColor = true;
			// 
			// cancelButton
			// 
			this.cancelButton.DialogResult = System.Windows.Forms.DialogResult.Cancel;
			this.cancelButton.Location = new System.Drawing.Point(0, 33);
			this.cancelButton.Name = "cancelButton";
			this.cancelButton.Size = new System.Drawing.Size(75, 23);
			this.cancelButton.TabIndex = 5;
			this.cancelButton.Text = "Cancel";
			this.cancelButton.UseVisualStyleBackColor = true;
			// 
			// tableLayoutPanel1
			// 
			this.tableLayoutPanel1.ColumnCount = 3;
			this.tableLayoutPanel1.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 57.65957F));
			this.tableLayoutPanel1.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 42.34043F));
			this.tableLayoutPanel1.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle());
			this.tableLayoutPanel1.Controls.Add(this.label1, 0, 0);
			this.tableLayoutPanel1.Controls.Add(this.label5, 0, 1);
			this.tableLayoutPanel1.Controls.Add(this.actionComboBox, 1, 1);
			this.tableLayoutPanel1.Controls.Add(this.nameTextBox, 1, 0);
			this.tableLayoutPanel1.Controls.Add(this.panel2, 2, 0);
			this.tableLayoutPanel1.Controls.Add(this.label6, 0, 2);
			this.tableLayoutPanel1.Controls.Add(this.enabledCheckBox, 1, 2);
			this.tableLayoutPanel1.Controls.Add(this.tabControl1, 0, 3);
			this.tableLayoutPanel1.Dock = System.Windows.Forms.DockStyle.Fill;
			this.tableLayoutPanel1.Location = new System.Drawing.Point(0, 0);
			this.tableLayoutPanel1.Name = "tableLayoutPanel1";
			this.tableLayoutPanel1.Padding = new System.Windows.Forms.Padding(3);
			this.tableLayoutPanel1.RowCount = 4;
			this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle());
			this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle());
			this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle());
			this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
			this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 20F));
			this.tableLayoutPanel1.Size = new System.Drawing.Size(553, 328);
			this.tableLayoutPanel1.TabIndex = 6;
			// 
			// label5
			// 
			this.label5.AutoSize = true;
			this.label5.Dock = System.Windows.Forms.DockStyle.Fill;
			this.label5.Location = new System.Drawing.Point(6, 28);
			this.label5.Name = "label5";
			this.label5.Size = new System.Drawing.Size(251, 25);
			this.label5.TabIndex = 2;
			this.label5.Text = "Action for the messages matching the criteria:";
			this.label5.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
			// 
			// actionComboBox
			// 
			this.actionComboBox.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
			this.actionComboBox.FormattingEnabled = true;
			this.actionComboBox.Items.AddRange(new object[] {
            "Show",
            "Hide"});
			this.actionComboBox.Location = new System.Drawing.Point(262, 30);
			this.actionComboBox.Margin = new System.Windows.Forms.Padding(2);
			this.actionComboBox.Name = "actionComboBox";
			this.actionComboBox.Size = new System.Drawing.Size(135, 21);
			this.actionComboBox.TabIndex = 3;
			// 
			// nameTextBox
			// 
			this.nameTextBox.Dock = System.Windows.Forms.DockStyle.Fill;
			this.nameTextBox.Location = new System.Drawing.Point(262, 5);
			this.nameTextBox.Margin = new System.Windows.Forms.Padding(2);
			this.nameTextBox.Name = "nameTextBox";
			this.nameTextBox.Size = new System.Drawing.Size(185, 21);
			this.nameTextBox.TabIndex = 4;
			// 
			// panel2
			// 
			this.panel2.AutoSize = true;
			this.panel2.Controls.Add(this.okButton);
			this.panel2.Controls.Add(this.cancelButton);
			this.panel2.Location = new System.Drawing.Point(469, 5);
			this.panel2.Margin = new System.Windows.Forms.Padding(20, 2, 2, 5);
			this.panel2.Name = "panel2";
			this.tableLayoutPanel1.SetRowSpan(this.panel2, 3);
			this.panel2.Size = new System.Drawing.Size(78, 59);
			this.panel2.TabIndex = 5;
			// 
			// label6
			// 
			this.label6.AutoSize = true;
			this.label6.Dock = System.Windows.Forms.DockStyle.Fill;
			this.label6.Location = new System.Drawing.Point(6, 53);
			this.label6.Name = "label6";
			this.label6.Size = new System.Drawing.Size(251, 22);
			this.label6.TabIndex = 2;
			this.label6.Text = "Rule is enabled:";
			this.label6.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
			// 
			// enabledCheckBox
			// 
			this.enabledCheckBox.AutoSize = true;
			this.enabledCheckBox.Dock = System.Windows.Forms.DockStyle.Fill;
			this.enabledCheckBox.Location = new System.Drawing.Point(264, 57);
			this.enabledCheckBox.Margin = new System.Windows.Forms.Padding(4);
			this.enabledCheckBox.Name = "enabledCheckBox";
			this.enabledCheckBox.Size = new System.Drawing.Size(181, 14);
			this.enabledCheckBox.TabIndex = 6;
			this.enabledCheckBox.UseVisualStyleBackColor = true;
			// 
			// matchFrameContentCheckBox
			// 
			this.matchFrameContentCheckBox.AutoSize = true;
			this.matchFrameContentCheckBox.Location = new System.Drawing.Point(8, 133);
			this.matchFrameContentCheckBox.Name = "matchFrameContentCheckBox";
			this.matchFrameContentCheckBox.Size = new System.Drawing.Size(269, 17);
			this.matchFrameContentCheckBox.TabIndex = 5;
			this.matchFrameContentCheckBox.Text = "Apply the rule to the content of frames recursively";
			this.matchFrameContentCheckBox.UseVisualStyleBackColor = true;
			// 
			// FilterDialog
			// 
			this.AcceptButton = this.okButton;
			this.AutoScaleDimensions = new System.Drawing.SizeF(96F, 96F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Dpi;
			this.CancelButton = this.cancelButton;
			this.ClientSize = new System.Drawing.Size(553, 328);
			this.Controls.Add(this.tableLayoutPanel1);
			this.Font = new System.Drawing.Font("Tahoma", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(204)));
			this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.SizableToolWindow;
			this.MinimumSize = new System.Drawing.Size(400, 300);
			this.Name = "FilterDialog";
			this.ShowInTaskbar = false;
			this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
			this.Text = "Filter Rule";
			this.tabControl1.ResumeLayout(false);
			this.tabPage3.ResumeLayout(false);
			this.tabPage3.PerformLayout();
			this.tabPage1.ResumeLayout(false);
			this.tabPage1.PerformLayout();
			this.panel3.ResumeLayout(false);
			this.tabPage2.ResumeLayout(false);
			this.tabPage2.PerformLayout();
			this.panel1.ResumeLayout(false);
			this.tableLayoutPanel1.ResumeLayout(false);
			this.tableLayoutPanel1.PerformLayout();
			this.panel2.ResumeLayout(false);
			this.ResumeLayout(false);

		}

		#endregion

		private System.Windows.Forms.Label label1;
		private System.Windows.Forms.TabControl tabControl1;
		private System.Windows.Forms.TabPage tabPage1;
		private System.Windows.Forms.TabPage tabPage2;
		private System.Windows.Forms.TabPage tabPage3;
		private System.Windows.Forms.Label label2;
		private System.Windows.Forms.TextBox templateTextBox;
		private System.Windows.Forms.CheckBox regExpCheckBox;
		private System.Windows.Forms.CheckBox wholeWordCheckbox;
		private System.Windows.Forms.CheckBox matchCaseCheckbox;
		private System.Windows.Forms.Label label3;
		private System.Windows.Forms.CheckedListBox threadsCheckedListBox;
		private System.Windows.Forms.Panel panel3;
		private System.Windows.Forms.Panel panel1;
		private System.Windows.Forms.CheckedListBox messagesTypesCheckedListBox;
		private System.Windows.Forms.Label label4;
		private System.Windows.Forms.Button okButton;
		private System.Windows.Forms.Button cancelButton;
		private System.Windows.Forms.TableLayoutPanel tableLayoutPanel1;
		private System.Windows.Forms.Label label5;
		private System.Windows.Forms.ComboBox actionComboBox;
		private System.Windows.Forms.TextBox nameTextBox;
		private System.Windows.Forms.Panel panel2;
		private System.Windows.Forms.Label label6;
		private System.Windows.Forms.CheckBox enabledCheckBox;
		private System.Windows.Forms.CheckBox matchFrameContentCheckBox;
	}
}