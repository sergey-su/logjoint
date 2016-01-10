namespace LogJoint.UI.Presenters.NewLogSourceDialog.Pages.WindowsEventsLog
{
	partial class EVTFactoryUI
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
			this.selectLiveLogButton = new System.Windows.Forms.Button();
			this.openFileDialog = new System.Windows.Forms.OpenFileDialog();
			this.logTextBox = new System.Windows.Forms.TextBox();
			this.label1 = new System.Windows.Forms.Label();
			this.button1 = new System.Windows.Forms.Button();
			this.SuspendLayout();
			// 
			// selectLiveLogButton
			// 
			this.selectLiveLogButton.Location = new System.Drawing.Point(198, 84);
			this.selectLiveLogButton.Margin = new System.Windows.Forms.Padding(4);
			this.selectLiveLogButton.Name = "selectLiveLogButton";
			this.selectLiveLogButton.Size = new System.Drawing.Size(154, 29);
			this.selectLiveLogButton.TabIndex = 2;
			this.selectLiveLogButton.Text = "Choose live log...";
			this.selectLiveLogButton.UseVisualStyleBackColor = true;
			this.selectLiveLogButton.Click += new System.EventHandler(this.openButton2_Click);
			// 
			// logTextBox
			// 
			this.logTextBox.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
			this.logTextBox.Location = new System.Drawing.Point(26, 42);
			this.logTextBox.Name = "logTextBox";
			this.logTextBox.ReadOnly = true;
			this.logTextBox.Size = new System.Drawing.Size(448, 24);
			this.logTextBox.TabIndex = 4;
			// 
			// label1
			// 
			this.label1.AutoSize = true;
			this.label1.Location = new System.Drawing.Point(26, 19);
			this.label1.Name = "label1";
			this.label1.Size = new System.Drawing.Size(186, 17);
			this.label1.TabIndex = 5;
			this.label1.Text = "Selected Windows Event Log";
			// 
			// button1
			// 
			this.button1.Location = new System.Drawing.Point(26, 84);
			this.button1.Margin = new System.Windows.Forms.Padding(4);
			this.button1.Name = "button1";
			this.button1.Size = new System.Drawing.Size(154, 29);
			this.button1.TabIndex = 2;
			this.button1.Text = "Choose saved log...";
			this.button1.UseVisualStyleBackColor = true;
			this.button1.Click += new System.EventHandler(this.openButton1_Click);
			// 
			// EVTFactoryUI
			// 
			this.AutoScaleDimensions = new System.Drawing.SizeF(120F, 120F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Dpi;
			this.Controls.Add(this.label1);
			this.Controls.Add(this.logTextBox);
			this.Controls.Add(this.button1);
			this.Controls.Add(this.selectLiveLogButton);
			this.Font = new System.Drawing.Font("Tahoma", 8.25F);
			this.Margin = new System.Windows.Forms.Padding(4);
			this.Name = "EVTFactoryUI";
			this.Size = new System.Drawing.Size(505, 342);
			this.ResumeLayout(false);
			this.PerformLayout();

		}

		#endregion

		private System.Windows.Forms.Button selectLiveLogButton;
		private System.Windows.Forms.OpenFileDialog openFileDialog;
		private System.Windows.Forms.TextBox logTextBox;
		private System.Windows.Forms.Label label1;
		private System.Windows.Forms.Button button1;


	}
}
