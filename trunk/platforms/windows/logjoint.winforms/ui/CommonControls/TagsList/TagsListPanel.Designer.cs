namespace LogJoint.UI
{
	partial class TagsListControl
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
			this.allTagsLinkLabel = new System.Windows.Forms.LinkLabel();
			this.SuspendLayout();
			// 
			// allTagsLinkLabel
			// 
			this.allTagsLinkLabel.AutoEllipsis = true;
			this.allTagsLinkLabel.Dock = System.Windows.Forms.DockStyle.Fill;
			this.allTagsLinkLabel.Location = new System.Drawing.Point(0, 0);
			this.allTagsLinkLabel.Name = "allTagsLinkLabel";
			this.allTagsLinkLabel.Size = new System.Drawing.Size(326, 20);
			this.allTagsLinkLabel.TabIndex = 7;
			this.allTagsLinkLabel.TabStop = true;
			this.allTagsLinkLabel.Text = "tags:";
			this.allTagsLinkLabel.LinkClicked += new System.Windows.Forms.LinkLabelLinkClickedEventHandler(this.allTagsLinkLabel_LinkClicked);
			// 
			// TagsListControl
			// 
			this.AutoScaleDimensions = new System.Drawing.SizeF(120F, 120F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Dpi;
			this.AutoSize = true;
			this.Controls.Add(this.allTagsLinkLabel);
			this.Name = "TagsListControl";
			this.Size = new System.Drawing.Size(326, 20);
			this.ResumeLayout(false);

		}

		#endregion

		private System.Windows.Forms.LinkLabel allTagsLinkLabel;
	}
}
