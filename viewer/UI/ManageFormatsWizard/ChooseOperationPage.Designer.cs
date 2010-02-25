namespace LogJoint.UI
{
	partial class ChooseOperationPage
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
			this.netLogParserBasedFmtRadioButton = new System.Windows.Forms.RadioButton();
			this.newREBasedFmtRadioButton = new System.Windows.Forms.RadioButton();
			this.importTSVRadioButton = new System.Windows.Forms.RadioButton();
			this.importCSVRadioButton = new System.Windows.Forms.RadioButton();
			this.importLog4NetRadioButton = new System.Windows.Forms.RadioButton();
			this.changeRadioButton = new System.Windows.Forms.RadioButton();
			this.label1 = new System.Windows.Forms.Label();
			this.importNLogRadioButton = new System.Windows.Forms.RadioButton();
			this.SuspendLayout();
			// 
			// netLogParserBasedFmtRadioButton
			// 
			this.netLogParserBasedFmtRadioButton.AutoSize = true;
			this.netLogParserBasedFmtRadioButton.Enabled = false;
			this.netLogParserBasedFmtRadioButton.Location = new System.Drawing.Point(16, 163);
			this.netLogParserBasedFmtRadioButton.Name = "netLogParserBasedFmtRadioButton";
			this.netLogParserBasedFmtRadioButton.Size = new System.Drawing.Size(336, 17);
			this.netLogParserBasedFmtRadioButton.TabIndex = 8;
			this.netLogParserBasedFmtRadioButton.Text = "New custom Microsoft LogParser-based format (advanced users)";
			this.netLogParserBasedFmtRadioButton.UseVisualStyleBackColor = true;
			this.netLogParserBasedFmtRadioButton.MouseDown += new System.Windows.Forms.MouseEventHandler(this.cloneRadioButton_MouseDown);
			// 
			// newREBasedFmtRadioButton
			// 
			this.newREBasedFmtRadioButton.AutoSize = true;
			this.newREBasedFmtRadioButton.Location = new System.Drawing.Point(16, 138);
			this.newREBasedFmtRadioButton.Name = "newREBasedFmtRadioButton";
			this.newREBasedFmtRadioButton.Size = new System.Drawing.Size(364, 17);
			this.newREBasedFmtRadioButton.TabIndex = 7;
			this.newREBasedFmtRadioButton.Text = "New custom text format (regular expressions based,  advanced users)";
			this.newREBasedFmtRadioButton.UseVisualStyleBackColor = true;
			this.newREBasedFmtRadioButton.MouseDown += new System.Windows.Forms.MouseEventHandler(this.cloneRadioButton_MouseDown);
			// 
			// importTSVRadioButton
			// 
			this.importTSVRadioButton.AutoSize = true;
			this.importTSVRadioButton.Enabled = false;
			this.importTSVRadioButton.Location = new System.Drawing.Point(16, 113);
			this.importTSVRadioButton.Name = "importTSVRadioButton";
			this.importTSVRadioButton.Size = new System.Drawing.Size(295, 17);
			this.importTSVRadioButton.TabIndex = 6;
			this.importTSVRadioButton.Text = "Import tab-separated or space-separated values format";
			this.importTSVRadioButton.UseVisualStyleBackColor = true;
			this.importTSVRadioButton.MouseDown += new System.Windows.Forms.MouseEventHandler(this.cloneRadioButton_MouseDown);
			// 
			// importCSVRadioButton
			// 
			this.importCSVRadioButton.AutoSize = true;
			this.importCSVRadioButton.Enabled = false;
			this.importCSVRadioButton.Location = new System.Drawing.Point(16, 88);
			this.importCSVRadioButton.Name = "importCSVRadioButton";
			this.importCSVRadioButton.Size = new System.Drawing.Size(215, 17);
			this.importCSVRadioButton.TabIndex = 5;
			this.importCSVRadioButton.Text = "Import comma-separated values format";
			this.importCSVRadioButton.UseVisualStyleBackColor = true;
			this.importCSVRadioButton.MouseDown += new System.Windows.Forms.MouseEventHandler(this.cloneRadioButton_MouseDown);
			// 
			// importLog4NetRadioButton
			// 
			this.importLog4NetRadioButton.AutoSize = true;
			this.importLog4NetRadioButton.Location = new System.Drawing.Point(16, 65);
			this.importLog4NetRadioButton.Name = "importLog4NetRadioButton";
			this.importLog4NetRadioButton.Size = new System.Drawing.Size(131, 17);
			this.importLog4NetRadioButton.TabIndex = 3;
			this.importLog4NetRadioButton.Text = "Import log4net format";
			this.importLog4NetRadioButton.UseVisualStyleBackColor = true;
			this.importLog4NetRadioButton.MouseDown += new System.Windows.Forms.MouseEventHandler(this.cloneRadioButton_MouseDown);
			// 
			// changeRadioButton
			// 
			this.changeRadioButton.AutoSize = true;
			this.changeRadioButton.Checked = true;
			this.changeRadioButton.Location = new System.Drawing.Point(16, 40);
			this.changeRadioButton.Name = "changeRadioButton";
			this.changeRadioButton.Size = new System.Drawing.Size(183, 17);
			this.changeRadioButton.TabIndex = 2;
			this.changeRadioButton.TabStop = true;
			this.changeRadioButton.Text = "Operations over existing formats";
			this.changeRadioButton.UseVisualStyleBackColor = true;
			this.changeRadioButton.MouseDown += new System.Windows.Forms.MouseEventHandler(this.cloneRadioButton_MouseDown);
			// 
			// label1
			// 
			this.label1.AutoSize = true;
			this.label1.Font = new System.Drawing.Font("Tahoma", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(204)));
			this.label1.Location = new System.Drawing.Point(13, 12);
			this.label1.Name = "label1";
			this.label1.Size = new System.Drawing.Size(83, 13);
			this.label1.TabIndex = 14;
			this.label1.Text = "Select action:";
			// 
			// importNLogRadioButton
			// 
			this.importNLogRadioButton.AutoSize = true;
			this.importNLogRadioButton.Location = new System.Drawing.Point(144, 74);
			this.importNLogRadioButton.Name = "importNLogRadioButton";
			this.importNLogRadioButton.Size = new System.Drawing.Size(119, 17);
			this.importNLogRadioButton.TabIndex = 4;
			this.importNLogRadioButton.Text = "Import NLog format";
			this.importNLogRadioButton.UseVisualStyleBackColor = true;
			this.importNLogRadioButton.Visible = false;
			this.importNLogRadioButton.MouseDown += new System.Windows.Forms.MouseEventHandler(this.cloneRadioButton_MouseDown);
			// 
			// ChooseOperationPage
			// 
			this.AutoScaleDimensions = new System.Drawing.SizeF(96F, 96F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Dpi;
			this.Controls.Add(this.label1);
			this.Controls.Add(this.netLogParserBasedFmtRadioButton);
			this.Controls.Add(this.newREBasedFmtRadioButton);
			this.Controls.Add(this.importTSVRadioButton);
			this.Controls.Add(this.importCSVRadioButton);
			this.Controls.Add(this.importNLogRadioButton);
			this.Controls.Add(this.importLog4NetRadioButton);
			this.Controls.Add(this.changeRadioButton);
			this.Font = new System.Drawing.Font("Tahoma", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(204)));
			this.Name = "ChooseOperationPage";
			this.Size = new System.Drawing.Size(442, 305);
			this.ResumeLayout(false);
			this.PerformLayout();

		}

		#endregion

		private System.Windows.Forms.Label label1;
		public System.Windows.Forms.RadioButton netLogParserBasedFmtRadioButton;
		public System.Windows.Forms.RadioButton newREBasedFmtRadioButton;
		public System.Windows.Forms.RadioButton importTSVRadioButton;
		public System.Windows.Forms.RadioButton importCSVRadioButton;
		public System.Windows.Forms.RadioButton importLog4NetRadioButton;
		public System.Windows.Forms.RadioButton changeRadioButton;
		public System.Windows.Forms.RadioButton importNLogRadioButton;


	}
}
