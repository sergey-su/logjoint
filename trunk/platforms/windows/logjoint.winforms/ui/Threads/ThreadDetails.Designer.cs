namespace LogJoint.UI
{
	partial class ThreadPropertiesForm
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
			this.label2 = new System.Windows.Forms.Label();
			this.label4 = new System.Windows.Forms.Label();
			this.label5 = new System.Windows.Forms.Label();
			this.label6 = new System.Windows.Forms.Label();
			this.label7 = new System.Windows.Forms.Label();
			this.tableLayoutPanel1 = new System.Windows.Forms.TableLayoutPanel();
			this.flowLayoutPanel1 = new System.Windows.Forms.FlowLayoutPanel();
			this.colorPanel = new System.Windows.Forms.Panel();
			this.changeColorLinkLabel = new System.Windows.Forms.LinkLabel();
			this.idTextBox = new System.Windows.Forms.TextBox();
			this.logSourceLink = new System.Windows.Forms.LinkLabel();
			this.visibleCheckBox = new System.Windows.Forms.CheckBox();
			this.lastMessageLinkLabel = new System.Windows.Forms.LinkLabel();
			this.firstMessageLinkLabel = new System.Windows.Forms.LinkLabel();
			this.nameTextBox = new System.Windows.Forms.TextBox();
			this.label8 = new System.Windows.Forms.Label();
			this.button1 = new System.Windows.Forms.Button();
			this.tableLayoutPanel1.SuspendLayout();
			this.flowLayoutPanel1.SuspendLayout();
			this.SuspendLayout();
			// 
			// label1
			// 
			this.label1.AutoSize = true;
			this.label1.Location = new System.Drawing.Point(3, 0);
			this.label1.Name = "label1";
			this.label1.Padding = new System.Windows.Forms.Padding(0, 3, 0, 3);
			this.label1.Size = new System.Drawing.Size(22, 19);
			this.label1.TabIndex = 0;
			this.label1.Text = "ID:";
			// 
			// label2
			// 
			this.label2.AutoSize = true;
			this.label2.Location = new System.Drawing.Point(3, 20);
			this.label2.Name = "label2";
			this.label2.Padding = new System.Windows.Forms.Padding(0, 3, 0, 3);
			this.label2.Size = new System.Drawing.Size(64, 19);
			this.label2.TabIndex = 0;
			this.label2.Text = "Description:";
			// 
			// label4
			// 
			this.label4.AutoSize = true;
			this.label4.Location = new System.Drawing.Point(3, 40);
			this.label4.Name = "label4";
			this.label4.Padding = new System.Windows.Forms.Padding(0, 3, 0, 3);
			this.label4.Size = new System.Drawing.Size(71, 19);
			this.label4.TabIndex = 0;
			this.label4.Text = "Display color:";
			// 
			// label5
			// 
			this.label5.AutoSize = true;
			this.label5.Location = new System.Drawing.Point(3, 78);
			this.label5.Name = "label5";
			this.label5.Padding = new System.Windows.Forms.Padding(0, 3, 0, 3);
			this.label5.Size = new System.Drawing.Size(107, 19);
			this.label5.TabIndex = 0;
			this.label5.Text = "First known message";
			// 
			// label6
			// 
			this.label6.AutoSize = true;
			this.label6.Location = new System.Drawing.Point(3, 97);
			this.label6.Name = "label6";
			this.label6.Padding = new System.Windows.Forms.Padding(0, 3, 0, 3);
			this.label6.Size = new System.Drawing.Size(110, 19);
			this.label6.TabIndex = 0;
			this.label6.Text = "Last known message:";
			// 
			// label7
			// 
			this.label7.AutoSize = true;
			this.label7.Location = new System.Drawing.Point(3, 59);
			this.label7.Name = "label7";
			this.label7.Padding = new System.Windows.Forms.Padding(0, 3, 0, 3);
			this.label7.Size = new System.Drawing.Size(40, 19);
			this.label7.TabIndex = 0;
			this.label7.Text = "Visible:";
			// 
			// tableLayoutPanel1
			// 
			this.tableLayoutPanel1.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom)
						| System.Windows.Forms.AnchorStyles.Left)
						| System.Windows.Forms.AnchorStyles.Right)));
			this.tableLayoutPanel1.ColumnCount = 2;
			this.tableLayoutPanel1.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle());
			this.tableLayoutPanel1.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
			this.tableLayoutPanel1.Controls.Add(this.flowLayoutPanel1, 1, 2);
			this.tableLayoutPanel1.Controls.Add(this.idTextBox, 1, 0);
			this.tableLayoutPanel1.Controls.Add(this.logSourceLink, 1, 6);
			this.tableLayoutPanel1.Controls.Add(this.label6, 0, 5);
			this.tableLayoutPanel1.Controls.Add(this.label5, 0, 4);
			this.tableLayoutPanel1.Controls.Add(this.label4, 0, 2);
			this.tableLayoutPanel1.Controls.Add(this.label1, 0, 0);
			this.tableLayoutPanel1.Controls.Add(this.label2, 0, 1);
			this.tableLayoutPanel1.Controls.Add(this.label7, 0, 3);
			this.tableLayoutPanel1.Controls.Add(this.visibleCheckBox, 1, 3);
			this.tableLayoutPanel1.Controls.Add(this.lastMessageLinkLabel, 1, 5);
			this.tableLayoutPanel1.Controls.Add(this.firstMessageLinkLabel, 1, 4);
			this.tableLayoutPanel1.Controls.Add(this.nameTextBox, 1, 1);
			this.tableLayoutPanel1.Controls.Add(this.label8, 0, 6);
			this.tableLayoutPanel1.Location = new System.Drawing.Point(12, 12);
			this.tableLayoutPanel1.Name = "tableLayoutPanel1";
			this.tableLayoutPanel1.RowCount = 8;
			this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle());
			this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle());
			this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle());
			this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle());
			this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle());
			this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle());
			this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 20F));
			this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 20F));
			this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 20F));
			this.tableLayoutPanel1.Size = new System.Drawing.Size(276, 146);
			this.tableLayoutPanel1.TabIndex = 5;
			// 
			// flowLayoutPanel1
			// 
			this.flowLayoutPanel1.AutoSize = true;
			this.flowLayoutPanel1.Controls.Add(this.colorPanel);
			this.flowLayoutPanel1.Controls.Add(this.changeColorLinkLabel);
			this.flowLayoutPanel1.Dock = System.Windows.Forms.DockStyle.Fill;
			this.flowLayoutPanel1.Location = new System.Drawing.Point(116, 40);
			this.flowLayoutPanel1.Margin = new System.Windows.Forms.Padding(0);
			this.flowLayoutPanel1.Name = "flowLayoutPanel1";
			this.flowLayoutPanel1.Size = new System.Drawing.Size(160, 19);
			this.flowLayoutPanel1.TabIndex = 2;
			// 
			// colorPanel
			// 
			this.colorPanel.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
			this.colorPanel.Location = new System.Drawing.Point(1, 1);
			this.colorPanel.Margin = new System.Windows.Forms.Padding(1);
			this.colorPanel.Name = "colorPanel";
			this.colorPanel.Size = new System.Drawing.Size(43, 15);
			this.colorPanel.TabIndex = 3;
			// 
			// changeColorLinkLabel
			// 
			this.changeColorLinkLabel.AutoSize = true;
			this.changeColorLinkLabel.Location = new System.Drawing.Point(48, 0);
			this.changeColorLinkLabel.Name = "changeColorLinkLabel";
			this.changeColorLinkLabel.Size = new System.Drawing.Size(42, 13);
			this.changeColorLinkLabel.TabIndex = 4;
			this.changeColorLinkLabel.TabStop = true;
			this.changeColorLinkLabel.Text = "change";
			this.changeColorLinkLabel.Visible = false;
			// 
			// idTextBox
			// 
			this.idTextBox.BorderStyle = System.Windows.Forms.BorderStyle.None;
			this.idTextBox.Dock = System.Windows.Forms.DockStyle.Fill;
			this.idTextBox.Location = new System.Drawing.Point(119, 3);
			this.idTextBox.Name = "idTextBox";
			this.idTextBox.ReadOnly = true;
			this.idTextBox.Size = new System.Drawing.Size(154, 14);
			this.idTextBox.TabIndex = 0;
			// 
			// logSourceLink
			// 
			this.logSourceLink.AutoSize = true;
			this.logSourceLink.Location = new System.Drawing.Point(116, 116);
			this.logSourceLink.Margin = new System.Windows.Forms.Padding(0, 0, 3, 0);
			this.logSourceLink.Name = "logSourceLink";
			this.logSourceLink.Padding = new System.Windows.Forms.Padding(0, 3, 0, 3);
			this.logSourceLink.Size = new System.Drawing.Size(53, 19);
			this.logSourceLink.TabIndex = 9;
			this.logSourceLink.TabStop = true;
			this.logSourceLink.Text = "linkLabel1";
			this.logSourceLink.LinkClicked += new System.Windows.Forms.LinkLabelLinkClickedEventHandler(this.logSourceLink_LinkClicked);
			// 
			// visibleCheckBox
			// 
			this.visibleCheckBox.Dock = System.Windows.Forms.DockStyle.Fill;
			this.visibleCheckBox.Location = new System.Drawing.Point(116, 59);
			this.visibleCheckBox.Margin = new System.Windows.Forms.Padding(0);
			this.visibleCheckBox.Name = "visibleCheckBox";
			this.visibleCheckBox.Size = new System.Drawing.Size(160, 19);
			this.visibleCheckBox.TabIndex = 5;
			this.visibleCheckBox.Text = " ";
			this.visibleCheckBox.UseVisualStyleBackColor = true;
			this.visibleCheckBox.CheckedChanged += new System.EventHandler(this.visibleCheckBox_CheckedChanged);
			// 
			// lastMessageLinkLabel
			// 
			this.lastMessageLinkLabel.AutoSize = true;
			this.lastMessageLinkLabel.Location = new System.Drawing.Point(116, 97);
			this.lastMessageLinkLabel.Margin = new System.Windows.Forms.Padding(0, 0, 3, 0);
			this.lastMessageLinkLabel.Name = "lastMessageLinkLabel";
			this.lastMessageLinkLabel.Padding = new System.Windows.Forms.Padding(0, 3, 0, 3);
			this.lastMessageLinkLabel.Size = new System.Drawing.Size(53, 19);
			this.lastMessageLinkLabel.TabIndex = 8;
			this.lastMessageLinkLabel.TabStop = true;
			this.lastMessageLinkLabel.Text = "linkLabel1";
			this.lastMessageLinkLabel.LinkClicked += new System.Windows.Forms.LinkLabelLinkClickedEventHandler(this.linkLabelClicked);
			// 
			// firstMessageLinkLabel
			// 
			this.firstMessageLinkLabel.AutoSize = true;
			this.firstMessageLinkLabel.Location = new System.Drawing.Point(116, 78);
			this.firstMessageLinkLabel.Margin = new System.Windows.Forms.Padding(0, 0, 3, 0);
			this.firstMessageLinkLabel.Name = "firstMessageLinkLabel";
			this.firstMessageLinkLabel.Padding = new System.Windows.Forms.Padding(0, 3, 0, 3);
			this.firstMessageLinkLabel.Size = new System.Drawing.Size(53, 19);
			this.firstMessageLinkLabel.TabIndex = 7;
			this.firstMessageLinkLabel.TabStop = true;
			this.firstMessageLinkLabel.Text = "linkLabel1";
			this.firstMessageLinkLabel.LinkClicked += new System.Windows.Forms.LinkLabelLinkClickedEventHandler(this.linkLabelClicked);
			// 
			// nameTextBox
			// 
			this.nameTextBox.BorderStyle = System.Windows.Forms.BorderStyle.None;
			this.nameTextBox.Dock = System.Windows.Forms.DockStyle.Fill;
			this.nameTextBox.Location = new System.Drawing.Point(119, 23);
			this.nameTextBox.Name = "nameTextBox";
			this.nameTextBox.ReadOnly = true;
			this.nameTextBox.Size = new System.Drawing.Size(154, 14);
			this.nameTextBox.TabIndex = 1;
			// 
			// label8
			// 
			this.label8.AutoSize = true;
			this.label8.Location = new System.Drawing.Point(3, 116);
			this.label8.Name = "label8";
			this.label8.Padding = new System.Windows.Forms.Padding(0, 3, 0, 3);
			this.label8.Size = new System.Drawing.Size(64, 19);
			this.label8.TabIndex = 0;
			this.label8.Text = "Log Source:";
			// 
			// button1
			// 
			this.button1.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
			this.button1.DialogResult = System.Windows.Forms.DialogResult.Cancel;
			this.button1.Location = new System.Drawing.Point(213, 164);
			this.button1.Name = "button1";
			this.button1.Size = new System.Drawing.Size(75, 23);
			this.button1.TabIndex = 10;
			this.button1.Text = "Close";
			this.button1.UseVisualStyleBackColor = true;
			// 
			// ThreadPropertiesForm
			// 
			this.AcceptButton = this.button1;
			this.AutoScaleDimensions = new System.Drawing.SizeF(96F, 96F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Dpi;
			this.CancelButton = this.button1;
			this.ClientSize = new System.Drawing.Size(300, 195);
			this.Controls.Add(this.button1);
			this.Controls.Add(this.tableLayoutPanel1);
			this.Font = new System.Drawing.Font("Tahoma", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(204)));
			this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedToolWindow;
			this.Name = "ThreadPropertiesForm";
			this.ShowInTaskbar = false;
			this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
			this.Text = "Thread Proprties";
			this.tableLayoutPanel1.ResumeLayout(false);
			this.tableLayoutPanel1.PerformLayout();
			this.flowLayoutPanel1.ResumeLayout(false);
			this.flowLayoutPanel1.PerformLayout();
			this.ResumeLayout(false);

		}

		#endregion

		private System.Windows.Forms.Label label1;
		private System.Windows.Forms.Label label2;
		private System.Windows.Forms.Label label4;
		private System.Windows.Forms.Label label5;
		private System.Windows.Forms.Label label6;
		private System.Windows.Forms.Label label7;
		private System.Windows.Forms.TableLayoutPanel tableLayoutPanel1;
		private System.Windows.Forms.CheckBox visibleCheckBox;
		private System.Windows.Forms.LinkLabel firstMessageLinkLabel;
		private System.Windows.Forms.Panel colorPanel;
		private System.Windows.Forms.LinkLabel lastMessageLinkLabel;
		private System.Windows.Forms.Button button1;
		private System.Windows.Forms.TextBox idTextBox;
		private System.Windows.Forms.TextBox nameTextBox;
		private System.Windows.Forms.LinkLabel logSourceLink;
		private System.Windows.Forms.Label label8;
		private System.Windows.Forms.FlowLayoutPanel flowLayoutPanel1;
		private System.Windows.Forms.LinkLabel changeColorLinkLabel;

	}
}