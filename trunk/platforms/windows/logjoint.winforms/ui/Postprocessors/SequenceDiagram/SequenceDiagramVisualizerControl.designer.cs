namespace LogJoint.UI.Postprocessing.SequenceDiagramVisualizer
{
	partial class SequenceDiagramVisualizerControl
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
			this.components = new System.ComponentModel.Container();
            this.panel1 = new System.Windows.Forms.Panel();
            this.currentArrowDescription = new System.Windows.Forms.LinkLabel();
            this.currentArrowCaptionLabel = new System.Windows.Forms.TextBox();
            this.panel3 = new System.Windows.Forms.Panel();
            this.flowLayoutPanel1 = new System.Windows.Forms.FlowLayoutPanel();
            this.prevUserActionButton = new System.Windows.Forms.LinkLabel();
            this.nextUserActionButton = new System.Windows.Forms.LinkLabel();
            this.prevBookmarkButton = new System.Windows.Forms.LinkLabel();
            this.nextBookmarkButton = new System.Windows.Forms.LinkLabel();
            this.findCurrentTimeButton = new System.Windows.Forms.LinkLabel();
            this.zoomInButton = new System.Windows.Forms.LinkLabel();
            this.zoomOutButton = new System.Windows.Forms.LinkLabel();
            this.collapseResponsesCheckbox = new System.Windows.Forms.CheckBox();
            this.collapseRoleInstancesCheckbox = new System.Windows.Forms.CheckBox();
            this.panel6 = new System.Windows.Forms.Panel();
            this.vScrollBar = new System.Windows.Forms.VScrollBar();
            this.hScrollBar = new System.Windows.Forms.HScrollBar();
            this.tableLayoutPanel1 = new System.Windows.Forms.TableLayoutPanel();
            this.arrowsPanel = new DoubleBufferedPanel();
            this.rolesCaptionsPanel = new DoubleBufferedPanel();
            this.leftPanel = new DoubleBufferedPanel();
            this.tagsListContainerPanel = new System.Windows.Forms.Panel();
            this.tagsListControl = new TagsListControl();
            this.panel2 = new System.Windows.Forms.Panel();
            this.quickSearchEditBox = new LogJoint.UI.QuickSearchTextBox.BorderedQuickSearchTextBox();
            this.toastNotificationsListControl = new ToastNotificationsListControl();
            this.notificationsButton = new System.Windows.Forms.Button();
            this.toolTip1 = new System.Windows.Forms.ToolTip(this.components);
            this.panel1.SuspendLayout();
            this.flowLayoutPanel1.SuspendLayout();
            this.tableLayoutPanel1.SuspendLayout();
            this.leftPanel.SuspendLayout();
            this.tagsListContainerPanel.SuspendLayout();
            this.SuspendLayout();
            // 
            // panel1
            // 
            this.panel1.BackColor = System.Drawing.Color.White;
            this.panel1.Controls.Add(this.currentArrowDescription);
            this.panel1.Controls.Add(this.currentArrowCaptionLabel);
            this.panel1.Controls.Add(this.panel3);
            this.panel1.Dock = System.Windows.Forms.DockStyle.Bottom;
            this.panel1.Location = new System.Drawing.Point(0, 411);
            this.panel1.Name = "panel1";
            this.panel1.Size = new System.Drawing.Size(703, 46);
            this.panel1.TabIndex = 3;
            // 
            // currentArrowDescription
            // 
            this.currentArrowDescription.BackColor = System.Drawing.Color.White;
            this.currentArrowDescription.Dock = System.Windows.Forms.DockStyle.Fill;
            this.currentArrowDescription.Location = new System.Drawing.Point(0, 19);
            this.currentArrowDescription.Margin = new System.Windows.Forms.Padding(3, 2, 3, 0);
            this.currentArrowDescription.Name = "currentArrowDescription";
            this.currentArrowDescription.Size = new System.Drawing.Size(703, 27);
            this.currentArrowDescription.TabIndex = 4;
            this.currentArrowDescription.LinkClicked += new System.Windows.Forms.LinkLabelLinkClickedEventHandler(this.currentArrowDescription_LinkClicked);
            // 
            // currentArrowCaptionLabel
            // 
            this.currentArrowCaptionLabel.BackColor = System.Drawing.Color.White;
            this.currentArrowCaptionLabel.BorderStyle = System.Windows.Forms.BorderStyle.None;
            this.currentArrowCaptionLabel.Dock = System.Windows.Forms.DockStyle.Top;
            this.currentArrowCaptionLabel.Font = new System.Drawing.Font("Tahoma", 7.8F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(204)));
            this.currentArrowCaptionLabel.Location = new System.Drawing.Point(0, 3);
            this.currentArrowCaptionLabel.Name = "currentArrowCaptionLabel";
            this.currentArrowCaptionLabel.ReadOnly = true;
            this.currentArrowCaptionLabel.Size = new System.Drawing.Size(703, 16);
            this.currentArrowCaptionLabel.TabIndex = 3;
            this.currentArrowCaptionLabel.TabStop = false;
            // 
            // panel3
            // 
            this.panel3.BackColor = System.Drawing.SystemColors.Control;
            this.panel3.Dock = System.Windows.Forms.DockStyle.Top;
            this.panel3.Enabled = false;
            this.panel3.Location = new System.Drawing.Point(0, 0);
            this.panel3.Name = "panel3";
            this.panel3.Size = new System.Drawing.Size(703, 3);
            this.panel3.TabIndex = 5;
            // 
            // flowLayoutPanel1
            // 
            this.flowLayoutPanel1.AutoSize = true;
            this.flowLayoutPanel1.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
            this.flowLayoutPanel1.Controls.Add(this.prevUserActionButton);
            this.flowLayoutPanel1.Controls.Add(this.nextUserActionButton);
            this.flowLayoutPanel1.Controls.Add(this.prevBookmarkButton);
            this.flowLayoutPanel1.Controls.Add(this.nextBookmarkButton);
            this.flowLayoutPanel1.Controls.Add(this.findCurrentTimeButton);
            this.flowLayoutPanel1.Controls.Add(this.zoomInButton);
            this.flowLayoutPanel1.Controls.Add(this.zoomOutButton);
            this.flowLayoutPanel1.Controls.Add(this.collapseResponsesCheckbox);
            this.flowLayoutPanel1.Controls.Add(this.collapseRoleInstancesCheckbox);
            this.flowLayoutPanel1.Controls.Add(this.notificationsButton);
            this.flowLayoutPanel1.Dock = System.Windows.Forms.DockStyle.Top;
            this.flowLayoutPanel1.Location = new System.Drawing.Point(0, 0);
            this.flowLayoutPanel1.Name = "flowLayoutPanel1";
            this.flowLayoutPanel1.Padding = new System.Windows.Forms.Padding(0, 2, 0, 2);
            this.flowLayoutPanel1.Size = new System.Drawing.Size(703, 21);
            this.flowLayoutPanel1.TabIndex = 11;
            // 
            // prevUserActionButton
            // 
            this.prevUserActionButton.AutoSize = true;
            this.prevUserActionButton.ImageAlign = System.Drawing.ContentAlignment.MiddleLeft;
            this.prevUserActionButton.Location = new System.Drawing.Point(3, 2);
            this.prevUserActionButton.Name = "prevUserActionButton";
            this.prevUserActionButton.Padding = new System.Windows.Forms.Padding(17, 0, 0, 0);
            this.prevUserActionButton.Size = new System.Drawing.Size(73, 17);
            this.prevUserActionButton.TabIndex = 0;
            this.prevUserActionButton.TabStop = true;
            this.prevUserActionButton.Text = "<<prev";
            this.prevUserActionButton.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            this.prevUserActionButton.LinkClicked += new System.Windows.Forms.LinkLabelLinkClickedEventHandler(this.toolPanelLinkClicked);
            this.prevUserActionButton.MouseDoubleClick += new System.Windows.Forms.MouseEventHandler(this.toolPanelLinkMouseDoubleClick);
            // 
            // nextUserActionButton
            // 
            this.nextUserActionButton.AutoSize = true;
            this.nextUserActionButton.ImageAlign = System.Drawing.ContentAlignment.MiddleLeft;
            this.nextUserActionButton.Location = new System.Drawing.Point(84, 2);
            this.nextUserActionButton.Margin = new System.Windows.Forms.Padding(5, 0, 3, 0);
            this.nextUserActionButton.Name = "nextUserActionButton";
            this.nextUserActionButton.Padding = new System.Windows.Forms.Padding(17, 0, 0, 0);
            this.nextUserActionButton.Size = new System.Drawing.Size(73, 17);
            this.nextUserActionButton.TabIndex = 1;
            this.nextUserActionButton.TabStop = true;
            this.nextUserActionButton.Text = "next>>";
            this.nextUserActionButton.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            this.nextUserActionButton.LinkClicked += new System.Windows.Forms.LinkLabelLinkClickedEventHandler(this.toolPanelLinkClicked);
            this.nextUserActionButton.MouseDoubleClick += new System.Windows.Forms.MouseEventHandler(this.toolPanelLinkMouseDoubleClick);
            // 
            // prevBookmarkButton
            // 
            this.prevBookmarkButton.AutoSize = true;
            this.prevBookmarkButton.ImageAlign = System.Drawing.ContentAlignment.MiddleLeft;
            this.prevBookmarkButton.Location = new System.Drawing.Point(170, 2);
            this.prevBookmarkButton.Margin = new System.Windows.Forms.Padding(10, 0, 3, 0);
            this.prevBookmarkButton.Name = "prevBookmarkButton";
            this.prevBookmarkButton.Padding = new System.Windows.Forms.Padding(17, 0, 0, 0);
            this.prevBookmarkButton.Size = new System.Drawing.Size(73, 17);
            this.prevBookmarkButton.TabIndex = 2;
            this.prevBookmarkButton.TabStop = true;
            this.prevBookmarkButton.Text = "<<prev";
            this.toolTip1.SetToolTip(this.prevBookmarkButton, "Find bookmark preceding currently selected sequence diagram item (Shift+F2)");
            this.prevBookmarkButton.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            this.prevBookmarkButton.LinkClicked += new System.Windows.Forms.LinkLabelLinkClickedEventHandler(this.toolPanelLinkClicked);
            this.prevBookmarkButton.MouseDoubleClick += new System.Windows.Forms.MouseEventHandler(this.toolPanelLinkMouseDoubleClick);
            // 
            // nextBookmarkButton
            // 
            this.nextBookmarkButton.AutoSize = true;
            this.nextBookmarkButton.ImageAlign = System.Drawing.ContentAlignment.MiddleLeft;
            this.nextBookmarkButton.Location = new System.Drawing.Point(251, 2);
            this.nextBookmarkButton.Margin = new System.Windows.Forms.Padding(5, 0, 3, 0);
            this.nextBookmarkButton.Name = "nextBookmarkButton";
            this.nextBookmarkButton.Padding = new System.Windows.Forms.Padding(17, 0, 0, 0);
            this.nextBookmarkButton.Size = new System.Drawing.Size(73, 17);
            this.nextBookmarkButton.TabIndex = 3;
            this.nextBookmarkButton.TabStop = true;
            this.nextBookmarkButton.Text = "next>>";
            this.toolTip1.SetToolTip(this.nextBookmarkButton, "Find the bookmark following currently selected sequence diagram item (F2)");
            this.nextBookmarkButton.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            this.nextBookmarkButton.LinkClicked += new System.Windows.Forms.LinkLabelLinkClickedEventHandler(this.toolPanelLinkClicked);
            this.nextBookmarkButton.MouseDoubleClick += new System.Windows.Forms.MouseEventHandler(this.toolPanelLinkMouseDoubleClick);
            // 
            // findCurrentTimeButton
            // 
            this.findCurrentTimeButton.AutoSize = true;
            this.findCurrentTimeButton.Location = new System.Drawing.Point(337, 2);
            this.findCurrentTimeButton.Margin = new System.Windows.Forms.Padding(10, 0, 3, 0);
            this.findCurrentTimeButton.Name = "findCurrentTimeButton";
            this.findCurrentTimeButton.Padding = new System.Windows.Forms.Padding(17, 0, 0, 0);
            this.findCurrentTimeButton.Size = new System.Drawing.Size(100, 17);
            this.findCurrentTimeButton.TabIndex = 4;
            this.findCurrentTimeButton.TabStop = true;
            this.findCurrentTimeButton.Text = "current time";
			this.toolTip1.SetToolTip(this.findCurrentTimeButton, "Find the position of log message currently selected in log text view (F6)");
            this.findCurrentTimeButton.ImageAlign = System.Drawing.ContentAlignment.MiddleLeft;
            this.findCurrentTimeButton.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            this.findCurrentTimeButton.LinkClicked += new System.Windows.Forms.LinkLabelLinkClickedEventHandler(this.toolPanelLinkClicked);
            this.findCurrentTimeButton.MouseDoubleClick += new System.Windows.Forms.MouseEventHandler(this.toolPanelLinkMouseDoubleClick);
            // 
            // zoomInButton
            // 
            this.zoomInButton.AutoSize = true;
            this.zoomInButton.ImageAlign = System.Drawing.ContentAlignment.MiddleLeft;
            this.zoomInButton.Location = new System.Drawing.Point(450, 2);
            this.zoomInButton.Margin = new System.Windows.Forms.Padding(10, 0, 3, 0);
            this.zoomInButton.Name = "zoomInButton";
            this.zoomInButton.Padding = new System.Windows.Forms.Padding(17, 0, 0, 0);
            this.zoomInButton.Size = new System.Drawing.Size(73, 17);
            this.zoomInButton.TabIndex = 5;
            this.zoomInButton.TabStop = true;
            this.zoomInButton.Text = "zoom in";
            this.zoomInButton.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            this.zoomInButton.LinkClicked += new System.Windows.Forms.LinkLabelLinkClickedEventHandler(this.toolPanelLinkClicked);
            this.zoomInButton.MouseDoubleClick += new System.Windows.Forms.MouseEventHandler(this.toolPanelLinkMouseDoubleClick);
            // 
            // zoomOutButton
            // 
            this.zoomOutButton.AutoSize = true;
            this.zoomOutButton.ImageAlign = System.Drawing.ContentAlignment.MiddleLeft;
            this.zoomOutButton.Location = new System.Drawing.Point(531, 2);
            this.zoomOutButton.Margin = new System.Windows.Forms.Padding(5, 0, 3, 0);
            this.zoomOutButton.Name = "zoomOutButton";
            this.zoomOutButton.Padding = new System.Windows.Forms.Padding(17, 0, 0, 0);
            this.zoomOutButton.Size = new System.Drawing.Size(84, 17);
            this.zoomOutButton.TabIndex = 6;
            this.zoomOutButton.TabStop = true;
            this.zoomOutButton.Text = "zoom out";
            this.zoomOutButton.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            this.zoomOutButton.LinkClicked += new System.Windows.Forms.LinkLabelLinkClickedEventHandler(this.toolPanelLinkClicked);
            this.zoomOutButton.MouseDoubleClick += new System.Windows.Forms.MouseEventHandler(this.toolPanelLinkMouseDoubleClick);
            // 
            // collapseResponsesCheckbox
            // 
            this.collapseResponsesCheckbox.AutoSize = true;
            this.collapseResponsesCheckbox.Location = new System.Drawing.Point(620, 2);
            this.collapseResponsesCheckbox.Margin = new System.Windows.Forms.Padding(5, 0, 3, 0);
            this.collapseResponsesCheckbox.Name = "collapseResponsesCheckbox";
            this.collapseResponsesCheckbox.Padding = new System.Windows.Forms.Padding(10, 0, 0, 0);
            this.collapseResponsesCheckbox.Size = new System.Drawing.Size(84, 17);
            this.collapseResponsesCheckbox.TabIndex = 7;
            this.collapseResponsesCheckbox.TabStop = true;
            this.collapseResponsesCheckbox.Text = "collapse responses";
            this.collapseResponsesCheckbox.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            this.collapseResponsesCheckbox.Click += collapseResponsesCheckbox_Click;
            // 
			// collapseRoleInstancesCheckbox
            // 
			this.collapseRoleInstancesCheckbox.AutoSize = true;
			this.collapseRoleInstancesCheckbox.Location = new System.Drawing.Point(680, 2);
			this.collapseRoleInstancesCheckbox.Margin = new System.Windows.Forms.Padding(5, 0, 3, 0);
			this.collapseRoleInstancesCheckbox.Name = "collapseRoleInstancesCheckbox";
			this.collapseRoleInstancesCheckbox.Padding = new System.Windows.Forms.Padding(10, 0, 0, 0);
			this.collapseRoleInstancesCheckbox.Size = new System.Drawing.Size(84, 17);
			this.collapseRoleInstancesCheckbox.TabIndex = 8;
			this.collapseRoleInstancesCheckbox.TabStop = true;
			this.collapseRoleInstancesCheckbox.Text = "collapse role instances";
			this.collapseRoleInstancesCheckbox.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
			this.collapseRoleInstancesCheckbox.Click += collapseRoleInstancesCheckbox_Click;
            // 
            // panel6
            // 
            this.panel6.BackColor = System.Drawing.SystemColors.Control;
            this.panel6.Dock = System.Windows.Forms.DockStyle.Top;
            this.panel6.Location = new System.Drawing.Point(0, 21);
            this.panel6.Margin = new System.Windows.Forms.Padding(0);
            this.panel6.Name = "panel6";
            this.panel6.Size = new System.Drawing.Size(703, 2);
            this.panel6.TabIndex = 12;
            // 
            // vScrollBar
            // 
            this.vScrollBar.Dock = System.Windows.Forms.DockStyle.Right;
            this.vScrollBar.Location = new System.Drawing.Point(546, 0);
            this.vScrollBar.Name = "vScrollBar";
            this.vScrollBar.Size = new System.Drawing.Size(21, 303);
            this.vScrollBar.TabIndex = 1;
            this.vScrollBar.Scroll += new System.Windows.Forms.ScrollEventHandler(this.ScrollBar_Scroll);
            // 
            // hScrollBar
            // 
            this.hScrollBar.Dock = System.Windows.Forms.DockStyle.Bottom;
            this.hScrollBar.Location = new System.Drawing.Point(0, 303);
            this.hScrollBar.Name = "hScrollBar";
            this.hScrollBar.Size = new System.Drawing.Size(546, 21);
            this.hScrollBar.TabIndex = 2;
            this.hScrollBar.Scroll += new System.Windows.Forms.ScrollEventHandler(this.ScrollBar_Scroll);
            // 
            // tableLayoutPanel1
            // 
            this.tableLayoutPanel1.ColumnCount = 2;
            this.tableLayoutPanel1.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.tableLayoutPanel1.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle());
            this.tableLayoutPanel1.Controls.Add(this.hScrollBar, 0, 1);
            this.tableLayoutPanel1.Controls.Add(this.arrowsPanel, 0, 0);
            this.tableLayoutPanel1.Controls.Add(this.vScrollBar, 1, 0);
            this.tableLayoutPanel1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.tableLayoutPanel1.Location = new System.Drawing.Point(136, 87);
            this.tableLayoutPanel1.Margin = new System.Windows.Forms.Padding(0);
            this.tableLayoutPanel1.Name = "tableLayoutPanel1";
            this.tableLayoutPanel1.RowCount = 2;
            this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle());
            this.tableLayoutPanel1.Size = new System.Drawing.Size(567, 324);
            this.tableLayoutPanel1.TabIndex = 13;
            // 
            // arrowsPanel
            // 
            this.arrowsPanel.BackColor = System.Drawing.Color.White;
            this.arrowsPanel.DisplayPaintTime = false;
            this.arrowsPanel.Dock = System.Windows.Forms.DockStyle.Fill;
            this.arrowsPanel.FocuslessMouseWheel = true;
            this.arrowsPanel.Location = new System.Drawing.Point(0, 0);
            this.arrowsPanel.Margin = new System.Windows.Forms.Padding(0);
            this.arrowsPanel.Name = "arrowsPanel";
            this.arrowsPanel.Size = new System.Drawing.Size(546, 303);
            this.arrowsPanel.TabIndex = 0;
            this.arrowsPanel.TabStop = true;
            this.arrowsPanel.MouseWheel += new System.Windows.Forms.MouseEventHandler(this.arrowsPanel_MouseWheel);
            this.arrowsPanel.Paint += new System.Windows.Forms.PaintEventHandler(this.arrowsPanel_Paint);
            this.arrowsPanel.MouseDown += new System.Windows.Forms.MouseEventHandler(this.arrowsPanel_MouseDown);
            this.arrowsPanel.MouseMove += new System.Windows.Forms.MouseEventHandler(this.arrowsPanel_MouseMove);
            this.arrowsPanel.MouseUp += new System.Windows.Forms.MouseEventHandler(this.arrowsPanel_MouseUp);
            this.arrowsPanel.PreviewKeyDown += new System.Windows.Forms.PreviewKeyDownEventHandler(this.arrowsPanel_PreviewKeyDown);
            this.arrowsPanel.Resize += new System.EventHandler(this.arrowsPanel_Resize);
            this.arrowsPanel.Controls.Add(this.toastNotificationsListControl);
            // 
            // rolesCaptionsPanel
            // 
            this.rolesCaptionsPanel.BackColor = System.Drawing.Color.White;
            this.rolesCaptionsPanel.DisplayPaintTime = false;
            this.rolesCaptionsPanel.Dock = System.Windows.Forms.DockStyle.Top;
            this.rolesCaptionsPanel.FocuslessMouseWheel = false;
            this.rolesCaptionsPanel.Location = new System.Drawing.Point(136, 23);
            this.rolesCaptionsPanel.Name = "rolesCaptionsPanel";
            this.rolesCaptionsPanel.Size = new System.Drawing.Size(567, 64);
            this.rolesCaptionsPanel.TabIndex = 2;
            this.rolesCaptionsPanel.MouseWheel += new System.Windows.Forms.MouseEventHandler(this.arrowsPanel_MouseWheel);
            this.rolesCaptionsPanel.SetCursor += new System.EventHandler<System.Windows.Forms.HandledMouseEventArgs>(this.rolesCaptionsPanel_SetCursor);
            this.rolesCaptionsPanel.Paint += new System.Windows.Forms.PaintEventHandler(this.rolesCaptionsPanel_Paint);
            this.rolesCaptionsPanel.MouseDown += new System.Windows.Forms.MouseEventHandler(this.rolesCaptionsPanel_MouseDown);
            this.rolesCaptionsPanel.PreviewKeyDown += new System.Windows.Forms.PreviewKeyDownEventHandler(this.arrowsPanel_PreviewKeyDown);
            // 
            // leftPanel
            // 
            this.leftPanel.BackColor = System.Drawing.Color.White;
            this.leftPanel.Controls.Add(this.tagsListContainerPanel);
            this.leftPanel.Controls.Add(this.panel2);
            this.leftPanel.DisplayPaintTime = false;
            this.leftPanel.Dock = System.Windows.Forms.DockStyle.Left;
            this.leftPanel.FocuslessMouseWheel = false;
            this.leftPanel.Location = new System.Drawing.Point(0, 23);
            this.leftPanel.Name = "leftPanel";
            this.leftPanel.Size = new System.Drawing.Size(136, 388);
            this.leftPanel.TabIndex = 10;
            this.leftPanel.Paint += new System.Windows.Forms.PaintEventHandler(this.leftPanel_Paint);
            this.leftPanel.MouseDown += new System.Windows.Forms.MouseEventHandler(this.leftPanel_MouseDown);
            // 
            // tagsListContainerPanel
            // 
            this.tagsListContainerPanel.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.tagsListContainerPanel.BackColor = System.Drawing.SystemColors.Control;
            this.tagsListContainerPanel.Controls.Add(this.tagsListControl);
            this.tagsListContainerPanel.Controls.Add(this.quickSearchEditBox);
            this.tagsListContainerPanel.Location = new System.Drawing.Point(0, 0);
            this.tagsListContainerPanel.Margin = new System.Windows.Forms.Padding(0);
            this.tagsListContainerPanel.Name = "tagsListContainerPanel";
            this.tagsListContainerPanel.Padding = new System.Windows.Forms.Padding(0, 0, 2, 2);
            this.tagsListContainerPanel.Size = new System.Drawing.Size(136, 64);
            this.tagsListContainerPanel.TabIndex = 0;
            // 
            // tagsListControl
            // 
            this.tagsListControl.AutoSize = true;
            this.tagsListControl.BackColor = System.Drawing.Color.White;
            this.tagsListControl.Dock = System.Windows.Forms.DockStyle.Fill;
            this.tagsListControl.Location = new System.Drawing.Point(0, 0);
            this.tagsListControl.Margin = new System.Windows.Forms.Padding(0);
            this.tagsListControl.Name = "tagsListControl";
            this.tagsListControl.Size = new System.Drawing.Size(134, 38);
            this.tagsListControl.TabIndex = 3;
            // 
            // panel2
            // 
            this.panel2.BackColor = System.Drawing.SystemColors.Control;
            this.panel2.Dock = System.Windows.Forms.DockStyle.Right;
            this.panel2.Enabled = false;
            this.panel2.Location = new System.Drawing.Point(134, 0);
            this.panel2.Name = "panel2";
            this.panel2.Size = new System.Drawing.Size(2, 388);
            this.panel2.TabIndex = 0;
            // 
            // quickSearchEditBox
            // 
            this.quickSearchEditBox.BackColor = System.Drawing.Color.DarkGray;
            this.quickSearchEditBox.DefaultBorderColor = System.Drawing.Color.DarkGray;
            this.quickSearchEditBox.Dock = System.Windows.Forms.DockStyle.Bottom;
            this.quickSearchEditBox.FocusedBorderColor = System.Drawing.Color.Blue;
            this.quickSearchEditBox.Location = new System.Drawing.Point(0, 38);
            this.quickSearchEditBox.Margin = new System.Windows.Forms.Padding(0);
            this.quickSearchEditBox.Name = "quickSearchEditBox";
            this.quickSearchEditBox.Padding = new System.Windows.Forms.Padding(1);
            this.quickSearchEditBox.Size = new System.Drawing.Size(134, 24);
            this.quickSearchEditBox.TabIndex = 4;
            //
            // toastNotificationsListControl
            //
            this.toastNotificationsListControl.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.toastNotificationsListControl.AutoSize = true;
            this.toastNotificationsListControl.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
            this.toastNotificationsListControl.Location = new System.Drawing.Point(398, 5);
            this.toastNotificationsListControl.Margin = new System.Windows.Forms.Padding(2);
            this.toastNotificationsListControl.Name = "toastNotificationsListControl";
            this.toastNotificationsListControl.Size = new System.Drawing.Size(144, 119);
            this.toastNotificationsListControl.TabIndex = 0;
            // 
            // notificationsButton
            // 
            this.notificationsButton.FlatAppearance.BorderSize = 0;
            this.notificationsButton.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.notificationsButton.Location = new System.Drawing.Point(0, 119);
            this.notificationsButton.Name = "notificationsButton";
            this.notificationsButton.Size = new System.Drawing.Size(18, 18);
            this.notificationsButton.TabIndex = 12;
            this.notificationsButton.UseVisualStyleBackColor = true;
            this.notificationsButton.Visible = false;
            this.toolTip1.SetToolTip(this.notificationsButton, "This view has unresolved issues. Click to see.");
            this.notificationsButton.Click += notificationsButton_Click;
            this.notificationsButton.Margin = new System.Windows.Forms.Padding(5, 0, 3, 0);

            // 
            // SequenceDiagramVisualizerControl
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(120F, 120F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Dpi;
            this.BackColor = System.Drawing.Color.White;
            this.Controls.Add(this.tableLayoutPanel1);
            this.Controls.Add(this.rolesCaptionsPanel);
            this.Controls.Add(this.leftPanel);
            this.Controls.Add(this.panel1);
            this.Controls.Add(this.panel6);
            this.Controls.Add(this.flowLayoutPanel1);
            this.Font = new System.Drawing.Font("Tahoma", 8.25F);
            this.Name = "SequenceDiagramVisualizerControl";
            this.Size = new System.Drawing.Size(703, 457);
            this.panel1.ResumeLayout(false);
            this.panel1.PerformLayout();
            this.flowLayoutPanel1.ResumeLayout(false);
            this.flowLayoutPanel1.PerformLayout();
            this.tableLayoutPanel1.ResumeLayout(false);
            this.leftPanel.ResumeLayout(false);
            this.tagsListContainerPanel.ResumeLayout(false);
            this.tagsListContainerPanel.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();
		}

		#endregion

		private DoubleBufferedPanel arrowsPanel;
		private DoubleBufferedPanel rolesCaptionsPanel;
		private System.Windows.Forms.Panel panel1;
		private System.Windows.Forms.LinkLabel currentArrowDescription;
		private System.Windows.Forms.TextBox currentArrowCaptionLabel;
		private DoubleBufferedPanel leftPanel;
		private System.Windows.Forms.Panel panel2;
		private System.Windows.Forms.Panel panel3;
		private TagsListControl tagsListControl;
		private System.Windows.Forms.Panel tagsListContainerPanel;
		private System.Windows.Forms.FlowLayoutPanel flowLayoutPanel1;
		private System.Windows.Forms.LinkLabel prevUserActionButton;
		private System.Windows.Forms.LinkLabel nextUserActionButton;
		private System.Windows.Forms.LinkLabel prevBookmarkButton;
		private System.Windows.Forms.LinkLabel nextBookmarkButton;
		private System.Windows.Forms.LinkLabel findCurrentTimeButton;
		private System.Windows.Forms.LinkLabel zoomInButton;
		private System.Windows.Forms.LinkLabel zoomOutButton;
		private System.Windows.Forms.CheckBox collapseResponsesCheckbox;
		private System.Windows.Forms.CheckBox collapseRoleInstancesCheckbox;
		private System.Windows.Forms.Panel panel6;
		private System.Windows.Forms.VScrollBar vScrollBar;
		private System.Windows.Forms.HScrollBar hScrollBar;
		private System.Windows.Forms.TableLayoutPanel tableLayoutPanel1;
        private LogJoint.UI.QuickSearchTextBox.BorderedQuickSearchTextBox quickSearchEditBox;
        private ToastNotificationsListControl toastNotificationsListControl;
        private System.Windows.Forms.Button notificationsButton;
		private System.Windows.Forms.ToolTip toolTip1;
	}
}
