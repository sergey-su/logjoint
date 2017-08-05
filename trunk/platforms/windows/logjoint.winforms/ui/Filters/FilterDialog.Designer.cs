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
			this.nameLinkLabel = new System.Windows.Forms.LinkLabel();
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
			this.label1.Location = new System.Drawing.Point(8, 4);
			this.label1.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
			this.label1.Name = "label1";
			this.label1.Size = new System.Drawing.Size(102, 28);
			this.label1.TabIndex = 2;
			this.label1.Text = "Rule name:";
			this.label1.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
			// 
			// tabControl1
			// 
			this.tabControl1.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
			this.tableLayoutPanel1.SetColumnSpan(this.tabControl1, 4);
			this.tabControl1.Controls.Add(this.tabPage3);
			this.tabControl1.Controls.Add(this.tabPage1);
			this.tabControl1.Controls.Add(this.tabPage2);
			this.tabControl1.Location = new System.Drawing.Point(8, 97);
			this.tabControl1.Margin = new System.Windows.Forms.Padding(4, 8, 4, 4);
			this.tabControl1.Name = "tabControl1";
			this.tabControl1.SelectedIndex = 0;
			this.tabControl1.Size = new System.Drawing.Size(675, 305);
			this.tabControl1.TabIndex = 13;
			// 
			// tabPage3
			// 
			this.tabPage3.Controls.Add(this.regExpCheckBox);
			this.tabPage3.Controls.Add(this.wholeWordCheckbox);
			this.tabPage3.Controls.Add(this.label2);
			this.tabPage3.Controls.Add(this.matchCaseCheckbox);
			this.tabPage3.Controls.Add(this.templateTextBox);
			this.tabPage3.Location = new System.Drawing.Point(4, 26);
			this.tabPage3.Margin = new System.Windows.Forms.Padding(4);
			this.tabPage3.Name = "tabPage3";
			this.tabPage3.Padding = new System.Windows.Forms.Padding(4);
			this.tabPage3.Size = new System.Drawing.Size(667, 275);
			this.tabPage3.TabIndex = 2;
			this.tabPage3.Text = "Text criterion";
			this.tabPage3.UseVisualStyleBackColor = true;
			// 
			// regExpCheckBox
			// 
			this.regExpCheckBox.AutoSize = true;
			this.regExpCheckBox.Location = new System.Drawing.Point(305, 70);
			this.regExpCheckBox.Margin = new System.Windows.Forms.Padding(2);
			this.regExpCheckBox.Name = "regExpCheckBox";
			this.regExpCheckBox.Size = new System.Drawing.Size(77, 21);
			this.regExpCheckBox.TabIndex = 27;
			this.regExpCheckBox.Text = "Regexp";
			this.regExpCheckBox.UseVisualStyleBackColor = true;
			this.regExpCheckBox.CheckedChanged += new System.EventHandler(this.criteriaInputChanged);
			// 
			// wholeWordCheckbox
			// 
			this.wholeWordCheckbox.AutoSize = true;
			this.wholeWordCheckbox.Location = new System.Drawing.Point(162, 70);
			this.wholeWordCheckbox.Margin = new System.Windows.Forms.Padding(2);
			this.wholeWordCheckbox.Name = "wholeWordCheckbox";
			this.wholeWordCheckbox.Size = new System.Drawing.Size(104, 21);
			this.wholeWordCheckbox.TabIndex = 25;
			this.wholeWordCheckbox.Text = "Whole word";
			this.wholeWordCheckbox.UseVisualStyleBackColor = true;
			this.wholeWordCheckbox.CheckedChanged += new System.EventHandler(this.criteriaInputChanged);
			// 
			// label2
			// 
			this.label2.AutoSize = true;
			this.label2.Location = new System.Drawing.Point(6, 8);
			this.label2.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
			this.label2.Name = "label2";
			this.label2.Size = new System.Drawing.Size(250, 17);
			this.label2.TabIndex = 2;
			this.label2.Text = "Match the messages containig this text:";
			// 
			// matchCaseCheckbox
			// 
			this.matchCaseCheckbox.AutoSize = true;
			this.matchCaseCheckbox.Location = new System.Drawing.Point(24, 70);
			this.matchCaseCheckbox.Margin = new System.Windows.Forms.Padding(2);
			this.matchCaseCheckbox.Name = "matchCaseCheckbox";
			this.matchCaseCheckbox.Size = new System.Drawing.Size(98, 21);
			this.matchCaseCheckbox.TabIndex = 24;
			this.matchCaseCheckbox.Text = "Match case";
			this.matchCaseCheckbox.UseVisualStyleBackColor = true;
			this.matchCaseCheckbox.CheckedChanged += new System.EventHandler(this.criteriaInputChanged);
			// 
			// templateTextBox
			// 
			this.templateTextBox.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
			this.templateTextBox.Location = new System.Drawing.Point(11, 32);
			this.templateTextBox.Margin = new System.Windows.Forms.Padding(4);
			this.templateTextBox.Name = "templateTextBox";
			this.templateTextBox.Size = new System.Drawing.Size(635, 24);
			this.templateTextBox.TabIndex = 3;
			this.templateTextBox.TextChanged += new System.EventHandler(this.criteriaInputChanged);
			// 
			// tabPage1
			// 
			this.tabPage1.Controls.Add(this.panel3);
			this.tabPage1.Controls.Add(this.label3);
			this.tabPage1.Location = new System.Drawing.Point(4, 26);
			this.tabPage1.Margin = new System.Windows.Forms.Padding(4);
			this.tabPage1.Name = "tabPage1";
			this.tabPage1.Padding = new System.Windows.Forms.Padding(4);
			this.tabPage1.Size = new System.Drawing.Size(667, 278);
			this.tabPage1.TabIndex = 0;
			this.tabPage1.Text = "Threads criterion";
			this.tabPage1.UseVisualStyleBackColor = true;
			// 
			// panel3
			// 
			this.panel3.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
			this.panel3.Controls.Add(this.threadsCheckedListBox);
			this.panel3.Location = new System.Drawing.Point(-1, 32);
			this.panel3.Margin = new System.Windows.Forms.Padding(4);
			this.panel3.Name = "panel3";
			this.panel3.Padding = new System.Windows.Forms.Padding(5);
			this.panel3.Size = new System.Drawing.Size(667, 255);
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
			this.threadsCheckedListBox.Location = new System.Drawing.Point(5, 5);
			this.threadsCheckedListBox.Margin = new System.Windows.Forms.Padding(4);
			this.threadsCheckedListBox.Name = "threadsCheckedListBox";
			this.threadsCheckedListBox.Size = new System.Drawing.Size(657, 245);
			this.threadsCheckedListBox.TabIndex = 7;
			this.threadsCheckedListBox.ItemCheck += new System.Windows.Forms.ItemCheckEventHandler(this.threadsCheckedListBox_ItemCheck);
			// 
			// label3
			// 
			this.label3.AutoSize = true;
			this.label3.Location = new System.Drawing.Point(6, 8);
			this.label3.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
			this.label3.Name = "label3";
			this.label3.Size = new System.Drawing.Size(257, 17);
			this.label3.TabIndex = 2;
			this.label3.Text = "Match the messages from these threads:";
			// 
			// tabPage2
			// 
			this.tabPage2.Controls.Add(this.panel1);
			this.tabPage2.Controls.Add(this.label4);
			this.tabPage2.Location = new System.Drawing.Point(4, 26);
			this.tabPage2.Margin = new System.Windows.Forms.Padding(4);
			this.tabPage2.Name = "tabPage2";
			this.tabPage2.Padding = new System.Windows.Forms.Padding(4);
			this.tabPage2.Size = new System.Drawing.Size(667, 278);
			this.tabPage2.TabIndex = 1;
			this.tabPage2.Text = "Types criterion";
			this.tabPage2.UseVisualStyleBackColor = true;
			// 
			// panel1
			// 
			this.panel1.Controls.Add(this.messagesTypesCheckedListBox);
			this.panel1.Location = new System.Drawing.Point(10, 30);
			this.panel1.Margin = new System.Windows.Forms.Padding(4);
			this.panel1.Name = "panel1";
			this.panel1.Size = new System.Drawing.Size(268, 129);
			this.panel1.TabIndex = 4;
			// 
			// messagesTypesCheckedListBox
			// 
			this.messagesTypesCheckedListBox.CheckOnClick = true;
			this.messagesTypesCheckedListBox.Dock = System.Windows.Forms.DockStyle.Fill;
			this.messagesTypesCheckedListBox.FormattingEnabled = true;
			this.messagesTypesCheckedListBox.IntegralHeight = false;
			this.messagesTypesCheckedListBox.Location = new System.Drawing.Point(0, 0);
			this.messagesTypesCheckedListBox.Margin = new System.Windows.Forms.Padding(4);
			this.messagesTypesCheckedListBox.Name = "messagesTypesCheckedListBox";
			this.messagesTypesCheckedListBox.Size = new System.Drawing.Size(268, 129);
			this.messagesTypesCheckedListBox.TabIndex = 0;
			this.messagesTypesCheckedListBox.ItemCheck += new System.Windows.Forms.ItemCheckEventHandler(this.messagesTypesCheckedListBox_ItemCheck);
			// 
			// label4
			// 
			this.label4.AutoSize = true;
			this.label4.Location = new System.Drawing.Point(6, 8);
			this.label4.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
			this.label4.Name = "label4";
			this.label4.Size = new System.Drawing.Size(228, 17);
			this.label4.TabIndex = 3;
			this.label4.Text = "Match the messages of these types:";
			// 
			// okButton
			// 
			this.okButton.DialogResult = System.Windows.Forms.DialogResult.OK;
			this.okButton.Location = new System.Drawing.Point(0, 4);
			this.okButton.Margin = new System.Windows.Forms.Padding(4);
			this.okButton.Name = "okButton";
			this.okButton.Size = new System.Drawing.Size(94, 29);
			this.okButton.TabIndex = 30;
			this.okButton.Text = "OK";
			this.okButton.UseVisualStyleBackColor = true;
			// 
			// cancelButton
			// 
			this.cancelButton.DialogResult = System.Windows.Forms.DialogResult.Cancel;
			this.cancelButton.Location = new System.Drawing.Point(0, 41);
			this.cancelButton.Margin = new System.Windows.Forms.Padding(4);
			this.cancelButton.Name = "cancelButton";
			this.cancelButton.Size = new System.Drawing.Size(94, 29);
			this.cancelButton.TabIndex = 31;
			this.cancelButton.Text = "Cancel";
			this.cancelButton.UseVisualStyleBackColor = true;
			// 
			// tableLayoutPanel1
			// 
			this.tableLayoutPanel1.ColumnCount = 4;
			this.tableLayoutPanel1.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle());
			this.tableLayoutPanel1.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
			this.tableLayoutPanel1.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle());
			this.tableLayoutPanel1.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle());
			this.tableLayoutPanel1.Controls.Add(this.label1, 0, 0);
			this.tableLayoutPanel1.Controls.Add(this.label5, 0, 1);
			this.tableLayoutPanel1.Controls.Add(this.actionComboBox, 1, 1);
			this.tableLayoutPanel1.Controls.Add(this.nameTextBox, 1, 0);
			this.tableLayoutPanel1.Controls.Add(this.panel2, 3, 0);
			this.tableLayoutPanel1.Controls.Add(this.label6, 0, 2);
			this.tableLayoutPanel1.Controls.Add(this.enabledCheckBox, 1, 2);
			this.tableLayoutPanel1.Controls.Add(this.tabControl1, 0, 3);
			this.tableLayoutPanel1.Controls.Add(this.nameLinkLabel, 2, 0);
			this.tableLayoutPanel1.Dock = System.Windows.Forms.DockStyle.Fill;
			this.tableLayoutPanel1.Location = new System.Drawing.Point(0, 0);
			this.tableLayoutPanel1.Margin = new System.Windows.Forms.Padding(4);
			this.tableLayoutPanel1.Name = "tableLayoutPanel1";
			this.tableLayoutPanel1.Padding = new System.Windows.Forms.Padding(4);
			this.tableLayoutPanel1.RowCount = 4;
			this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle());
			this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle());
			this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle());
			this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
			this.tableLayoutPanel1.Size = new System.Drawing.Size(691, 410);
			this.tableLayoutPanel1.TabIndex = 6;
			// 
			// label5
			// 
			this.label5.AutoSize = true;
			this.label5.Dock = System.Windows.Forms.DockStyle.Fill;
			this.label5.Location = new System.Drawing.Point(8, 32);
			this.label5.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
			this.label5.Name = "label5";
			this.label5.Size = new System.Drawing.Size(102, 29);
			this.label5.TabIndex = 2;
			this.label5.Text = "Action:";
			this.label5.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
			// 
			// actionComboBox
			// 
			this.tableLayoutPanel1.SetColumnSpan(this.actionComboBox, 2);
			this.actionComboBox.Dock = System.Windows.Forms.DockStyle.Top;
			this.actionComboBox.DrawMode = System.Windows.Forms.DrawMode.OwnerDrawFixed;
			this.actionComboBox.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
			this.actionComboBox.FormattingEnabled = true;
			this.actionComboBox.Location = new System.Drawing.Point(116, 34);
			this.actionComboBox.Margin = new System.Windows.Forms.Padding(2);
			this.actionComboBox.Name = "actionComboBox";
			this.actionComboBox.Size = new System.Drawing.Size(444, 25);
			this.actionComboBox.TabIndex = 11;
			this.actionComboBox.DrawItem += new System.Windows.Forms.DrawItemEventHandler(this.ActionComboBox_DrawItem);
			// 
			// nameTextBox
			// 
			this.nameTextBox.Dock = System.Windows.Forms.DockStyle.Fill;
			this.nameTextBox.Location = new System.Drawing.Point(116, 6);
			this.nameTextBox.Margin = new System.Windows.Forms.Padding(2);
			this.nameTextBox.Name = "nameTextBox";
			this.nameTextBox.Size = new System.Drawing.Size(341, 24);
			this.nameTextBox.TabIndex = 10;
			// 
			// panel2
			// 
			this.panel2.AutoSize = true;
			this.panel2.Controls.Add(this.okButton);
			this.panel2.Controls.Add(this.cancelButton);
			this.panel2.Location = new System.Drawing.Point(587, 6);
			this.panel2.Margin = new System.Windows.Forms.Padding(25, 2, 2, 6);
			this.panel2.Name = "panel2";
			this.tableLayoutPanel1.SetRowSpan(this.panel2, 3);
			this.panel2.Size = new System.Drawing.Size(98, 74);
			this.panel2.TabIndex = 15;
			// 
			// label6
			// 
			this.label6.AutoSize = true;
			this.label6.Dock = System.Windows.Forms.DockStyle.Fill;
			this.label6.Location = new System.Drawing.Point(8, 61);
			this.label6.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
			this.label6.Name = "label6";
			this.label6.Size = new System.Drawing.Size(102, 28);
			this.label6.TabIndex = 2;
			this.label6.Text = "Rule is enabled:";
			this.label6.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
			// 
			// enabledCheckBox
			// 
			this.enabledCheckBox.Dock = System.Windows.Forms.DockStyle.Fill;
			this.enabledCheckBox.Location = new System.Drawing.Point(119, 66);
			this.enabledCheckBox.Margin = new System.Windows.Forms.Padding(5);
			this.enabledCheckBox.Name = "enabledCheckBox";
			this.enabledCheckBox.Size = new System.Drawing.Size(335, 18);
			this.enabledCheckBox.TabIndex = 12;
			this.enabledCheckBox.Text = "         ";
			this.enabledCheckBox.UseVisualStyleBackColor = true;
			// 
			// nameLinkLabel
			// 
			this.nameLinkLabel.AutoSize = true;
			this.nameLinkLabel.Location = new System.Drawing.Point(462, 9);
			this.nameLinkLabel.Margin = new System.Windows.Forms.Padding(3, 5, 3, 0);
			this.nameLinkLabel.Name = "nameLinkLabel";
			this.nameLinkLabel.Size = new System.Drawing.Size(97, 17);
			this.nameLinkLabel.TabIndex = 16;
			this.nameLinkLabel.TabStop = true;
			this.nameLinkLabel.Text = "nameLinkLabel";
			this.nameLinkLabel.LinkClicked += new System.Windows.Forms.LinkLabelLinkClickedEventHandler(this.nameLinkLabel_LinkClicked);
			// 
			// FilterDialog
			// 
			this.AcceptButton = this.okButton;
			this.AutoScaleDimensions = new System.Drawing.SizeF(120F, 120F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Dpi;
			this.CancelButton = this.cancelButton;
			this.ClientSize = new System.Drawing.Size(691, 410);
			this.Controls.Add(this.tableLayoutPanel1);
			this.Font = new System.Drawing.Font("Tahoma", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(204)));
			this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.SizableToolWindow;
			this.Margin = new System.Windows.Forms.Padding(4);
			this.MinimumSize = new System.Drawing.Size(496, 363);
			this.Name = "FilterDialog";
			this.ShowInTaskbar = false;
			this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
			this.Shown += new System.EventHandler(this.FilterDialog_Shown);
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
		public System.Windows.Forms.TextBox templateTextBox;
		public System.Windows.Forms.CheckBox regExpCheckBox;
		public System.Windows.Forms.CheckBox wholeWordCheckbox;
		public System.Windows.Forms.CheckBox matchCaseCheckbox;
		private System.Windows.Forms.Label label3;
		public System.Windows.Forms.CheckedListBox threadsCheckedListBox;
		private System.Windows.Forms.Panel panel3;
		private System.Windows.Forms.Panel panel1;
		public System.Windows.Forms.CheckedListBox messagesTypesCheckedListBox;
		private System.Windows.Forms.Label label4;
		private System.Windows.Forms.Button okButton;
		private System.Windows.Forms.Button cancelButton;
		private System.Windows.Forms.TableLayoutPanel tableLayoutPanel1;
		private System.Windows.Forms.Label label5;
		public System.Windows.Forms.ComboBox actionComboBox;
		public System.Windows.Forms.TextBox nameTextBox;
		private System.Windows.Forms.Panel panel2;
		private System.Windows.Forms.Label label6;
		public System.Windows.Forms.CheckBox enabledCheckBox;
		internal System.Windows.Forms.LinkLabel nameLinkLabel;
	}
}