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
			this.newREBasedFmtRadioButton = new System.Windows.Forms.RadioButton();
			this.importLog4NetRadioButton = new System.Windows.Forms.RadioButton();
			this.changeRadioButton = new System.Windows.Forms.RadioButton();
			this.label1 = new System.Windows.Forms.Label();
			this.importNLogRadioButton = new System.Windows.Forms.RadioButton();
			this.SuspendLayout();
			// 
			// newREBasedFmtRadioButton
			// 
			this.newREBasedFmtRadioButton.AutoSize = true;
			this.newREBasedFmtRadioButton.Location = new System.Drawing.Point(20, 139);
			this.newREBasedFmtRadioButton.Margin = new System.Windows.Forms.Padding(4);
			this.newREBasedFmtRadioButton.Name = "newREBasedFmtRadioButton";
			this.newREBasedFmtRadioButton.Size = new System.Drawing.Size(458, 21);
			this.newREBasedFmtRadioButton.TabIndex = 7;
			this.newREBasedFmtRadioButton.Text = "New custom text format (regular expressions based,  advanced users)";
			this.newREBasedFmtRadioButton.UseVisualStyleBackColor = true;
			this.newREBasedFmtRadioButton.MouseDown += new System.Windows.Forms.MouseEventHandler(this.cloneRadioButton_MouseDown);
			// 
			// importLog4NetRadioButton
			// 
			this.importLog4NetRadioButton.AutoSize = true;
			this.importLog4NetRadioButton.Location = new System.Drawing.Point(20, 81);
			this.importLog4NetRadioButton.Margin = new System.Windows.Forms.Padding(4);
			this.importLog4NetRadioButton.Name = "importLog4NetRadioButton";
			this.importLog4NetRadioButton.Size = new System.Drawing.Size(166, 21);
			this.importLog4NetRadioButton.TabIndex = 3;
			this.importLog4NetRadioButton.Text = "Import log4net format";
			this.importLog4NetRadioButton.UseVisualStyleBackColor = true;
			this.importLog4NetRadioButton.MouseDown += new System.Windows.Forms.MouseEventHandler(this.cloneRadioButton_MouseDown);
			// 
			// changeRadioButton
			// 
			this.changeRadioButton.AutoSize = true;
			this.changeRadioButton.Checked = true;
			this.changeRadioButton.Location = new System.Drawing.Point(20, 50);
			this.changeRadioButton.Margin = new System.Windows.Forms.Padding(4);
			this.changeRadioButton.Name = "changeRadioButton";
			this.changeRadioButton.Size = new System.Drawing.Size(225, 21);
			this.changeRadioButton.TabIndex = 2;
			this.changeRadioButton.TabStop = true;
			this.changeRadioButton.Text = "Operations with existing formats";
			this.changeRadioButton.UseVisualStyleBackColor = true;
			this.changeRadioButton.MouseDown += new System.Windows.Forms.MouseEventHandler(this.cloneRadioButton_MouseDown);
			// 
			// label1
			// 
			this.label1.AutoSize = true;
			this.label1.Font = new System.Drawing.Font("Tahoma", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(204)));
			this.label1.Location = new System.Drawing.Point(16, 15);
			this.label1.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
			this.label1.Name = "label1";
			this.label1.Size = new System.Drawing.Size(100, 17);
			this.label1.TabIndex = 14;
			this.label1.Text = "Select action:";
			// 
			// importNLogRadioButton
			// 
			this.importNLogRadioButton.AutoSize = true;
			this.importNLogRadioButton.Location = new System.Drawing.Point(20, 110);
			this.importNLogRadioButton.Margin = new System.Windows.Forms.Padding(4);
			this.importNLogRadioButton.Name = "importNLogRadioButton";
			this.importNLogRadioButton.Size = new System.Drawing.Size(152, 21);
			this.importNLogRadioButton.TabIndex = 4;
			this.importNLogRadioButton.Text = "Import NLog format";
			this.importNLogRadioButton.UseVisualStyleBackColor = true;
			this.importNLogRadioButton.MouseDown += new System.Windows.Forms.MouseEventHandler(this.cloneRadioButton_MouseDown);
			// 
			// ChooseOperationPage
			// 
			this.AutoScaleDimensions = new System.Drawing.SizeF(120F, 120F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Dpi;
			this.Controls.Add(this.label1);
			this.Controls.Add(this.newREBasedFmtRadioButton);
			this.Controls.Add(this.importNLogRadioButton);
			this.Controls.Add(this.importLog4NetRadioButton);
			this.Controls.Add(this.changeRadioButton);
			this.Font = new System.Drawing.Font("Tahoma", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(204)));
			this.Margin = new System.Windows.Forms.Padding(4);
			this.Name = "ChooseOperationPage";
			this.Size = new System.Drawing.Size(552, 381);
			this.ResumeLayout(false);
			this.PerformLayout();

		}

		#endregion

		private System.Windows.Forms.Label label1;
		public System.Windows.Forms.RadioButton newREBasedFmtRadioButton;
		public System.Windows.Forms.RadioButton importLog4NetRadioButton;
		public System.Windows.Forms.RadioButton changeRadioButton;
		public System.Windows.Forms.RadioButton importNLogRadioButton;


	}
}
