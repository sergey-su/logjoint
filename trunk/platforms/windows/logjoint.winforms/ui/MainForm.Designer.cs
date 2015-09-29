namespace LogJoint.UI
{
    partial class MainForm
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
			this.components = new System.ComponentModel.Container();
			System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(MainForm));
			this.imageList1 = new System.Windows.Forms.ImageList(this.components);
			this.openFileDialog1 = new System.Windows.Forms.OpenFileDialog();
			this.mruContextMenuStrip = new System.Windows.Forms.ContextMenuStrip(this.components);
			this.statusStrip1 = new System.Windows.Forms.StatusStrip();
			this.toolStripAnalizingLabel = new System.Windows.Forms.ToolStripStatusLabel();
			this.toolStripStatusLabel = new System.Windows.Forms.ToolStripStatusLabel();
			this.cancelLongRunningProcessDropDownButton = new System.Windows.Forms.ToolStripDropDownButton();
			this.aboutLinkLabel = new System.Windows.Forms.LinkLabel();
			this.button2 = new System.Windows.Forms.Button();
			this.button3 = new System.Windows.Forms.Button();
			this.button4 = new System.Windows.Forms.Button();
			this.button5 = new System.Windows.Forms.Button();
			this.toolTip1 = new System.Windows.Forms.ToolTip(this.components);
			this.button6 = new System.Windows.Forms.Button();
			this.panel2 = new System.Windows.Forms.Panel();
			this.splitContainer_Menu_Workspace = new System.Windows.Forms.SplitContainer();
			this.menuTabControl = new System.Windows.Forms.TabControl();
			this.sourcesTabPage = new System.Windows.Forms.TabPage();
			this.threadsTabPage = new System.Windows.Forms.TabPage();
			this.filtersTabPage = new System.Windows.Forms.TabPage();
			this.highlightTabPage = new System.Windows.Forms.TabPage();
			this.searchTabPage = new System.Windows.Forms.TabPage();
			this.navigationTabPage = new System.Windows.Forms.TabPage();
			this.splitContainer_Timeline_Log = new System.Windows.Forms.SplitContainer();
			this.optionsContextMenu = new System.Windows.Forms.ContextMenuStrip(this.components);
			this.configurationToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.aboutToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.restartAppToUpdatePicture = new System.Windows.Forms.PictureBox();
			this.toolStripAnalizingImage = new System.Windows.Forms.ToolStripStatusLabel();
			this.toolStripStatusImage = new System.Windows.Forms.ToolStripStatusLabel();
			this.cancelLongRunningProcessLabel = new System.Windows.Forms.ToolStripStatusLabel();
			this.sourcesListView = new LogJoint.UI.SourcesManagementView();
			this.threadsListView = new LogJoint.UI.ThreadsListView();
			this.displayFiltersManagementView = new LogJoint.UI.FiltersManagerView();
			this.hlFiltersManagementView = new LogJoint.UI.FiltersManagerView();
			this.searchPanelView = new LogJoint.UI.SearchPanelView();
			this.bookmarksManagerView = new LogJoint.UI.BookmarksManagerView();
			this.timeLinePanel = new LogJoint.UI.TimelinePanel();
			this.splitContainer_Log_SearchResults = new System.Windows.Forms.ExtendedSplitContainer();
			this.loadedMessagesControl = new LogJoint.UI.LoadedMessagesControl();
			this.searchResultView = new LogJoint.UI.SearchResultView();
			this.statusStrip1.SuspendLayout();
			this.panel2.SuspendLayout();
			((System.ComponentModel.ISupportInitialize)(this.splitContainer_Menu_Workspace)).BeginInit();
			this.splitContainer_Menu_Workspace.Panel1.SuspendLayout();
			this.splitContainer_Menu_Workspace.Panel2.SuspendLayout();
			this.splitContainer_Menu_Workspace.SuspendLayout();
			this.menuTabControl.SuspendLayout();
			this.sourcesTabPage.SuspendLayout();
			this.threadsTabPage.SuspendLayout();
			this.filtersTabPage.SuspendLayout();
			this.highlightTabPage.SuspendLayout();
			this.searchTabPage.SuspendLayout();
			this.navigationTabPage.SuspendLayout();
			((System.ComponentModel.ISupportInitialize)(this.splitContainer_Timeline_Log)).BeginInit();
			this.splitContainer_Timeline_Log.Panel1.SuspendLayout();
			this.splitContainer_Timeline_Log.Panel2.SuspendLayout();
			this.splitContainer_Timeline_Log.SuspendLayout();
			this.optionsContextMenu.SuspendLayout();
			((System.ComponentModel.ISupportInitialize)(this.restartAppToUpdatePicture)).BeginInit();
			((System.ComponentModel.ISupportInitialize)(this.splitContainer_Log_SearchResults)).BeginInit();
			this.splitContainer_Log_SearchResults.Panel1.SuspendLayout();
			this.splitContainer_Log_SearchResults.Panel2.SuspendLayout();
			this.splitContainer_Log_SearchResults.SuspendLayout();
			this.SuspendLayout();
			// 
			// imageList1
			// 
			this.imageList1.ImageStream = ((System.Windows.Forms.ImageListStreamer)(resources.GetObject("imageList1.ImageStream")));
			this.imageList1.TransparentColor = System.Drawing.Color.Fuchsia;
			this.imageList1.Images.SetKeyName(0, "images_01.png");
			this.imageList1.Images.SetKeyName(1, "images_02.png");
			this.imageList1.Images.SetKeyName(2, "images_03.png");
			this.imageList1.Images.SetKeyName(3, "images_04.png");
			this.imageList1.Images.SetKeyName(4, "images_05.png");
			// 
			// openFileDialog1
			// 
			this.openFileDialog1.FileName = "openFileDialog1";
			// 
			// mruContextMenuStrip
			// 
			this.mruContextMenuStrip.ImageScalingSize = new System.Drawing.Size(20, 20);
			this.mruContextMenuStrip.Name = "mruContextMenuStrip";
			this.mruContextMenuStrip.Size = new System.Drawing.Size(61, 4);
			// 
			// statusStrip1
			// 
			this.statusStrip1.AutoSize = false;
			this.statusStrip1.ImageScalingSize = new System.Drawing.Size(20, 20);
			this.statusStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.toolStripAnalizingImage,
            this.toolStripAnalizingLabel,
            this.toolStripStatusImage,
            this.toolStripStatusLabel,
            this.cancelLongRunningProcessLabel,
            this.cancelLongRunningProcessDropDownButton});
			this.statusStrip1.Location = new System.Drawing.Point(0, 595);
			this.statusStrip1.Name = "statusStrip1";
			this.statusStrip1.Size = new System.Drawing.Size(898, 25);
			this.statusStrip1.TabIndex = 1;
			this.statusStrip1.Text = "statusStrip1";
			// 
			// toolStripAnalizingLabel
			// 
			this.toolStripAnalizingLabel.Name = "toolStripAnalizingLabel";
			this.toolStripAnalizingLabel.Padding = new System.Windows.Forms.Padding(0, 0, 10, 0);
			this.toolStripAnalizingLabel.Size = new System.Drawing.Size(102, 20);
			this.toolStripAnalizingLabel.Text = "Analizing logs";
			this.toolStripAnalizingLabel.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
			this.toolStripAnalizingLabel.Visible = false;
			// 
			// toolStripStatusLabel
			// 
			this.toolStripStatusLabel.Name = "toolStripStatusLabel";
			this.toolStripStatusLabel.Size = new System.Drawing.Size(0, 20);
			// 
			// cancelLongRunningProcessDropDownButton
			// 
			this.cancelLongRunningProcessDropDownButton.Alignment = System.Windows.Forms.ToolStripItemAlignment.Right;
			this.cancelLongRunningProcessDropDownButton.Font = new System.Drawing.Font("Tahoma", 8.25F, System.Drawing.FontStyle.Bold);
			this.cancelLongRunningProcessDropDownButton.ImageScaling = System.Windows.Forms.ToolStripItemImageScaling.None;
			this.cancelLongRunningProcessDropDownButton.ImageTransparentColor = System.Drawing.Color.Magenta;
			this.cancelLongRunningProcessDropDownButton.Name = "cancelLongRunningProcessDropDownButton";
			this.cancelLongRunningProcessDropDownButton.Padding = new System.Windows.Forms.Padding(10, 0, 10, 0);
			this.cancelLongRunningProcessDropDownButton.ShowDropDownArrow = false;
			this.cancelLongRunningProcessDropDownButton.Size = new System.Drawing.Size(121, 23);
			this.cancelLongRunningProcessDropDownButton.Text = "Cancel (ESC)";
			this.cancelLongRunningProcessDropDownButton.TextImageRelation = System.Windows.Forms.TextImageRelation.TextBeforeImage;
			this.cancelLongRunningProcessDropDownButton.Visible = false;
			this.cancelLongRunningProcessDropDownButton.Click += new System.EventHandler(this.cancelLongRunningProcessDropDownButton_Click);
			// 
			// aboutLinkLabel
			// 
			this.aboutLinkLabel.Dock = System.Windows.Forms.DockStyle.Fill;
			this.aboutLinkLabel.ImageAlign = System.Drawing.ContentAlignment.MiddleRight;
			this.aboutLinkLabel.Location = new System.Drawing.Point(16, 0);
			this.aboutLinkLabel.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
			this.aboutLinkLabel.Name = "aboutLinkLabel";
			this.aboutLinkLabel.Size = new System.Drawing.Size(84, 19);
			this.aboutLinkLabel.TabIndex = 2;
			this.aboutLinkLabel.TabStop = true;
			this.aboutLinkLabel.Text = "Options";
			this.aboutLinkLabel.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
			this.aboutLinkLabel.LinkClicked += new System.Windows.Forms.LinkLabelLinkClickedEventHandler(this.optionsLinkLabel_LinkClicked);
			// 
			// button2
			// 
			this.button2.Enabled = false;
			this.button2.Location = new System.Drawing.Point(242, 3);
			this.button2.Margin = new System.Windows.Forms.Padding(2);
			this.button2.Name = "button2";
			this.button2.Size = new System.Drawing.Size(75, 23);
			this.button2.TabIndex = 29;
			this.button2.Text = "Move Down";
			this.button2.UseVisualStyleBackColor = true;
			// 
			// button3
			// 
			this.button3.Enabled = false;
			this.button3.Location = new System.Drawing.Point(163, 3);
			this.button3.Margin = new System.Windows.Forms.Padding(2);
			this.button3.Name = "button3";
			this.button3.Size = new System.Drawing.Size(75, 23);
			this.button3.TabIndex = 28;
			this.button3.Text = "Move Up";
			this.button3.UseVisualStyleBackColor = true;
			// 
			// button4
			// 
			this.button4.Enabled = false;
			this.button4.Location = new System.Drawing.Point(84, 3);
			this.button4.Margin = new System.Windows.Forms.Padding(2);
			this.button4.Name = "button4";
			this.button4.Size = new System.Drawing.Size(75, 23);
			this.button4.TabIndex = 27;
			this.button4.Text = "Remove";
			this.button4.UseVisualStyleBackColor = true;
			// 
			// button5
			// 
			this.button5.Location = new System.Drawing.Point(5, 3);
			this.button5.Margin = new System.Windows.Forms.Padding(2);
			this.button5.Name = "button5";
			this.button5.Size = new System.Drawing.Size(75, 23);
			this.button5.TabIndex = 25;
			this.button5.Text = "Add...";
			this.button5.UseVisualStyleBackColor = true;
			// 
			// button6
			// 
			this.button6.Enabled = false;
			this.button6.Location = new System.Drawing.Point(536, 2);
			this.button6.Margin = new System.Windows.Forms.Padding(2);
			this.button6.Name = "button6";
			this.button6.Size = new System.Drawing.Size(75, 23);
			this.button6.TabIndex = 34;
			this.button6.Text = "Next >>";
			this.button6.UseVisualStyleBackColor = true;
			// 
			// panel2
			// 
			this.panel2.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
			this.panel2.Controls.Add(this.aboutLinkLabel);
			this.panel2.Controls.Add(this.restartAppToUpdatePicture);
			this.panel2.Location = new System.Drawing.Point(793, 2);
			this.panel2.Margin = new System.Windows.Forms.Padding(4);
			this.panel2.Name = "panel2";
			this.panel2.Size = new System.Drawing.Size(100, 19);
			this.panel2.TabIndex = 13;
			// 
			// splitContainer_Menu_Workspace
			// 
			this.splitContainer_Menu_Workspace.Dock = System.Windows.Forms.DockStyle.Fill;
			this.splitContainer_Menu_Workspace.FixedPanel = System.Windows.Forms.FixedPanel.Panel1;
			this.splitContainer_Menu_Workspace.Location = new System.Drawing.Point(0, 0);
			this.splitContainer_Menu_Workspace.Margin = new System.Windows.Forms.Padding(4);
			this.splitContainer_Menu_Workspace.Name = "splitContainer_Menu_Workspace";
			this.splitContainer_Menu_Workspace.Orientation = System.Windows.Forms.Orientation.Horizontal;
			// 
			// splitContainer_Menu_Workspace.Panel1
			// 
			this.splitContainer_Menu_Workspace.Panel1.Controls.Add(this.menuTabControl);
			this.splitContainer_Menu_Workspace.Panel1MinSize = 130;
			// 
			// splitContainer_Menu_Workspace.Panel2
			// 
			this.splitContainer_Menu_Workspace.Panel2.Controls.Add(this.splitContainer_Timeline_Log);
			this.splitContainer_Menu_Workspace.Panel2MinSize = 50;
			this.splitContainer_Menu_Workspace.Size = new System.Drawing.Size(898, 595);
			this.splitContainer_Menu_Workspace.SplitterDistance = 163;
			this.splitContainer_Menu_Workspace.SplitterWidth = 5;
			this.splitContainer_Menu_Workspace.TabIndex = 1;
			// 
			// menuTabControl
			// 
			this.menuTabControl.Controls.Add(this.sourcesTabPage);
			this.menuTabControl.Controls.Add(this.threadsTabPage);
			this.menuTabControl.Controls.Add(this.filtersTabPage);
			this.menuTabControl.Controls.Add(this.highlightTabPage);
			this.menuTabControl.Controls.Add(this.searchTabPage);
			this.menuTabControl.Controls.Add(this.navigationTabPage);
			this.menuTabControl.Dock = System.Windows.Forms.DockStyle.Fill;
			this.menuTabControl.Location = new System.Drawing.Point(0, 0);
			this.menuTabControl.Margin = new System.Windows.Forms.Padding(2);
			this.menuTabControl.Name = "menuTabControl";
			this.menuTabControl.SelectedIndex = 0;
			this.menuTabControl.Size = new System.Drawing.Size(898, 163);
			this.menuTabControl.TabIndex = 1;
			// 
			// sourcesTabPage
			// 
			this.sourcesTabPage.Controls.Add(this.sourcesListView);
			this.sourcesTabPage.Location = new System.Drawing.Point(4, 26);
			this.sourcesTabPage.Margin = new System.Windows.Forms.Padding(0);
			this.sourcesTabPage.Name = "sourcesTabPage";
			this.sourcesTabPage.Size = new System.Drawing.Size(890, 133);
			this.sourcesTabPage.TabIndex = 0;
			this.sourcesTabPage.Text = "Log Sources";
			this.sourcesTabPage.UseVisualStyleBackColor = true;
			// 
			// threadsTabPage
			// 
			this.threadsTabPage.Controls.Add(this.threadsListView);
			this.threadsTabPage.Location = new System.Drawing.Point(4, 26);
			this.threadsTabPage.Margin = new System.Windows.Forms.Padding(0);
			this.threadsTabPage.Name = "threadsTabPage";
			this.threadsTabPage.Size = new System.Drawing.Size(890, 133);
			this.threadsTabPage.TabIndex = 1;
			this.threadsTabPage.Text = "Threads";
			this.threadsTabPage.UseVisualStyleBackColor = true;
			// 
			// filtersTabPage
			// 
			this.filtersTabPage.Controls.Add(this.displayFiltersManagementView);
			this.filtersTabPage.Location = new System.Drawing.Point(4, 26);
			this.filtersTabPage.Margin = new System.Windows.Forms.Padding(0);
			this.filtersTabPage.Name = "filtersTabPage";
			this.filtersTabPage.Size = new System.Drawing.Size(890, 133);
			this.filtersTabPage.TabIndex = 4;
			this.filtersTabPage.Text = "Filtering Rules";
			this.filtersTabPage.UseVisualStyleBackColor = true;
			// 
			// highlightTabPage
			// 
			this.highlightTabPage.Controls.Add(this.hlFiltersManagementView);
			this.highlightTabPage.Location = new System.Drawing.Point(4, 26);
			this.highlightTabPage.Margin = new System.Windows.Forms.Padding(0);
			this.highlightTabPage.Name = "highlightTabPage";
			this.highlightTabPage.Size = new System.Drawing.Size(890, 133);
			this.highlightTabPage.TabIndex = 5;
			this.highlightTabPage.Text = "Highlighting Rules";
			this.highlightTabPage.UseVisualStyleBackColor = true;
			// 
			// searchTabPage
			// 
			this.searchTabPage.Controls.Add(this.searchPanelView);
			this.searchTabPage.Location = new System.Drawing.Point(4, 26);
			this.searchTabPage.Margin = new System.Windows.Forms.Padding(0);
			this.searchTabPage.Name = "searchTabPage";
			this.searchTabPage.Size = new System.Drawing.Size(890, 133);
			this.searchTabPage.TabIndex = 2;
			this.searchTabPage.Text = "Search";
			this.searchTabPage.UseVisualStyleBackColor = true;
			// 
			// navigationTabPage
			// 
			this.navigationTabPage.Controls.Add(this.bookmarksManagerView);
			this.navigationTabPage.Location = new System.Drawing.Point(4, 26);
			this.navigationTabPage.Margin = new System.Windows.Forms.Padding(0);
			this.navigationTabPage.Name = "navigationTabPage";
			this.navigationTabPage.Size = new System.Drawing.Size(890, 133);
			this.navigationTabPage.TabIndex = 3;
			this.navigationTabPage.Text = "Bookmarks";
			this.navigationTabPage.UseVisualStyleBackColor = true;
			// 
			// splitContainer_Timeline_Log
			// 
			this.splitContainer_Timeline_Log.BorderStyle = System.Windows.Forms.BorderStyle.Fixed3D;
			this.splitContainer_Timeline_Log.Dock = System.Windows.Forms.DockStyle.Fill;
			this.splitContainer_Timeline_Log.FixedPanel = System.Windows.Forms.FixedPanel.Panel1;
			this.splitContainer_Timeline_Log.Location = new System.Drawing.Point(0, 0);
			this.splitContainer_Timeline_Log.Margin = new System.Windows.Forms.Padding(2);
			this.splitContainer_Timeline_Log.Name = "splitContainer_Timeline_Log";
			// 
			// splitContainer_Timeline_Log.Panel1
			// 
			this.splitContainer_Timeline_Log.Panel1.Controls.Add(this.timeLinePanel);
			// 
			// splitContainer_Timeline_Log.Panel2
			// 
			this.splitContainer_Timeline_Log.Panel2.Controls.Add(this.splitContainer_Log_SearchResults);
			this.splitContainer_Timeline_Log.Size = new System.Drawing.Size(898, 427);
			this.splitContainer_Timeline_Log.SplitterDistance = 133;
			this.splitContainer_Timeline_Log.TabIndex = 2;
			// 
			// optionsContextMenu
			// 
			this.optionsContextMenu.ImageScalingSize = new System.Drawing.Size(20, 20);
			this.optionsContextMenu.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.configurationToolStripMenuItem,
            this.aboutToolStripMenuItem});
			this.optionsContextMenu.Name = "optionsContextMenu";
			this.optionsContextMenu.Size = new System.Drawing.Size(176, 48);
			// 
			// configurationToolStripMenuItem
			// 
			this.configurationToolStripMenuItem.Name = "configurationToolStripMenuItem";
			this.configurationToolStripMenuItem.Size = new System.Drawing.Size(175, 22);
			this.configurationToolStripMenuItem.Text = "Configuration...";
			this.configurationToolStripMenuItem.Click += new System.EventHandler(this.configurationToolStripMenuItem_Click);
			// 
			// aboutToolStripMenuItem
			// 
			this.aboutToolStripMenuItem.Name = "aboutToolStripMenuItem";
			this.aboutToolStripMenuItem.Size = new System.Drawing.Size(175, 22);
			this.aboutToolStripMenuItem.Text = "About...";
			this.aboutToolStripMenuItem.Click += new System.EventHandler(this.aboutToolStripMenuItem_Click);
			// 
			// restartAppToUpdatePicture
			// 
			this.restartAppToUpdatePicture.Dock = System.Windows.Forms.DockStyle.Left;
			this.restartAppToUpdatePicture.Image = global::LogJoint.Properties.Resources.RestartApp;
			this.restartAppToUpdatePicture.Location = new System.Drawing.Point(0, 0);
			this.restartAppToUpdatePicture.Margin = new System.Windows.Forms.Padding(0);
			this.restartAppToUpdatePicture.Name = "restartAppToUpdatePicture";
			this.restartAppToUpdatePicture.Size = new System.Drawing.Size(16, 19);
			this.restartAppToUpdatePicture.SizeMode = System.Windows.Forms.PictureBoxSizeMode.Zoom;
			this.restartAppToUpdatePicture.TabIndex = 3;
			this.restartAppToUpdatePicture.TabStop = false;
			this.toolTip1.SetToolTip(this.restartAppToUpdatePicture, "New update available. Restart application to apply it.");
			this.restartAppToUpdatePicture.Visible = false;
			this.restartAppToUpdatePicture.Click += restartAppToUpdatePicture_Click;
			// 
			// toolStripAnalizingImage
			// 
			this.toolStripAnalizingImage.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
			this.toolStripAnalizingImage.Image = global::LogJoint.Properties.Resources.loader;
			this.toolStripAnalizingImage.Name = "toolStripAnalizingImage";
			this.toolStripAnalizingImage.Size = new System.Drawing.Size(20, 20);
			this.toolStripAnalizingImage.Text = "toolStripStatusLabel1";
			this.toolStripAnalizingImage.Visible = false;
			// 
			// toolStripStatusImage
			// 
			this.toolStripStatusImage.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
			this.toolStripStatusImage.Image = global::LogJoint.Properties.Resources.InfoBlinking;
			this.toolStripStatusImage.Name = "toolStripStatusImage";
			this.toolStripStatusImage.Size = new System.Drawing.Size(20, 20);
			this.toolStripStatusImage.Text = "toolStripStatusLabel1";
			this.toolStripStatusImage.Visible = false;
			// 
			// cancelLongRunningProcessLabel
			// 
			this.cancelLongRunningProcessLabel.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
			this.cancelLongRunningProcessLabel.ImageScaling = System.Windows.Forms.ToolStripItemImageScaling.None;
			this.cancelLongRunningProcessLabel.Name = "cancelLongRunningProcessLabel";
			this.cancelLongRunningProcessLabel.Padding = new System.Windows.Forms.Padding(2, 0, 2, 0);
			this.cancelLongRunningProcessLabel.Size = new System.Drawing.Size(49, 20);
			this.cancelLongRunningProcessLabel.Visible = false;
			// 
			// sourcesListView
			// 
			this.sourcesListView.Dock = System.Windows.Forms.DockStyle.Fill;
			this.sourcesListView.Font = new System.Drawing.Font("Tahoma", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
			this.sourcesListView.Location = new System.Drawing.Point(0, 0);
			this.sourcesListView.Margin = new System.Windows.Forms.Padding(0);
			this.sourcesListView.Name = "sourcesListView";
			this.sourcesListView.Size = new System.Drawing.Size(890, 133);
			this.sourcesListView.TabIndex = 4;
			// 
			// threadsListView
			// 
			this.threadsListView.Dock = System.Windows.Forms.DockStyle.Fill;
			this.threadsListView.Location = new System.Drawing.Point(0, 0);
			this.threadsListView.Margin = new System.Windows.Forms.Padding(0);
			this.threadsListView.Name = "threadsListView";
			this.threadsListView.Size = new System.Drawing.Size(890, 134);
			this.threadsListView.TabIndex = 0;
			this.threadsListView.TopItem = null;
			// 
			// displayFiltersManagementView
			// 
			this.displayFiltersManagementView.Dock = System.Windows.Forms.DockStyle.Fill;
			this.displayFiltersManagementView.Font = new System.Drawing.Font("Tahoma", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(204)));
			this.displayFiltersManagementView.Location = new System.Drawing.Point(0, 0);
			this.displayFiltersManagementView.Margin = new System.Windows.Forms.Padding(0);
			this.displayFiltersManagementView.Name = "displayFiltersManagementView";
			this.displayFiltersManagementView.Size = new System.Drawing.Size(890, 134);
			this.displayFiltersManagementView.TabIndex = 2;
			// 
			// hlFiltersManagementView
			// 
			this.hlFiltersManagementView.Dock = System.Windows.Forms.DockStyle.Fill;
			this.hlFiltersManagementView.Font = new System.Drawing.Font("Tahoma", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(204)));
			this.hlFiltersManagementView.Location = new System.Drawing.Point(0, 0);
			this.hlFiltersManagementView.Margin = new System.Windows.Forms.Padding(0);
			this.hlFiltersManagementView.Name = "hlFiltersManagementView";
			this.hlFiltersManagementView.Size = new System.Drawing.Size(890, 134);
			this.hlFiltersManagementView.TabIndex = 2;
			// 
			// searchPanelView
			// 
			this.searchPanelView.Dock = System.Windows.Forms.DockStyle.Fill;
			this.searchPanelView.Font = new System.Drawing.Font("Tahoma", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
			this.searchPanelView.Location = new System.Drawing.Point(0, 0);
			this.searchPanelView.Margin = new System.Windows.Forms.Padding(0);
			this.searchPanelView.Name = "searchPanelView";
			this.searchPanelView.Size = new System.Drawing.Size(890, 134);
			this.searchPanelView.TabIndex = 42;
			// 
			// bookmarksManagerView
			// 
			this.bookmarksManagerView.Dock = System.Windows.Forms.DockStyle.Fill;
			this.bookmarksManagerView.Font = new System.Drawing.Font("Tahoma", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
			this.bookmarksManagerView.Location = new System.Drawing.Point(0, 0);
			this.bookmarksManagerView.Margin = new System.Windows.Forms.Padding(0);
			this.bookmarksManagerView.Name = "bookmarksManagerView";
			this.bookmarksManagerView.Size = new System.Drawing.Size(890, 134);
			this.bookmarksManagerView.TabIndex = 10;
			// 
			// timeLinePanel
			// 
			this.timeLinePanel.BackColor = System.Drawing.Color.White;
			this.timeLinePanel.Dock = System.Windows.Forms.DockStyle.Fill;
			this.timeLinePanel.Location = new System.Drawing.Point(0, 0);
			this.timeLinePanel.Margin = new System.Windows.Forms.Padding(2);
			this.timeLinePanel.MinimumSize = new System.Drawing.Size(12, 50);
			this.timeLinePanel.Name = "timeLinePanel";
			this.timeLinePanel.Size = new System.Drawing.Size(129, 423);
			this.timeLinePanel.TabIndex = 15;
			// 
			// splitContainer_Log_SearchResults
			// 
			this.splitContainer_Log_SearchResults.Dock = System.Windows.Forms.DockStyle.Fill;
			this.splitContainer_Log_SearchResults.Location = new System.Drawing.Point(0, 0);
			this.splitContainer_Log_SearchResults.Margin = new System.Windows.Forms.Padding(4);
			this.splitContainer_Log_SearchResults.Name = "splitContainer_Log_SearchResults";
			this.splitContainer_Log_SearchResults.Orientation = System.Windows.Forms.Orientation.Horizontal;
			// 
			// splitContainer_Log_SearchResults.Panel1
			// 
			this.splitContainer_Log_SearchResults.Panel1.Controls.Add(this.loadedMessagesControl);
			// 
			// splitContainer_Log_SearchResults.Panel2
			// 
			this.splitContainer_Log_SearchResults.Panel2.Controls.Add(this.searchResultView);
			this.splitContainer_Log_SearchResults.Panel2Collapsed = true;
			this.splitContainer_Log_SearchResults.Size = new System.Drawing.Size(757, 423);
			this.splitContainer_Log_SearchResults.SplitterDistance = 200;
			this.splitContainer_Log_SearchResults.SplitterWidth = 6;
			this.splitContainer_Log_SearchResults.TabIndex = 12;
			// 
			// loadedMessagesControl
			// 
			this.loadedMessagesControl.Cursor = System.Windows.Forms.Cursors.Arrow;
			this.loadedMessagesControl.Dock = System.Windows.Forms.DockStyle.Fill;
			this.loadedMessagesControl.Location = new System.Drawing.Point(0, 0);
			this.loadedMessagesControl.Margin = new System.Windows.Forms.Padding(2);
			this.loadedMessagesControl.Name = "loadedMessagesControl";
			this.loadedMessagesControl.Size = new System.Drawing.Size(757, 423);
			this.loadedMessagesControl.TabIndex = 11;
			// 
			// searchResultView
			// 
			this.searchResultView.Dock = System.Windows.Forms.DockStyle.Fill;
			this.searchResultView.Font = new System.Drawing.Font("Tahoma", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(204)));
			this.searchResultView.Location = new System.Drawing.Point(0, 0);
			this.searchResultView.Margin = new System.Windows.Forms.Padding(5);
			this.searchResultView.Name = "searchResultView";
			this.searchResultView.Size = new System.Drawing.Size(150, 46);
			this.searchResultView.TabIndex = 0;
			// 
			// MainForm
			// 
			this.AllowDrop = true;
			this.AutoScaleDimensions = new System.Drawing.SizeF(120F, 120F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Dpi;
			this.ClientSize = new System.Drawing.Size(898, 620);
			this.Controls.Add(this.panel2);
			this.Controls.Add(this.splitContainer_Menu_Workspace);
			this.Controls.Add(this.statusStrip1);
			this.Font = new System.Drawing.Font("Tahoma", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(204)));
			this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
			this.KeyPreview = true;
			this.Margin = new System.Windows.Forms.Padding(2);
			this.Name = "MainForm";
			this.Text = "LogJoint Log Viewer";
			this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.MainForm_FormClosing);
			this.Load += new System.EventHandler(this.MainForm_Load);
			this.DragDrop += new System.Windows.Forms.DragEventHandler(this.MainForm_DragDrop);
			this.DragOver += new System.Windows.Forms.DragEventHandler(this.MainForm_DragOver);
			this.KeyDown += new System.Windows.Forms.KeyEventHandler(this.MainForm_KeyDown);
			this.statusStrip1.ResumeLayout(false);
			this.statusStrip1.PerformLayout();
			this.panel2.ResumeLayout(false);
			this.splitContainer_Menu_Workspace.Panel1.ResumeLayout(false);
			this.splitContainer_Menu_Workspace.Panel2.ResumeLayout(false);
			((System.ComponentModel.ISupportInitialize)(this.splitContainer_Menu_Workspace)).EndInit();
			this.splitContainer_Menu_Workspace.ResumeLayout(false);
			this.menuTabControl.ResumeLayout(false);
			this.sourcesTabPage.ResumeLayout(false);
			this.threadsTabPage.ResumeLayout(false);
			this.filtersTabPage.ResumeLayout(false);
			this.highlightTabPage.ResumeLayout(false);
			this.searchTabPage.ResumeLayout(false);
			this.navigationTabPage.ResumeLayout(false);
			this.splitContainer_Timeline_Log.Panel1.ResumeLayout(false);
			this.splitContainer_Timeline_Log.Panel2.ResumeLayout(false);
			((System.ComponentModel.ISupportInitialize)(this.splitContainer_Timeline_Log)).EndInit();
			this.splitContainer_Timeline_Log.ResumeLayout(false);
			this.optionsContextMenu.ResumeLayout(false);
			((System.ComponentModel.ISupportInitialize)(this.restartAppToUpdatePicture)).EndInit();
			this.splitContainer_Log_SearchResults.Panel1.ResumeLayout(false);
			this.splitContainer_Log_SearchResults.Panel2.ResumeLayout(false);
			((System.ComponentModel.ISupportInitialize)(this.splitContainer_Log_SearchResults)).EndInit();
			this.splitContainer_Log_SearchResults.ResumeLayout(false);
			this.ResumeLayout(false);

		}

		#endregion

		private System.Windows.Forms.ImageList imageList1;
		private System.Windows.Forms.OpenFileDialog openFileDialog1;
		private System.Windows.Forms.ContextMenuStrip mruContextMenuStrip;
		private System.Windows.Forms.TabPage sourcesTabPage;
		private System.Windows.Forms.TabPage threadsTabPage;
		private System.Windows.Forms.TabPage searchTabPage;
		internal UI.TimelinePanel timeLinePanel;
		private System.Windows.Forms.SplitContainer splitContainer_Timeline_Log;
		private System.Windows.Forms.StatusStrip statusStrip1;
		internal System.Windows.Forms.ToolStripStatusLabel toolStripStatusLabel;
		private System.Windows.Forms.TabPage navigationTabPage;
		internal LogJoint.UI.ThreadsListView threadsListView;
		internal UI.SourcesManagementView sourcesListView;
		private System.Windows.Forms.ToolStripDropDownButton cancelLongRunningProcessDropDownButton;
		private System.Windows.Forms.ToolStripStatusLabel cancelLongRunningProcessLabel;
		private System.Windows.Forms.SplitContainer splitContainer_Menu_Workspace;
		private System.Windows.Forms.LinkLabel aboutLinkLabel;
		private System.Windows.Forms.ToolStripStatusLabel toolStripStatusImage;
        private System.Windows.Forms.ToolStripStatusLabel toolStripAnalizingImage;
        private System.Windows.Forms.ToolStripStatusLabel toolStripAnalizingLabel;
		private System.Windows.Forms.TabPage highlightTabPage;
		private System.Windows.Forms.Button button2;
		private System.Windows.Forms.Button button3;
		private System.Windows.Forms.Button button4;
		private System.Windows.Forms.Button button5;
		internal System.Windows.Forms.TabPage filtersTabPage;
		internal System.Windows.Forms.TabControl menuTabControl;
		private System.Windows.Forms.ToolTip toolTip1;
		private System.Windows.Forms.Button button6;
		private System.Windows.Forms.Panel panel2;
		internal System.Windows.Forms.ExtendedSplitContainer splitContainer_Log_SearchResults;
		internal SearchResultView searchResultView;
		internal LoadedMessagesControl loadedMessagesControl;
		internal FiltersManagerView displayFiltersManagementView;
		internal FiltersManagerView hlFiltersManagementView;
		internal SearchPanelView searchPanelView;
		internal LogJoint.UI.BookmarksManagerView bookmarksManagerView;
		private System.Windows.Forms.ContextMenuStrip optionsContextMenu;
		private System.Windows.Forms.ToolStripMenuItem configurationToolStripMenuItem;
		private System.Windows.Forms.ToolStripMenuItem aboutToolStripMenuItem;
		private System.Windows.Forms.PictureBox restartAppToUpdatePicture;
	}
}

