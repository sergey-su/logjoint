namespace LogJoint.UI.Postprocessing.TimelineVisualizer
{
    partial class TimelineVisualizerControl
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(TimelineVisualizerControl));
            this.activitiesContainer = new System.Windows.Forms.SplitContainer();
            this.panel1 = new System.Windows.Forms.Panel();
            this.activitiesScrollBar = new System.Windows.Forms.VScrollBar();
            this.panel5 = new System.Windows.Forms.Panel();
            this.panel2 = new System.Windows.Forms.Panel();
            this.panel3 = new System.Windows.Forms.Panel();
            this.currentActivitySourceLinkLabel = new System.Windows.Forms.LinkLabel();
            this.currentActivityDescription = new System.Windows.Forms.LinkLabel();
            this.currentActivityCaptionLabel = new System.Windows.Forms.TextBox();
            this.panel4 = new System.Windows.Forms.Panel();
            this.flowLayoutPanel1 = new System.Windows.Forms.FlowLayoutPanel();
            this.prevUserActionButton = new System.Windows.Forms.LinkLabel();
            this.nextUserActionButton = new System.Windows.Forms.LinkLabel();
            this.prevBookmarkButton = new System.Windows.Forms.LinkLabel();
            this.nextBookmarkButton = new System.Windows.Forms.LinkLabel();
            this.findCurrentTimeButton = new System.Windows.Forms.LinkLabel();
            this.zoomInButton = new System.Windows.Forms.LinkLabel();
            this.zoomOutButton = new System.Windows.Forms.LinkLabel();
            this.panel6 = new System.Windows.Forms.Panel();
            this.activitesCaptionsPanel = new LogJoint.UI.DoubleBufferedPanel();
            this.tagsListControl = new LogJoint.UI.TagsListControl();
            this.quickSearchEditBox = new LogJoint.UI.QuickSearchTextBox.BorderedQuickSearchTextBox();
            this.toastNotificationsListControl = new ToastNotificationsListControl();
            this.notificationsButton = new System.Windows.Forms.Button();
            this.toolTip1 = new System.Windows.Forms.ToolTip(this.components);
            this.activitiesViewPanel = new LogJoint.UI.DoubleBufferedPanel();
            this.navigationPanel = new LogJoint.UI.DoubleBufferedPanel();
            this.noContentLink = new System.Windows.Forms.LinkLabel();
            ((System.ComponentModel.ISupportInitialize)(this.activitiesContainer)).BeginInit();
            this.activitiesContainer.Panel1.SuspendLayout();
            this.activitiesContainer.Panel2.SuspendLayout();
            this.activitiesContainer.SuspendLayout();
            this.panel1.SuspendLayout();
            this.panel3.SuspendLayout();
            this.flowLayoutPanel1.SuspendLayout();
            this.activitesCaptionsPanel.SuspendLayout();
            this.SuspendLayout();
            // 
            // activitiesContainer
            // 
            this.activitiesContainer.BackColor = System.Drawing.Color.LightGray;
            this.activitiesContainer.Dock = System.Windows.Forms.DockStyle.Fill;
            this.activitiesContainer.FixedPanel = System.Windows.Forms.FixedPanel.Panel1;
            this.activitiesContainer.Location = new System.Drawing.Point(0, 49);
            this.activitiesContainer.Margin = new System.Windows.Forms.Padding(2);
            this.activitiesContainer.Name = "activitiesContainer";
            // 
            // activitiesContainer.Panel1
            // 
            this.activitiesContainer.Panel1.Controls.Add(this.activitesCaptionsPanel);
            // 
            // activitiesContainer.Panel2
            // 
            this.activitiesContainer.Panel2.Controls.Add(this.activitiesViewPanel);
            this.activitiesContainer.Panel2.Controls.Add(this.panel1);
            this.activitiesContainer.Size = new System.Drawing.Size(739, 322);
            this.activitiesContainer.SplitterDistance = 260;
            this.activitiesContainer.SplitterWidth = 2;
            this.activitiesContainer.TabIndex = 0;
            this.activitiesContainer.DoubleClick += new System.EventHandler(this.activitiesContainer_DoubleClick);
            // 
            // panel1
            // 
            this.panel1.BackColor = System.Drawing.Color.White;
            this.panel1.Controls.Add(this.activitiesScrollBar);
            this.panel1.Controls.Add(this.panel5);
            this.panel1.Dock = System.Windows.Forms.DockStyle.Right;
            this.panel1.Location = new System.Drawing.Point(457, 0);
            this.panel1.Margin = new System.Windows.Forms.Padding(0);
            this.panel1.Name = "panel1";
            this.panel1.Size = new System.Drawing.Size(20, 322);
            this.panel1.TabIndex = 2;
            // 
            // activitiesScrollBar
            // 
            this.activitiesScrollBar.Dock = System.Windows.Forms.DockStyle.Fill;
            this.activitiesScrollBar.Location = new System.Drawing.Point(0, 50);
            this.activitiesScrollBar.Name = "activitiesScrollBar";
            this.activitiesScrollBar.Size = new System.Drawing.Size(20, 272);
            this.activitiesScrollBar.TabIndex = 0;
            this.activitiesScrollBar.Scroll += new System.Windows.Forms.ScrollEventHandler(this.vScrollBar1_Scroll);
            // 
            // panel5
            // 
            this.panel5.Dock = System.Windows.Forms.DockStyle.Top;
            this.panel5.Location = new System.Drawing.Point(0, 0);
            this.panel5.Margin = new System.Windows.Forms.Padding(0);
            this.panel5.Name = "panel5";
            this.panel5.Size = new System.Drawing.Size(20, 50);
            this.panel5.TabIndex = 1;
            // 
            // panel2
            // 
            this.panel2.BackColor = System.Drawing.SystemColors.Control;
            this.panel2.Dock = System.Windows.Forms.DockStyle.Top;
            this.panel2.Location = new System.Drawing.Point(0, 49);
            this.panel2.Margin = new System.Windows.Forms.Padding(0);
            this.panel2.Name = "panel2";
            this.panel2.Size = new System.Drawing.Size(739, 2);
            this.panel2.TabIndex = 2;
            // 
            // panel3
            // 
            this.panel3.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
            this.panel3.Controls.Add(this.currentActivitySourceLinkLabel);
            this.panel3.Controls.Add(this.currentActivityDescription);
            this.panel3.Controls.Add(this.currentActivityCaptionLabel);
            this.panel3.Dock = System.Windows.Forms.DockStyle.Bottom;
            this.panel3.Location = new System.Drawing.Point(0, 373);
            this.panel3.Margin = new System.Windows.Forms.Padding(2);
            this.panel3.Name = "panel3";
            this.panel3.Size = new System.Drawing.Size(739, 52);
            this.panel3.TabIndex = 0;
            // 
            // currentActivitySourceLinkLabel
            // 
            this.currentActivitySourceLinkLabel.AutoEllipsis = true;
            this.currentActivitySourceLinkLabel.Dock = System.Windows.Forms.DockStyle.Fill;
            this.currentActivitySourceLinkLabel.Location = new System.Drawing.Point(0, 34);
            this.currentActivitySourceLinkLabel.Margin = new System.Windows.Forms.Padding(2, 0, 2, 0);
            this.currentActivitySourceLinkLabel.Name = "currentActivitySourceLinkLabel";
            this.currentActivitySourceLinkLabel.Padding = new System.Windows.Forms.Padding(0);
            this.currentActivitySourceLinkLabel.Size = new System.Drawing.Size(739, 18);
            this.currentActivitySourceLinkLabel.TabIndex = 2;
            this.currentActivitySourceLinkLabel.LinkClicked += new System.Windows.Forms.LinkLabelLinkClickedEventHandler(this.currentActivitySourceLinkLabel_LinkClicked);
            // 
            // currentActivityDescription
            // 
            this.currentActivityDescription.Dock = System.Windows.Forms.DockStyle.Top;
            this.currentActivityDescription.Location = new System.Drawing.Point(0, 16);
            this.currentActivityDescription.Margin = new System.Windows.Forms.Padding(2, 0, 2, 0);
            this.currentActivityDescription.Name = "currentActivityDescription";
            this.currentActivityDescription.Size = new System.Drawing.Size(739, 18);
            this.currentActivityDescription.TabIndex = 2;
            this.currentActivityDescription.LinkClicked += new System.Windows.Forms.LinkLabelLinkClickedEventHandler(this.currentActivityDescription_LinkClicked);
            // 
            // currentActivityCaptionLabel
            // 
            this.currentActivityCaptionLabel.BackColor = System.Drawing.Color.White;
            this.currentActivityCaptionLabel.BorderStyle = System.Windows.Forms.BorderStyle.None;
            this.currentActivityCaptionLabel.Dock = System.Windows.Forms.DockStyle.Top;
            this.currentActivityCaptionLabel.Location = new System.Drawing.Point(0, 0);
            this.currentActivityCaptionLabel.Margin = new System.Windows.Forms.Padding(2);
            this.currentActivityCaptionLabel.Name = "currentActivityCaptionLabel";
            this.currentActivityCaptionLabel.ReadOnly = true;
            this.currentActivityCaptionLabel.Size = new System.Drawing.Size(739, 16);
            this.currentActivityCaptionLabel.TabIndex = 1;
            // 
            // panel4
            // 
            this.panel4.BackColor = System.Drawing.SystemColors.Control;
            this.panel4.Dock = System.Windows.Forms.DockStyle.Bottom;
            this.panel4.Location = new System.Drawing.Point(0, 371);
            this.panel4.Margin = new System.Windows.Forms.Padding(0);
            this.panel4.Name = "panel4";
            this.panel4.Size = new System.Drawing.Size(739, 2);
            this.panel4.TabIndex = 3;
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
            this.flowLayoutPanel1.Controls.Add(this.notificationsButton);
            this.flowLayoutPanel1.Dock = System.Windows.Forms.DockStyle.Top;
            this.flowLayoutPanel1.Location = new System.Drawing.Point(0, 0);
            this.flowLayoutPanel1.Name = "flowLayoutPanel1";
            this.flowLayoutPanel1.Padding = new System.Windows.Forms.Padding(0, 2, 0, 2);
            this.flowLayoutPanel1.Size = new System.Drawing.Size(739, 21);
            this.flowLayoutPanel1.TabIndex = 4;
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
            this.toolTip1.SetToolTip(this.prevBookmarkButton, "Find the first bookmark having timestamp smaller than the middle of this view (Shift+F2)");
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
            this.toolTip1.SetToolTip(this.nextBookmarkButton, "Find the first bookmark having timestamp bigger than the middle of this view (F2)");
            this.nextBookmarkButton.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            this.nextBookmarkButton.LinkClicked += new System.Windows.Forms.LinkLabelLinkClickedEventHandler(this.toolPanelLinkClicked);
            this.nextBookmarkButton.MouseDoubleClick += new System.Windows.Forms.MouseEventHandler(this.toolPanelLinkMouseDoubleClick);
            // 
            // findCurrentTimeButton
            // 
            this.findCurrentTimeButton.AutoSize = true;
            this.findCurrentTimeButton.ImageAlign = System.Drawing.ContentAlignment.MiddleLeft;
            this.findCurrentTimeButton.Location = new System.Drawing.Point(337, 2);
            this.findCurrentTimeButton.Margin = new System.Windows.Forms.Padding(10, 0, 3, 0);
            this.findCurrentTimeButton.Name = "findCurrentTimeButton";
            this.findCurrentTimeButton.Padding = new System.Windows.Forms.Padding(17, 0, 0, 0);
            this.findCurrentTimeButton.Size = new System.Drawing.Size(100, 17);
            this.findCurrentTimeButton.TabIndex = 4;
            this.findCurrentTimeButton.TabStop = true;
            this.findCurrentTimeButton.Text = "current time";
            this.toolTip1.SetToolTip(this.findCurrentTimeButton, "Scroll view to see the time of log message currently selected in log text view (F6)");
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
            // noContentLink
            // 
            this.noContentLink.AutoSize = true;
            this.noContentLink.Location = new System.Drawing.Point(20, 65);
            this.noContentLink.Name = "noContentLink";
            this.noContentLink.Size = new System.Drawing.Size(73, 17);
            this.noContentLink.TabIndex = 3;
            this.noContentLink.TabStop = true;
            this.noContentLink.Visible = false;
            this.noContentLink.Text = "";
            this.noContentLink.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.noContentLink.BackColor = System.Drawing.Color.Cornsilk;
            this.noContentLink.Padding = new System.Windows.Forms.Padding(6, 4, 6, 0);
            this.noContentLink.TextAlign = System.Drawing.ContentAlignment.TopLeft;
            this.noContentLink.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)));
            this.noContentLink.LinkClicked += new System.Windows.Forms.LinkLabelLinkClickedEventHandler(this.noContentLinkClicked);
            // 
            // panel6
            // 
            this.panel6.BackColor = System.Drawing.SystemColors.Control;
            this.panel6.Dock = System.Windows.Forms.DockStyle.Top;
            this.panel6.Location = new System.Drawing.Point(0, 21);
            this.panel6.Margin = new System.Windows.Forms.Padding(0);
            this.panel6.Name = "panel6";
            this.panel6.Size = new System.Drawing.Size(739, 2);
            this.panel6.TabIndex = 0;
            // 
            // activitesCaptionsPanel
            // 
            this.activitesCaptionsPanel.BackColor = System.Drawing.Color.White;
            this.activitesCaptionsPanel.Controls.Add(this.tagsListControl);
            this.activitesCaptionsPanel.Controls.Add(this.quickSearchEditBox);
            this.activitesCaptionsPanel.DisplayPaintTime = false;
            this.activitesCaptionsPanel.Dock = System.Windows.Forms.DockStyle.Fill;
            this.activitesCaptionsPanel.FocuslessMouseWheel = true;
            this.activitesCaptionsPanel.Location = new System.Drawing.Point(0, 0);
            this.activitesCaptionsPanel.Margin = new System.Windows.Forms.Padding(2);
            this.activitesCaptionsPanel.Name = "activitesCaptionsPanel";
            this.activitesCaptionsPanel.Size = new System.Drawing.Size(260, 322);
            this.activitesCaptionsPanel.TabIndex = 0;
            this.activitesCaptionsPanel.TabStop = true;
            this.activitesCaptionsPanel.KeyPress += new System.Windows.Forms.KeyPressEventHandler(this.activitiesViewPanel_KeyPress);
            this.activitesCaptionsPanel.Paint += new System.Windows.Forms.PaintEventHandler(this.activitesCaptionsPanel_Paint);
            this.activitesCaptionsPanel.MouseDown += new System.Windows.Forms.MouseEventHandler(this.activitiesPanel_MouseDown);
            this.activitesCaptionsPanel.PreviewKeyDown += new System.Windows.Forms.PreviewKeyDownEventHandler(this.activitiesPanel_PreviewKeyDown);
            this.activitesCaptionsPanel.Resize += new System.EventHandler(this.activitesCaptionsPanel_Resize);
            // 
            // tagsListControl
            // 
            this.tagsListControl.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
            | System.Windows.Forms.AnchorStyles.Right)));
            this.tagsListControl.AutoSize = true;
            this.tagsListControl.BackColor = System.Drawing.Color.White;
            this.tagsListControl.Location = new System.Drawing.Point(1, 6);
            this.tagsListControl.Margin = new System.Windows.Forms.Padding(0);
            this.tagsListControl.Name = "tagsListControl";
            this.tagsListControl.Size = new System.Drawing.Size(256, 20);
            this.tagsListControl.TabIndex = 2;
            //
            // quickSearchEditBox
            // 
            this.quickSearchEditBox.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
            | System.Windows.Forms.AnchorStyles.Right)));
            this.quickSearchEditBox.BorderStyle = System.Windows.Forms.BorderStyle.None;
            this.quickSearchEditBox.Location = new System.Drawing.Point(1, 1);
            this.quickSearchEditBox.Margin = new System.Windows.Forms.Padding(0);
            this.quickSearchEditBox.Name = "quickSearchEditBox";
            this.quickSearchEditBox.Size = new System.Drawing.Size(251, 16);
            this.quickSearchEditBox.TabIndex = 0;
            // 
            // activitiesViewPanel
            // 
            this.activitiesViewPanel.BackColor = System.Drawing.Color.White;
            this.activitiesViewPanel.DisplayPaintTime = false;
            this.activitiesViewPanel.Dock = System.Windows.Forms.DockStyle.Fill;
            this.activitiesViewPanel.FocuslessMouseWheel = true;
            this.activitiesViewPanel.Location = new System.Drawing.Point(0, 0);
            this.activitiesViewPanel.Margin = new System.Windows.Forms.Padding(0);
            this.activitiesViewPanel.Name = "activitiesViewPanel";
            this.activitiesViewPanel.Size = new System.Drawing.Size(457, 322);
            this.activitiesViewPanel.TabIndex = 0;
            this.activitiesViewPanel.TabStop = true;
            this.activitiesViewPanel.KeyPress += new System.Windows.Forms.KeyPressEventHandler(this.activitiesViewPanel_KeyPress);
            this.activitiesViewPanel.SetCursor += new System.EventHandler<System.Windows.Forms.HandledMouseEventArgs>(this.activitiesViewPanel_SetCursor);
            this.activitiesViewPanel.Paint += new System.Windows.Forms.PaintEventHandler(this.activitiesViewPanel_Paint);
            this.activitiesViewPanel.MouseDown += new System.Windows.Forms.MouseEventHandler(this.activitiesPanel_MouseDown);
            this.activitiesViewPanel.MouseMove += new System.Windows.Forms.MouseEventHandler(this.activitiesViewPanel_MouseMove);
            this.activitiesViewPanel.MouseUp += new System.Windows.Forms.MouseEventHandler(this.activitiesViewPanel_MouseUp);
            this.activitiesViewPanel.PreviewKeyDown += new System.Windows.Forms.PreviewKeyDownEventHandler(this.activitiesPanel_PreviewKeyDown);
            this.activitiesViewPanel.Resize += new System.EventHandler(this.activitiesViewPanel_Resize);
            this.activitiesViewPanel.Controls.Add(toastNotificationsListControl);
            this.activitiesViewPanel.Controls.Add(noContentLink);
            // 
            // navigationPanel
            // 
            this.navigationPanel.BackColor = System.Drawing.Color.White;
            this.navigationPanel.DisplayPaintTime = false;
            this.navigationPanel.Dock = System.Windows.Forms.DockStyle.Top;
            this.navigationPanel.FocuslessMouseWheel = false;
            this.navigationPanel.Location = new System.Drawing.Point(0, 23);
            this.navigationPanel.Margin = new System.Windows.Forms.Padding(2);
            this.navigationPanel.Name = "navigationPanel";
            this.navigationPanel.Size = new System.Drawing.Size(739, 26);
            this.navigationPanel.TabIndex = 1;
            this.navigationPanel.SetCursor += new System.EventHandler<System.Windows.Forms.HandledMouseEventArgs>(this.navigationPanel_SetCursor);
            this.navigationPanel.Paint += new System.Windows.Forms.PaintEventHandler(this.navigationPanel_Paint);
            this.navigationPanel.MouseDown += new System.Windows.Forms.MouseEventHandler(this.navigationPanel_MouseDown);
            this.navigationPanel.MouseMove += new System.Windows.Forms.MouseEventHandler(this.navigationPanel_MouseMove);
            this.navigationPanel.MouseUp += new System.Windows.Forms.MouseEventHandler(this.navigationPanel_MouseUp);
            this.navigationPanel.Resize += new System.EventHandler(this.navigationViewPanel_Resize);
            //
            // toastNotificationsListControl
            //
            this.toastNotificationsListControl.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.toastNotificationsListControl.AutoSize = true;
            this.toastNotificationsListControl.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
            this.toastNotificationsListControl.Location = new System.Drawing.Point(300, 55);
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
            this.notificationsButton.Size = new System.Drawing.Size(17, 17);
            this.notificationsButton.TabIndex = 12;
            this.notificationsButton.UseVisualStyleBackColor = true;
            this.notificationsButton.Visible = false;
            this.toolTip1.SetToolTip(this.notificationsButton, "This view has unresolved issues. Click to see.");
            this.notificationsButton.Click += notificationsButton_Click;
            this.notificationsButton.Margin = new System.Windows.Forms.Padding(7, 0, 3, 0);
            // 
            // TimelineVisualizerControl
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(120F, 120F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Dpi;
            this.BackColor = System.Drawing.Color.White;
            this.Controls.Add(this.panel2);
            this.Controls.Add(this.activitiesContainer);
            this.Controls.Add(this.navigationPanel);
            this.Controls.Add(this.panel4);
            this.Controls.Add(this.panel3);
            this.Controls.Add(this.panel6);
            this.Controls.Add(this.flowLayoutPanel1);
            this.Font = new System.Drawing.Font("Tahoma", 7.8F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(204)));
            this.Margin = new System.Windows.Forms.Padding(2);
            this.Name = "TimelineVisualizerControl";
            this.Size = new System.Drawing.Size(739, 425);
            this.activitiesContainer.Panel1.ResumeLayout(false);
            this.activitiesContainer.Panel2.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.activitiesContainer)).EndInit();
            this.activitiesContainer.ResumeLayout(false);
            this.panel1.ResumeLayout(false);
            this.panel3.ResumeLayout(false);
            this.panel3.PerformLayout();
            this.flowLayoutPanel1.ResumeLayout(false);
            this.flowLayoutPanel1.PerformLayout();
            this.activitesCaptionsPanel.ResumeLayout(false);
            this.activitesCaptionsPanel.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.SplitContainer activitiesContainer;
        private DoubleBufferedPanel activitesCaptionsPanel;
        private DoubleBufferedPanel activitiesViewPanel;
        private System.Windows.Forms.Panel panel1;
        private System.Windows.Forms.VScrollBar activitiesScrollBar;
        private DoubleBufferedPanel navigationPanel;
        private System.Windows.Forms.Panel panel2;
        private System.Windows.Forms.Panel panel3;
        private System.Windows.Forms.Panel panel4;
        private System.Windows.Forms.LinkLabel currentActivityDescription;
        private System.Windows.Forms.TextBox currentActivityCaptionLabel;
        private System.Windows.Forms.LinkLabel currentActivitySourceLinkLabel;
        private LogJoint.UI.QuickSearchTextBox.BorderedQuickSearchTextBox quickSearchEditBox;
        private LogJoint.UI.TagsListControl tagsListControl;
        private System.Windows.Forms.Panel panel5;
        private System.Windows.Forms.FlowLayoutPanel flowLayoutPanel1;
        private System.Windows.Forms.LinkLabel prevUserActionButton;
        private System.Windows.Forms.LinkLabel nextUserActionButton;
        private System.Windows.Forms.Panel panel6;
        private System.Windows.Forms.LinkLabel prevBookmarkButton;
        private System.Windows.Forms.LinkLabel nextBookmarkButton;
        private System.Windows.Forms.LinkLabel findCurrentTimeButton;
        private System.Windows.Forms.LinkLabel zoomInButton;
        private System.Windows.Forms.LinkLabel zoomOutButton;
        private ToastNotificationsListControl toastNotificationsListControl;
        private System.Windows.Forms.Button notificationsButton;
        private System.Windows.Forms.ToolTip toolTip1;
        private System.Windows.Forms.LinkLabel noContentLink;
    }
}
