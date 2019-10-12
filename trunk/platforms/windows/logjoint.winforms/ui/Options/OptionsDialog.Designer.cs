namespace LogJoint.UI
{
	partial class OptionsDialog
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
			System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(OptionsDialog));
			this.okButton = new System.Windows.Forms.Button();
			this.cancelButton = new System.Windows.Forms.Button();
			this.tabControl1 = new System.Windows.Forms.TabControl();
			this.memAndPerformanceTabPage = new System.Windows.Forms.TabPage();
			this.appearanceTabPage = new System.Windows.Forms.TabPage();
			this.updatesAndFeedbackTabPage = new System.Windows.Forms.TabPage();
			this.pluginsTabPage = new System.Windows.Forms.TabPage();
			this.memAndPerformanceSettingsView = new LogJoint.UI.MemAndPerformanceSettingsView();
			this.appearanceSettingsView1 = new LogJoint.UI.AppearanceSettingsView();
			this.updatesAndFeedbackView1 = new LogJoint.UI.UpdatesAndFeedbackView();
			this.pluginsView1 = new LogJoint.UI.PluginsView();
			this.tabControl1.SuspendLayout();
			this.memAndPerformanceTabPage.SuspendLayout();
			this.appearanceTabPage.SuspendLayout();
			this.updatesAndFeedbackTabPage.SuspendLayout();
			this.pluginsTabPage.SuspendLayout();
			this.SuspendLayout();
			// 
			// okButton
			// 
			this.okButton.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
			this.okButton.Location = new System.Drawing.Point(350, 409);
			this.okButton.Margin = new System.Windows.Forms.Padding(4);
			this.okButton.Name = "okButton";
			this.okButton.Size = new System.Drawing.Size(94, 29);
			this.okButton.TabIndex = 1;
			this.okButton.Text = "OK";
			this.okButton.UseVisualStyleBackColor = true;
			this.okButton.Click += new System.EventHandler(this.okButton_Click);
			// 
			// cancelButton
			// 
			this.cancelButton.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
			this.cancelButton.DialogResult = System.Windows.Forms.DialogResult.Cancel;
			this.cancelButton.Location = new System.Drawing.Point(465, 409);
			this.cancelButton.Margin = new System.Windows.Forms.Padding(4);
			this.cancelButton.Name = "cancelButton";
			this.cancelButton.Size = new System.Drawing.Size(94, 29);
			this.cancelButton.TabIndex = 2;
			this.cancelButton.Text = "Cancel";
			this.cancelButton.UseVisualStyleBackColor = true;
			this.cancelButton.Click += new System.EventHandler(this.cancelButton_Click);
			// 
			// tabControl1
			// 
			this.tabControl1.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
			this.tabControl1.Controls.Add(this.memAndPerformanceTabPage);
			this.tabControl1.Controls.Add(this.appearanceTabPage);
			this.tabControl1.Controls.Add(this.updatesAndFeedbackTabPage);
			this.tabControl1.Controls.Add(this.pluginsTabPage);
			this.tabControl1.Location = new System.Drawing.Point(2, 2);
			this.tabControl1.Name = "tabControl1";
			this.tabControl1.SelectedIndex = 0;
			this.tabControl1.Size = new System.Drawing.Size(569, 397);
			this.tabControl1.TabIndex = 3;
			// 
			// memAndPerformanceTabPage
			// 
			this.memAndPerformanceTabPage.Controls.Add(this.memAndPerformanceSettingsView);
			this.memAndPerformanceTabPage.Location = new System.Drawing.Point(4, 26);
			this.memAndPerformanceTabPage.Name = "memAndPerformanceTabPage";
			this.memAndPerformanceTabPage.Padding = new System.Windows.Forms.Padding(7);
			this.memAndPerformanceTabPage.Size = new System.Drawing.Size(561, 367);
			this.memAndPerformanceTabPage.TabIndex = 0;
			this.memAndPerformanceTabPage.Text = "Resources and performance";
			this.memAndPerformanceTabPage.UseVisualStyleBackColor = true;
			// 
			// appearanceTabPage
			// 
			this.appearanceTabPage.Controls.Add(this.appearanceSettingsView1);
			this.appearanceTabPage.Location = new System.Drawing.Point(4, 26);
			this.appearanceTabPage.Margin = new System.Windows.Forms.Padding(0);
			this.appearanceTabPage.Name = "appearanceTabPage";
			this.appearanceTabPage.Padding = new System.Windows.Forms.Padding(7);
			this.appearanceTabPage.Size = new System.Drawing.Size(561, 367);
			this.appearanceTabPage.TabIndex = 1;
			this.appearanceTabPage.Text = "Appearance";
			this.appearanceTabPage.UseVisualStyleBackColor = true;
			// 
			// updatesAndFeedbackTabPage
			// 
			this.updatesAndFeedbackTabPage.Controls.Add(this.updatesAndFeedbackView1);
			this.updatesAndFeedbackTabPage.Location = new System.Drawing.Point(4, 26);
			this.updatesAndFeedbackTabPage.Name = "updatesAndFeedbackTabPage";
			this.updatesAndFeedbackTabPage.Padding = new System.Windows.Forms.Padding(3);
			this.updatesAndFeedbackTabPage.Size = new System.Drawing.Size(561, 367);
			this.updatesAndFeedbackTabPage.TabIndex = 2;
			this.updatesAndFeedbackTabPage.Text = "Software Update";
			this.updatesAndFeedbackTabPage.UseVisualStyleBackColor = true;
			// 
			// pluginsTabPage
			// 
			this.pluginsTabPage.Controls.Add(this.pluginsView1);
			this.pluginsTabPage.Location = new System.Drawing.Point(4, 26);
			this.pluginsTabPage.Name = "pluginsTabPage";
			this.pluginsTabPage.Padding = new System.Windows.Forms.Padding(3);
			this.pluginsTabPage.Size = new System.Drawing.Size(561, 367);
			this.pluginsTabPage.TabIndex = 3;
			this.pluginsTabPage.Text = "Plug-ins";
			this.pluginsTabPage.UseVisualStyleBackColor = true;
			// 
			// memAndPerformanceSettingsView
			// 
			this.memAndPerformanceSettingsView.Dock = System.Windows.Forms.DockStyle.Fill;
			this.memAndPerformanceSettingsView.Font = new System.Drawing.Font("Tahoma", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(204)));
			this.memAndPerformanceSettingsView.Location = new System.Drawing.Point(7, 7);
			this.memAndPerformanceSettingsView.Margin = new System.Windows.Forms.Padding(5);
			this.memAndPerformanceSettingsView.Name = "memAndPerformanceSettingsView";
			this.memAndPerformanceSettingsView.Size = new System.Drawing.Size(547, 353);
			this.memAndPerformanceSettingsView.TabIndex = 0;
			// 
			// appearanceSettingsView1
			// 
			this.appearanceSettingsView1.Dock = System.Windows.Forms.DockStyle.Fill;
			this.appearanceSettingsView1.Font = new System.Drawing.Font("Tahoma", 8.25F);
			this.appearanceSettingsView1.Location = new System.Drawing.Point(7, 7);
			this.appearanceSettingsView1.Margin = new System.Windows.Forms.Padding(0);
			this.appearanceSettingsView1.Name = "appearanceSettingsView1";
			this.appearanceSettingsView1.Padding = new System.Windows.Forms.Padding(2);
			this.appearanceSettingsView1.Size = new System.Drawing.Size(547, 353);
			this.appearanceSettingsView1.TabIndex = 0;
			// 
			// updatesAndFeedbackView1
			// 
			this.updatesAndFeedbackView1.Dock = System.Windows.Forms.DockStyle.Fill;
			this.updatesAndFeedbackView1.Font = new System.Drawing.Font("Tahoma", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(204)));
			this.updatesAndFeedbackView1.Location = new System.Drawing.Point(3, 3);
			this.updatesAndFeedbackView1.Margin = new System.Windows.Forms.Padding(4);
			this.updatesAndFeedbackView1.Name = "updatesAndFeedbackView1";
			this.updatesAndFeedbackView1.Size = new System.Drawing.Size(555, 362);
			this.updatesAndFeedbackView1.TabIndex = 0;
			// 
			// pluginsView1
			// 
			this.pluginsView1.Dock = System.Windows.Forms.DockStyle.Fill;
			this.pluginsView1.Font = new System.Drawing.Font("Tahoma", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(204)));
			this.pluginsView1.Location = new System.Drawing.Point(3, 3);
			this.pluginsView1.Margin = new System.Windows.Forms.Padding(4);
			this.pluginsView1.Name = "pluginsView1";
			this.pluginsView1.Size = new System.Drawing.Size(555, 361);
			this.pluginsView1.TabIndex = 0;
			// 
			// OptionsDialog
			// 
			this.AcceptButton = this.okButton;
			this.AutoScaleDimensions = new System.Drawing.SizeF(120F, 120F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Dpi;
			this.CancelButton = this.cancelButton;
			this.ClientSize = new System.Drawing.Size(574, 450);
			this.Controls.Add(this.tabControl1);
			this.Controls.Add(this.cancelButton);
			this.Controls.Add(this.okButton);
			this.Font = new System.Drawing.Font("Tahoma", 8.25F);
			this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
			this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
			this.Margin = new System.Windows.Forms.Padding(4);
			this.MaximizeBox = false;
			this.MinimizeBox = false;
			this.Name = "OptionsDialog";
			this.ShowIcon = false;
			this.ShowInTaskbar = false;
			this.SizeGripStyle = System.Windows.Forms.SizeGripStyle.Hide;
			this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
			this.Text = "Options";
			this.tabControl1.ResumeLayout(false);
			this.memAndPerformanceTabPage.ResumeLayout(false);
			this.appearanceTabPage.ResumeLayout(false);
			this.updatesAndFeedbackTabPage.ResumeLayout(false);
			this.pluginsTabPage.ResumeLayout(false);
			this.ResumeLayout(false);

		}

		#endregion

		private System.Windows.Forms.Button okButton;
		private System.Windows.Forms.Button cancelButton;
		private System.Windows.Forms.TabControl tabControl1;
		private System.Windows.Forms.TabPage memAndPerformanceTabPage;
		internal MemAndPerformanceSettingsView memAndPerformanceSettingsView;
		private System.Windows.Forms.TabPage appearanceTabPage;
		private AppearanceSettingsView appearanceSettingsView1;
		private System.Windows.Forms.TabPage updatesAndFeedbackTabPage;
		private UpdatesAndFeedbackView updatesAndFeedbackView1;
		private System.Windows.Forms.TabPage pluginsTabPage;
		private PluginsView pluginsView1;
	}
}