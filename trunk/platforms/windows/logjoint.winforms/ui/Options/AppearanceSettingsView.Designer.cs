namespace LogJoint.UI
{
	partial class AppearanceSettingsView
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
			this.groupBox1 = new System.Windows.Forms.GroupBox();
			this.paletteComboBox = new System.Windows.Forms.ComboBox();
			this.coloringModeComboBox = new System.Windows.Forms.ComboBox();
			this.label4 = new System.Windows.Forms.Label();
			this.label3 = new System.Windows.Forms.Label();
			this.groupBox2 = new System.Windows.Forms.GroupBox();
			this.label2 = new System.Windows.Forms.Label();
			this.label1 = new System.Windows.Forms.Label();
			this.fontFamiliesComboBox = new System.Windows.Forms.ComboBox();
			this.panel1 = new System.Windows.Forms.Panel();
			this.logViewerControl1 = new LogJoint.UI.LogViewerControl();
			this.fontSizeEditor = new LogJoint.UI.GaugeControl();
			this.groupBox1.SuspendLayout();
			this.groupBox2.SuspendLayout();
			this.panel1.SuspendLayout();
			this.SuspendLayout();
			// 
			// groupBox1
			// 
			this.groupBox1.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
			this.groupBox1.Controls.Add(this.paletteComboBox);
			this.groupBox1.Controls.Add(this.coloringModeComboBox);
			this.groupBox1.Controls.Add(this.label4);
			this.groupBox1.Controls.Add(this.label3);
			this.groupBox1.Location = new System.Drawing.Point(0, 2);
			this.groupBox1.Name = "groupBox1";
			this.groupBox1.Size = new System.Drawing.Size(526, 110);
			this.groupBox1.TabIndex = 10;
			this.groupBox1.TabStop = false;
			this.groupBox1.Text = "Background coloring";
			// 
			// paletteComboBox
			// 
			this.paletteComboBox.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
			this.paletteComboBox.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
			this.paletteComboBox.FormattingEnabled = true;
			this.paletteComboBox.Location = new System.Drawing.Point(140, 70);
			this.paletteComboBox.Name = "paletteComboBox";
			this.paletteComboBox.Size = new System.Drawing.Size(362, 25);
			this.paletteComboBox.TabIndex = 2;
			this.paletteComboBox.SelectedIndexChanged += new System.EventHandler(this.fontFamiliesComboBox_SelectedIndexChanged);
			// 
			// coloringModeComboBox
			// 
			this.coloringModeComboBox.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
			this.coloringModeComboBox.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
			this.coloringModeComboBox.FormattingEnabled = true;
			this.coloringModeComboBox.Location = new System.Drawing.Point(140, 32);
			this.coloringModeComboBox.Name = "coloringModeComboBox";
			this.coloringModeComboBox.Size = new System.Drawing.Size(362, 25);
			this.coloringModeComboBox.TabIndex = 1;
			this.coloringModeComboBox.SelectedIndexChanged += new System.EventHandler(this.fontFamiliesComboBox_SelectedIndexChanged);
			// 
			// label4
			// 
			this.label4.AutoSize = true;
			this.label4.Location = new System.Drawing.Point(16, 73);
			this.label4.Name = "label4";
			this.label4.Size = new System.Drawing.Size(100, 17);
			this.label4.TabIndex = 1;
			this.label4.Text = "Coloring patelle";
			this.label4.Click += new System.EventHandler(this.label1_Click);
			// 
			// label3
			// 
			this.label3.AutoSize = true;
			this.label3.Location = new System.Drawing.Point(16, 35);
			this.label3.Name = "label3";
			this.label3.Size = new System.Drawing.Size(103, 17);
			this.label3.TabIndex = 1;
			this.label3.Text = "Default coloring";
			this.label3.Click += new System.EventHandler(this.label1_Click);
			// 
			// groupBox2
			// 
			this.groupBox2.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
			this.groupBox2.Controls.Add(this.fontSizeEditor);
			this.groupBox2.Controls.Add(this.label2);
			this.groupBox2.Controls.Add(this.label1);
			this.groupBox2.Controls.Add(this.fontFamiliesComboBox);
			this.groupBox2.Location = new System.Drawing.Point(0, 121);
			this.groupBox2.Name = "groupBox2";
			this.groupBox2.Size = new System.Drawing.Size(526, 93);
			this.groupBox2.TabIndex = 20;
			this.groupBox2.TabStop = false;
			this.groupBox2.Text = "Font";
			// 
			// label2
			// 
			this.label2.AutoSize = true;
			this.label2.Location = new System.Drawing.Point(16, 61);
			this.label2.Name = "label2";
			this.label2.Size = new System.Drawing.Size(76, 17);
			this.label2.TabIndex = 1;
			this.label2.Text = "Default size";
			// 
			// label1
			// 
			this.label1.AutoSize = true;
			this.label1.Location = new System.Drawing.Point(16, 24);
			this.label1.Name = "label1";
			this.label1.Size = new System.Drawing.Size(46, 17);
			this.label1.TabIndex = 1;
			this.label1.Text = "Family";
			this.label1.Click += new System.EventHandler(this.label1_Click);
			// 
			// fontFamiliesComboBox
			// 
			this.fontFamiliesComboBox.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
			this.fontFamiliesComboBox.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
			this.fontFamiliesComboBox.FormattingEnabled = true;
			this.fontFamiliesComboBox.Location = new System.Drawing.Point(140, 21);
			this.fontFamiliesComboBox.Name = "fontFamiliesComboBox";
			this.fontFamiliesComboBox.Size = new System.Drawing.Size(362, 25);
			this.fontFamiliesComboBox.TabIndex = 4;
			this.fontFamiliesComboBox.SelectedIndexChanged += new System.EventHandler(this.fontFamiliesComboBox_SelectedIndexChanged);
			// 
			// panel1
			// 
			this.panel1.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
			this.panel1.BorderStyle = System.Windows.Forms.BorderStyle.Fixed3D;
			this.panel1.Controls.Add(this.logViewerControl1);
			this.panel1.Location = new System.Drawing.Point(0, 222);
			this.panel1.Margin = new System.Windows.Forms.Padding(0);
			this.panel1.Name = "panel1";
			this.panel1.Size = new System.Drawing.Size(526, 204);
			this.panel1.TabIndex = 30;
			this.panel1.TabStop = true;
			// 
			// logViewerControl1
			// 
			this.logViewerControl1.BackColor = System.Drawing.Color.White;
			this.logViewerControl1.Cursor = System.Windows.Forms.Cursors.IBeam;
			this.logViewerControl1.Dock = System.Windows.Forms.DockStyle.Fill;
			this.logViewerControl1.Location = new System.Drawing.Point(0, 0);
			this.logViewerControl1.Name = "logViewerControl1";
			this.logViewerControl1.Size = new System.Drawing.Size(522, 200);
			this.logViewerControl1.TabIndex = 1;
			this.logViewerControl1.Text = "logViewerControl1";
			// 
			// fontSizeEditor
			// 
			this.fontSizeEditor.AllowedValues = new int[0];
			this.fontSizeEditor.AutoSize = true;
			this.fontSizeEditor.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
			this.fontSizeEditor.Font = new System.Drawing.Font("Tahoma", 8F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(204)));
			this.fontSizeEditor.Location = new System.Drawing.Point(140, 49);
			this.fontSizeEditor.Margin = new System.Windows.Forms.Padding(0);
			this.fontSizeEditor.MaxValue = 2147483647;
			this.fontSizeEditor.MinValue = -2147483648;
			this.fontSizeEditor.Name = "fontSizeEditor";
			this.fontSizeEditor.Size = new System.Drawing.Size(39, 37);
			this.fontSizeEditor.TabIndex = 6;
			this.fontSizeEditor.Value = 0;
			this.fontSizeEditor.ValueChanged += new System.EventHandler<System.EventArgs>(this.fontSizeEditor_ValueChanged);
			// 
			// AppearanceSettingsView
			// 
			this.AutoScaleDimensions = new System.Drawing.SizeF(120F, 120F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Dpi;
			this.Controls.Add(this.panel1);
			this.Controls.Add(this.groupBox2);
			this.Controls.Add(this.groupBox1);
			this.Font = new System.Drawing.Font("Tahoma", 8.25F);
			this.Margin = new System.Windows.Forms.Padding(0);
			this.Name = "AppearanceSettingsView";
			this.Padding = new System.Windows.Forms.Padding(2);
			this.Size = new System.Drawing.Size(526, 426);
			this.groupBox1.ResumeLayout(false);
			this.groupBox1.PerformLayout();
			this.groupBox2.ResumeLayout(false);
			this.groupBox2.PerformLayout();
			this.panel1.ResumeLayout(false);
			this.ResumeLayout(false);

		}

		#endregion

		private System.Windows.Forms.GroupBox groupBox1;
		private System.Windows.Forms.GroupBox groupBox2;
		private System.Windows.Forms.Label label2;
		private System.Windows.Forms.Label label1;
		private System.Windows.Forms.ComboBox fontFamiliesComboBox;
		private GaugeControl fontSizeEditor;
		private LogViewerControl logViewerControl1;
		private System.Windows.Forms.Panel panel1;
		private System.Windows.Forms.ComboBox coloringModeComboBox;
		private System.Windows.Forms.ComboBox paletteComboBox;
		private System.Windows.Forms.Label label4;
		private System.Windows.Forms.Label label3;
	}
}
