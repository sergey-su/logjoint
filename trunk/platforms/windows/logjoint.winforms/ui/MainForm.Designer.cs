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
			this.updateViewTimer = new System.Windows.Forms.Timer(this.components);
			this.mruContextMenuStrip = new System.Windows.Forms.ContextMenuStrip(this.components);
			this.statusStrip1 = new System.Windows.Forms.StatusStrip();
			this.toolStripAnalizingImage = new System.Windows.Forms.ToolStripStatusLabel();
			this.toolStripAnalizingLabel = new System.Windows.Forms.ToolStripStatusLabel();
			this.toolStripStatusImage = new System.Windows.Forms.ToolStripStatusLabel();
			this.toolStripStatusLabel = new System.Windows.Forms.ToolStripStatusLabel();
			this.cancelShiftingLabel = new System.Windows.Forms.ToolStripStatusLabel();
			this.cancelShiftingDropDownButton = new System.Windows.Forms.ToolStripDropDownButton();
			this.aboutLinkLabel = new System.Windows.Forms.LinkLabel();
			this.button2 = new System.Windows.Forms.Button();
			this.button3 = new System.Windows.Forms.Button();
			this.button4 = new System.Windows.Forms.Button();
			this.button5 = new System.Windows.Forms.Button();
			this.toolTip1 = new System.Windows.Forms.ToolTip(this.components);
			this.prevBookmarkButton = new System.Windows.Forms.Button();
			this.deleteAllBookmarksButton = new System.Windows.Forms.Button();
			this.nextBookmarkButton = new System.Windows.Forms.Button();
			this.toggleBookmarkButton = new System.Windows.Forms.Button();
			this.splitContainer2 = new System.Windows.Forms.SplitContainer();
			this.menuTabControl = new System.Windows.Forms.TabControl();
			this.sourcesTabPage = new System.Windows.Forms.TabPage();
			this.trackChangesCheckBox = new System.Windows.Forms.CheckBox();
			this.sourcesListView = new LogJoint.UI.SourcesListView();
			this.deleteButton = new System.Windows.Forms.Button();
			this.recentButton = new System.Windows.Forms.Button();
			this.addNewLogButton = new System.Windows.Forms.Button();
			this.threadsTabPage = new System.Windows.Forms.TabPage();
			this.threadsListView = new LogJoint.UI.ThreadsListView();
			this.filtersTabPage = new System.Windows.Forms.TabPage();
			this.flowLayoutPanel1 = new System.Windows.Forms.FlowLayoutPanel();
			this.enableFilteringCheckBox = new System.Windows.Forms.CheckBox();
			this.addDiplayFilterButton = new System.Windows.Forms.Button();
			this.deleteDisplayFilterButton = new System.Windows.Forms.Button();
			this.moveDisplayFilterUpButton = new System.Windows.Forms.Button();
			this.moveDisplayFilterDownButton = new System.Windows.Forms.Button();
			this.displayFiltersListView = new LogJoint.UI.FiltersListView();
			this.highlightTabPage = new System.Windows.Forms.TabPage();
			this.flowLayoutPanel2 = new System.Windows.Forms.FlowLayoutPanel();
			this.enableHighlightingCheckBox = new System.Windows.Forms.CheckBox();
			this.addHLFilterButton = new System.Windows.Forms.Button();
			this.deleteHLFilterButton = new System.Windows.Forms.Button();
			this.moveHLFilterUpButton = new System.Windows.Forms.Button();
			this.moveHLFilterDownButton = new System.Windows.Forms.Button();
			this.prevHightlightedButton = new System.Windows.Forms.Button();
			this.nextHightlightedButton = new System.Windows.Forms.Button();
			this.hlFiltersListView = new LogJoint.UI.FiltersListView();
			this.searchTabPage = new System.Windows.Forms.TabPage();
			this.respectFilteringRulesCheckBox = new System.Windows.Forms.CheckBox();
			this.searchAmongLoadedMessagesRadioButton = new System.Windows.Forms.RadioButton();
			this.searchAllOccurancesRadioButton = new System.Windows.Forms.RadioButton();
			this.searchWithinCurrentThreadCheckbox = new System.Windows.Forms.CheckBox();
			this.panel1 = new System.Windows.Forms.Panel();
			this.messageTypesCheckedListBox = new System.Windows.Forms.CheckedListBox();
			this.doSearchButton = new System.Windows.Forms.Button();
			this.regExpCheckBox = new System.Windows.Forms.CheckBox();
			this.searchUpCheckbox = new System.Windows.Forms.CheckBox();
			this.wholeWordCheckbox = new System.Windows.Forms.CheckBox();
			this.matchCaseCheckbox = new System.Windows.Forms.CheckBox();
			this.searchTextBox = new LogJoint.UI.SearchTextBox();
			this.navigationTabPage = new System.Windows.Forms.TabPage();
			this.bookmarksView = new LogJoint.UI.BookmarksView();
			this.splitContainer1 = new System.Windows.Forms.SplitContainer();
			this.timeLineControl = new LogJoint.UI.TimeLineControl();
			this.timelineControlPanel = new LogJoint.UI.TimelineControlPanel();
			this.splitContainer3 = new System.Windows.Forms.SplitContainer();
			this.logViewerControl = new LogJoint.UI.LogViewerControl();
			this.searchResultViewer = new LogJoint.UI.LogViewerControl();
			this.panel3 = new System.Windows.Forms.Panel();
			this.searchProgressBar = new System.Windows.Forms.ProgressBar();
			this.searchResultLabel = new System.Windows.Forms.Label();
			this.label1 = new System.Windows.Forms.Label();
			this.closeSearchResultButton = new System.Windows.Forms.Button();
			this.button6 = new System.Windows.Forms.Button();
			this.panel2 = new System.Windows.Forms.Panel();
			this.saveFileDialog1 = new System.Windows.Forms.SaveFileDialog();
			this.statusStrip1.SuspendLayout();
			((System.ComponentModel.ISupportInitialize)(this.splitContainer2)).BeginInit();
			this.splitContainer2.Panel1.SuspendLayout();
			this.splitContainer2.Panel2.SuspendLayout();
			this.splitContainer2.SuspendLayout();
			this.menuTabControl.SuspendLayout();
			this.sourcesTabPage.SuspendLayout();
			this.threadsTabPage.SuspendLayout();
			this.filtersTabPage.SuspendLayout();
			this.flowLayoutPanel1.SuspendLayout();
			this.highlightTabPage.SuspendLayout();
			this.flowLayoutPanel2.SuspendLayout();
			this.searchTabPage.SuspendLayout();
			this.panel1.SuspendLayout();
			this.navigationTabPage.SuspendLayout();
			((System.ComponentModel.ISupportInitialize)(this.splitContainer1)).BeginInit();
			this.splitContainer1.Panel1.SuspendLayout();
			this.splitContainer1.Panel2.SuspendLayout();
			this.splitContainer1.SuspendLayout();
			((System.ComponentModel.ISupportInitialize)(this.splitContainer3)).BeginInit();
			this.splitContainer3.Panel1.SuspendLayout();
			this.splitContainer3.Panel2.SuspendLayout();
			this.splitContainer3.SuspendLayout();
			this.panel3.SuspendLayout();
			this.panel2.SuspendLayout();
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
			// updateViewTimer
			// 
			this.updateViewTimer.Enabled = true;
			this.updateViewTimer.Interval = 400;
			this.updateViewTimer.Tick += new System.EventHandler(this.updateViewTimer_Tick);
			// 
			// mruContextMenuStrip
			// 
			this.mruContextMenuStrip.Name = "mruContextMenuStrip";
			this.mruContextMenuStrip.Size = new System.Drawing.Size(61, 4);
			this.mruContextMenuStrip.ItemClicked += new System.Windows.Forms.ToolStripItemClickedEventHandler(this.mruContextMenuStrip_ItemClicked);
			// 
			// statusStrip1
			// 
			this.statusStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.toolStripAnalizingImage,
            this.toolStripAnalizingLabel,
            this.toolStripStatusImage,
            this.toolStripStatusLabel,
            this.cancelShiftingLabel,
            this.cancelShiftingDropDownButton});
			this.statusStrip1.Location = new System.Drawing.Point(0, 474);
			this.statusStrip1.Name = "statusStrip1";
			this.statusStrip1.Padding = new System.Windows.Forms.Padding(1, 0, 11, 0);
			this.statusStrip1.Size = new System.Drawing.Size(669, 22);
			this.statusStrip1.TabIndex = 1;
			this.statusStrip1.Text = "statusStrip1";
			// 
			// toolStripAnalizingImage
			// 
			this.toolStripAnalizingImage.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
			this.toolStripAnalizingImage.Image = global::logjoint.Properties.Resources.loader;
			this.toolStripAnalizingImage.Name = "toolStripAnalizingImage";
			this.toolStripAnalizingImage.Size = new System.Drawing.Size(16, 17);
			this.toolStripAnalizingImage.Text = "toolStripStatusLabel1";
			this.toolStripAnalizingImage.Visible = false;
			// 
			// toolStripAnalizingLabel
			// 
			this.toolStripAnalizingLabel.Name = "toolStripAnalizingLabel";
			this.toolStripAnalizingLabel.Padding = new System.Windows.Forms.Padding(0, 0, 10, 0);
			this.toolStripAnalizingLabel.Size = new System.Drawing.Size(81, 17);
			this.toolStripAnalizingLabel.Text = "Analizing logs";
			this.toolStripAnalizingLabel.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
			this.toolStripAnalizingLabel.Visible = false;
			// 
			// toolStripStatusImage
			// 
			this.toolStripStatusImage.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
			this.toolStripStatusImage.Image = global::logjoint.Properties.Resources.InfoBlinking;
			this.toolStripStatusImage.Name = "toolStripStatusImage";
			this.toolStripStatusImage.Size = new System.Drawing.Size(16, 17);
			this.toolStripStatusImage.Text = "toolStripStatusLabel1";
			this.toolStripStatusImage.Visible = false;
			// 
			// toolStripStatusLabel
			// 
			this.toolStripStatusLabel.Name = "toolStripStatusLabel";
			this.toolStripStatusLabel.Size = new System.Drawing.Size(0, 17);
			// 
			// cancelShiftingLabel
			// 
			this.cancelShiftingLabel.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
			this.cancelShiftingLabel.Image = global::logjoint.Properties.Resources.status_anim;
			this.cancelShiftingLabel.ImageScaling = System.Windows.Forms.ToolStripItemImageScaling.None;
			this.cancelShiftingLabel.Name = "cancelShiftingLabel";
			this.cancelShiftingLabel.Padding = new System.Windows.Forms.Padding(2, 0, 2, 0);
			this.cancelShiftingLabel.Size = new System.Drawing.Size(49, 17);
			this.cancelShiftingLabel.Visible = false;
			// 
			// cancelShiftingDropDownButton
			// 
			this.cancelShiftingDropDownButton.Alignment = System.Windows.Forms.ToolStripItemAlignment.Right;
			this.cancelShiftingDropDownButton.Font = new System.Drawing.Font("Tahoma", 8.25F, System.Drawing.FontStyle.Bold);
			this.cancelShiftingDropDownButton.ImageScaling = System.Windows.Forms.ToolStripItemImageScaling.None;
			this.cancelShiftingDropDownButton.ImageTransparentColor = System.Drawing.Color.Magenta;
			this.cancelShiftingDropDownButton.Name = "cancelShiftingDropDownButton";
			this.cancelShiftingDropDownButton.Padding = new System.Windows.Forms.Padding(10, 0, 10, 0);
			this.cancelShiftingDropDownButton.ShowDropDownArrow = false;
			this.cancelShiftingDropDownButton.Size = new System.Drawing.Size(101, 20);
			this.cancelShiftingDropDownButton.Text = "Cancel (ESC)";
			this.cancelShiftingDropDownButton.TextImageRelation = System.Windows.Forms.TextImageRelation.TextBeforeImage;
			this.cancelShiftingDropDownButton.Visible = false;
			this.cancelShiftingDropDownButton.Click += new System.EventHandler(this.cancelShiftingDropDownButton_Click);
			// 
			// aboutLinkLabel
			// 
			this.aboutLinkLabel.Dock = System.Windows.Forms.DockStyle.Fill;
			this.aboutLinkLabel.Location = new System.Drawing.Point(0, 0);
			this.aboutLinkLabel.Name = "aboutLinkLabel";
			this.aboutLinkLabel.Size = new System.Drawing.Size(60, 15);
			this.aboutLinkLabel.TabIndex = 2;
			this.aboutLinkLabel.TabStop = true;
			this.aboutLinkLabel.Text = "About";
			this.aboutLinkLabel.TextAlign = System.Drawing.ContentAlignment.TopRight;
			this.aboutLinkLabel.LinkClicked += new System.Windows.Forms.LinkLabelLinkClickedEventHandler(this.aboutLinkLabel_LinkClicked);
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
			// prevBookmarkButton
			// 
			this.prevBookmarkButton.Image = global::logjoint.Properties.Resources.PrevBookmark;
			this.prevBookmarkButton.Location = new System.Drawing.Point(37, 30);
			this.prevBookmarkButton.Margin = new System.Windows.Forms.Padding(2);
			this.prevBookmarkButton.Name = "prevBookmarkButton";
			this.prevBookmarkButton.Size = new System.Drawing.Size(28, 22);
			this.prevBookmarkButton.TabIndex = 8;
			this.prevBookmarkButton.TextImageRelation = System.Windows.Forms.TextImageRelation.ImageBeforeText;
			this.toolTip1.SetToolTip(this.prevBookmarkButton, "Prev bookmark (Shift+F2)");
			this.prevBookmarkButton.UseVisualStyleBackColor = true;
			this.prevBookmarkButton.Click += new System.EventHandler(this.prevBookmarkButton_Click);
			// 
			// deleteAllBookmarksButton
			// 
			this.deleteAllBookmarksButton.Image = global::logjoint.Properties.Resources.BookmarksDelete;
			this.deleteAllBookmarksButton.Location = new System.Drawing.Point(37, 4);
			this.deleteAllBookmarksButton.Margin = new System.Windows.Forms.Padding(2);
			this.deleteAllBookmarksButton.Name = "deleteAllBookmarksButton";
			this.deleteAllBookmarksButton.Size = new System.Drawing.Size(28, 22);
			this.deleteAllBookmarksButton.TabIndex = 9;
			this.deleteAllBookmarksButton.TextImageRelation = System.Windows.Forms.TextImageRelation.ImageBeforeText;
			this.toolTip1.SetToolTip(this.deleteAllBookmarksButton, "Clear all bookmarks");
			this.deleteAllBookmarksButton.UseVisualStyleBackColor = true;
			this.deleteAllBookmarksButton.Click += new System.EventHandler(this.deleteAllBookmarksButton_Click);
			// 
			// nextBookmarkButton
			// 
			this.nextBookmarkButton.Image = global::logjoint.Properties.Resources.NextBookmark;
			this.nextBookmarkButton.Location = new System.Drawing.Point(6, 30);
			this.nextBookmarkButton.Margin = new System.Windows.Forms.Padding(2);
			this.nextBookmarkButton.Name = "nextBookmarkButton";
			this.nextBookmarkButton.Size = new System.Drawing.Size(28, 22);
			this.nextBookmarkButton.TabIndex = 7;
			this.nextBookmarkButton.TextImageRelation = System.Windows.Forms.TextImageRelation.ImageBeforeText;
			this.toolTip1.SetToolTip(this.nextBookmarkButton, "Next bookmark (F2)");
			this.nextBookmarkButton.UseVisualStyleBackColor = true;
			this.nextBookmarkButton.Click += new System.EventHandler(this.nextBookmarkButton_Click);
			// 
			// toggleBookmarkButton
			// 
			this.toggleBookmarkButton.Image = global::logjoint.Properties.Resources.Bookmark16x16;
			this.toggleBookmarkButton.Location = new System.Drawing.Point(6, 4);
			this.toggleBookmarkButton.Margin = new System.Windows.Forms.Padding(2);
			this.toggleBookmarkButton.Name = "toggleBookmarkButton";
			this.toggleBookmarkButton.Size = new System.Drawing.Size(28, 22);
			this.toggleBookmarkButton.TabIndex = 6;
			this.toggleBookmarkButton.TextImageRelation = System.Windows.Forms.TextImageRelation.ImageBeforeText;
			this.toolTip1.SetToolTip(this.toggleBookmarkButton, "Toggle bookmark (Ctrl+K)");
			this.toggleBookmarkButton.UseVisualStyleBackColor = true;
			this.toggleBookmarkButton.Click += new System.EventHandler(this.toggleBookmarkButton_Click);
			// 
			// splitContainer2
			// 
			this.splitContainer2.Dock = System.Windows.Forms.DockStyle.Fill;
			this.splitContainer2.FixedPanel = System.Windows.Forms.FixedPanel.Panel1;
			this.splitContainer2.Location = new System.Drawing.Point(0, 0);
			this.splitContainer2.Name = "splitContainer2";
			this.splitContainer2.Orientation = System.Windows.Forms.Orientation.Horizontal;
			// 
			// splitContainer2.Panel1
			// 
			this.splitContainer2.Panel1.Controls.Add(this.menuTabControl);
			this.splitContainer2.Panel1MinSize = 130;
			// 
			// splitContainer2.Panel2
			// 
			this.splitContainer2.Panel2.Controls.Add(this.splitContainer1);
			this.splitContainer2.Panel2MinSize = 50;
			this.splitContainer2.Size = new System.Drawing.Size(669, 474);
			this.splitContainer2.SplitterDistance = 130;
			this.splitContainer2.TabIndex = 1;
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
			this.menuTabControl.Size = new System.Drawing.Size(669, 130);
			this.menuTabControl.TabIndex = 1;
			// 
			// sourcesTabPage
			// 
			this.sourcesTabPage.Controls.Add(this.trackChangesCheckBox);
			this.sourcesTabPage.Controls.Add(this.sourcesListView);
			this.sourcesTabPage.Controls.Add(this.deleteButton);
			this.sourcesTabPage.Controls.Add(this.recentButton);
			this.sourcesTabPage.Controls.Add(this.addNewLogButton);
			this.sourcesTabPage.Location = new System.Drawing.Point(4, 22);
			this.sourcesTabPage.Margin = new System.Windows.Forms.Padding(2);
			this.sourcesTabPage.Name = "sourcesTabPage";
			this.sourcesTabPage.Padding = new System.Windows.Forms.Padding(2);
			this.sourcesTabPage.Size = new System.Drawing.Size(661, 104);
			this.sourcesTabPage.TabIndex = 0;
			this.sourcesTabPage.Text = "Log Sources";
			this.sourcesTabPage.UseVisualStyleBackColor = true;
			// 
			// trackChangesCheckBox
			// 
			this.trackChangesCheckBox.AutoCheck = false;
			this.trackChangesCheckBox.AutoSize = true;
			this.trackChangesCheckBox.Enabled = false;
			this.trackChangesCheckBox.Location = new System.Drawing.Point(245, 7);
			this.trackChangesCheckBox.Name = "trackChangesCheckBox";
			this.trackChangesCheckBox.Size = new System.Drawing.Size(95, 17);
			this.trackChangesCheckBox.TabIndex = 4;
			this.trackChangesCheckBox.Text = "Track changes";
			this.trackChangesCheckBox.ThreeState = true;
			this.trackChangesCheckBox.UseVisualStyleBackColor = true;
			this.trackChangesCheckBox.Click += new System.EventHandler(this.trackChangesCheckBox_Click);
			// 
			// sourcesListView
			// 
			this.sourcesListView.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom)
						| System.Windows.Forms.AnchorStyles.Left)
						| System.Windows.Forms.AnchorStyles.Right)));
			this.sourcesListView.Font = new System.Drawing.Font("Tahoma", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
			this.sourcesListView.Location = new System.Drawing.Point(5, 31);
			this.sourcesListView.Name = "sourcesListView";
			this.sourcesListView.Size = new System.Drawing.Size(651, 78);
			this.sourcesListView.TabIndex = 4;
			// 
			// deleteButton
			// 
			this.deleteButton.Enabled = false;
			this.deleteButton.Location = new System.Drawing.Point(163, 3);
			this.deleteButton.Margin = new System.Windows.Forms.Padding(2);
			this.deleteButton.Name = "deleteButton";
			this.deleteButton.Size = new System.Drawing.Size(75, 23);
			this.deleteButton.TabIndex = 3;
			this.deleteButton.Text = "Remove";
			this.deleteButton.UseVisualStyleBackColor = true;
			this.deleteButton.Click += new System.EventHandler(this.deleteButton_Click);
			// 
			// recentButton
			// 
			this.recentButton.Image = global::logjoint.Properties.Resources.ArrowDown;
			this.recentButton.ImageAlign = System.Drawing.ContentAlignment.MiddleRight;
			this.recentButton.Location = new System.Drawing.Point(84, 3);
			this.recentButton.Margin = new System.Windows.Forms.Padding(2);
			this.recentButton.Name = "recentButton";
			this.recentButton.Size = new System.Drawing.Size(75, 23);
			this.recentButton.TabIndex = 2;
			this.recentButton.Text = "Recent ";
			this.recentButton.UseVisualStyleBackColor = true;
			this.recentButton.Click += new System.EventHandler(this.recentButton_Click);
			// 
			// addNewLogButton
			// 
			this.addNewLogButton.Location = new System.Drawing.Point(5, 3);
			this.addNewLogButton.Margin = new System.Windows.Forms.Padding(2);
			this.addNewLogButton.Name = "addNewLogButton";
			this.addNewLogButton.Size = new System.Drawing.Size(75, 23);
			this.addNewLogButton.TabIndex = 1;
			this.addNewLogButton.Text = "Add...";
			this.addNewLogButton.UseVisualStyleBackColor = true;
			this.addNewLogButton.Click += new System.EventHandler(this.addNewLogButton_Click);
			// 
			// threadsTabPage
			// 
			this.threadsTabPage.Controls.Add(this.threadsListView);
			this.threadsTabPage.Location = new System.Drawing.Point(4, 22);
			this.threadsTabPage.Margin = new System.Windows.Forms.Padding(2);
			this.threadsTabPage.Name = "threadsTabPage";
			this.threadsTabPage.Padding = new System.Windows.Forms.Padding(2);
			this.threadsTabPage.Size = new System.Drawing.Size(661, 104);
			this.threadsTabPage.TabIndex = 1;
			this.threadsTabPage.Text = "Threads";
			this.threadsTabPage.UseVisualStyleBackColor = true;
			// 
			// threadsListView
			// 
			this.threadsListView.Dock = System.Windows.Forms.DockStyle.Fill;
			this.threadsListView.Location = new System.Drawing.Point(2, 2);
			this.threadsListView.Margin = new System.Windows.Forms.Padding(4);
			this.threadsListView.Name = "threadsListView";
			this.threadsListView.Size = new System.Drawing.Size(657, 100);
			this.threadsListView.TabIndex = 0;
			this.threadsListView.TopItem = null;
			// 
			// filtersTabPage
			// 
			this.filtersTabPage.Controls.Add(this.flowLayoutPanel1);
			this.filtersTabPage.Controls.Add(this.displayFiltersListView);
			this.filtersTabPage.Location = new System.Drawing.Point(4, 22);
			this.filtersTabPage.Name = "filtersTabPage";
			this.filtersTabPage.Padding = new System.Windows.Forms.Padding(3);
			this.filtersTabPage.Size = new System.Drawing.Size(661, 104);
			this.filtersTabPage.TabIndex = 4;
			this.filtersTabPage.Text = "Filtering Rules";
			this.filtersTabPage.UseVisualStyleBackColor = true;
			// 
			// flowLayoutPanel1
			// 
			this.flowLayoutPanel1.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
						| System.Windows.Forms.AnchorStyles.Right)));
			this.flowLayoutPanel1.Controls.Add(this.enableFilteringCheckBox);
			this.flowLayoutPanel1.Controls.Add(this.addDiplayFilterButton);
			this.flowLayoutPanel1.Controls.Add(this.deleteDisplayFilterButton);
			this.flowLayoutPanel1.Controls.Add(this.moveDisplayFilterUpButton);
			this.flowLayoutPanel1.Controls.Add(this.moveDisplayFilterDownButton);
			this.flowLayoutPanel1.Location = new System.Drawing.Point(5, 1);
			this.flowLayoutPanel1.Margin = new System.Windows.Forms.Padding(0);
			this.flowLayoutPanel1.Name = "flowLayoutPanel1";
			this.flowLayoutPanel1.Size = new System.Drawing.Size(651, 27);
			this.flowLayoutPanel1.TabIndex = 1;
			// 
			// enableFilteringCheckBox
			// 
			this.enableFilteringCheckBox.AutoSize = true;
			this.enableFilteringCheckBox.Checked = true;
			this.enableFilteringCheckBox.CheckState = System.Windows.Forms.CheckState.Checked;
			this.enableFilteringCheckBox.Location = new System.Drawing.Point(3, 5);
			this.enableFilteringCheckBox.Margin = new System.Windows.Forms.Padding(3, 5, 3, 3);
			this.enableFilteringCheckBox.MinimumSize = new System.Drawing.Size(120, 0);
			this.enableFilteringCheckBox.Name = "enableFilteringCheckBox";
			this.enableFilteringCheckBox.Size = new System.Drawing.Size(120, 17);
			this.enableFilteringCheckBox.TabIndex = 24;
			this.enableFilteringCheckBox.Text = "Enable filtering";
			this.enableFilteringCheckBox.UseVisualStyleBackColor = true;
			this.enableFilteringCheckBox.CheckedChanged += new System.EventHandler(this.enableFilteringCheckBox_CheckedChanged);
			// 
			// addDiplayFilterButton
			// 
			this.addDiplayFilterButton.Location = new System.Drawing.Point(128, 2);
			this.addDiplayFilterButton.Margin = new System.Windows.Forms.Padding(2);
			this.addDiplayFilterButton.Name = "addDiplayFilterButton";
			this.addDiplayFilterButton.Size = new System.Drawing.Size(75, 23);
			this.addDiplayFilterButton.TabIndex = 25;
			this.addDiplayFilterButton.Text = "Add...";
			this.addDiplayFilterButton.UseVisualStyleBackColor = true;
			this.addDiplayFilterButton.Click += new System.EventHandler(this.addDisplayFilterClick);
			// 
			// deleteDisplayFilterButton
			// 
			this.deleteDisplayFilterButton.Enabled = false;
			this.deleteDisplayFilterButton.Location = new System.Drawing.Point(207, 2);
			this.deleteDisplayFilterButton.Margin = new System.Windows.Forms.Padding(2);
			this.deleteDisplayFilterButton.Name = "deleteDisplayFilterButton";
			this.deleteDisplayFilterButton.Size = new System.Drawing.Size(75, 23);
			this.deleteDisplayFilterButton.TabIndex = 27;
			this.deleteDisplayFilterButton.Text = "Remove";
			this.deleteDisplayFilterButton.UseVisualStyleBackColor = true;
			this.deleteDisplayFilterButton.Click += new System.EventHandler(this.deleteDisplayFilterButton_Click);
			// 
			// moveDisplayFilterUpButton
			// 
			this.moveDisplayFilterUpButton.Enabled = false;
			this.moveDisplayFilterUpButton.Location = new System.Drawing.Point(286, 2);
			this.moveDisplayFilterUpButton.Margin = new System.Windows.Forms.Padding(2);
			this.moveDisplayFilterUpButton.Name = "moveDisplayFilterUpButton";
			this.moveDisplayFilterUpButton.Size = new System.Drawing.Size(75, 23);
			this.moveDisplayFilterUpButton.TabIndex = 28;
			this.moveDisplayFilterUpButton.Text = "Move Up";
			this.moveDisplayFilterUpButton.UseVisualStyleBackColor = true;
			this.moveDisplayFilterUpButton.Click += new System.EventHandler(this.moveDisplayFilterUpButton_Click);
			// 
			// moveDisplayFilterDownButton
			// 
			this.moveDisplayFilterDownButton.Enabled = false;
			this.moveDisplayFilterDownButton.Location = new System.Drawing.Point(365, 2);
			this.moveDisplayFilterDownButton.Margin = new System.Windows.Forms.Padding(2);
			this.moveDisplayFilterDownButton.Name = "moveDisplayFilterDownButton";
			this.moveDisplayFilterDownButton.Size = new System.Drawing.Size(75, 23);
			this.moveDisplayFilterDownButton.TabIndex = 29;
			this.moveDisplayFilterDownButton.Text = "Move Down";
			this.moveDisplayFilterDownButton.UseVisualStyleBackColor = true;
			this.moveDisplayFilterDownButton.Click += new System.EventHandler(this.moveDisplayFilterUpButton_Click);
			// 
			// displayFiltersListView
			// 
			this.displayFiltersListView.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom)
						| System.Windows.Forms.AnchorStyles.Left)
						| System.Windows.Forms.AnchorStyles.Right)));
			this.displayFiltersListView.Font = new System.Drawing.Font("Tahoma", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(204)));
			this.displayFiltersListView.Location = new System.Drawing.Point(5, 31);
			this.displayFiltersListView.Name = "displayFiltersListView";
			this.displayFiltersListView.Size = new System.Drawing.Size(651, 79);
			this.displayFiltersListView.TabIndex = 2;
			// 
			// highlightTabPage
			// 
			this.highlightTabPage.Controls.Add(this.flowLayoutPanel2);
			this.highlightTabPage.Controls.Add(this.hlFiltersListView);
			this.highlightTabPage.Location = new System.Drawing.Point(4, 22);
			this.highlightTabPage.Name = "highlightTabPage";
			this.highlightTabPage.Padding = new System.Windows.Forms.Padding(3);
			this.highlightTabPage.Size = new System.Drawing.Size(661, 104);
			this.highlightTabPage.TabIndex = 5;
			this.highlightTabPage.Text = "Highlighting Rules";
			this.highlightTabPage.UseVisualStyleBackColor = true;
			// 
			// flowLayoutPanel2
			// 
			this.flowLayoutPanel2.Controls.Add(this.enableHighlightingCheckBox);
			this.flowLayoutPanel2.Controls.Add(this.addHLFilterButton);
			this.flowLayoutPanel2.Controls.Add(this.deleteHLFilterButton);
			this.flowLayoutPanel2.Controls.Add(this.moveHLFilterUpButton);
			this.flowLayoutPanel2.Controls.Add(this.moveHLFilterDownButton);
			this.flowLayoutPanel2.Controls.Add(this.prevHightlightedButton);
			this.flowLayoutPanel2.Controls.Add(this.nextHightlightedButton);
			this.flowLayoutPanel2.Location = new System.Drawing.Point(5, 1);
			this.flowLayoutPanel2.Name = "flowLayoutPanel2";
			this.flowLayoutPanel2.Size = new System.Drawing.Size(661, 27);
			this.flowLayoutPanel2.TabIndex = 1;
			// 
			// enableHighlightingCheckBox
			// 
			this.enableHighlightingCheckBox.AutoSize = true;
			this.enableHighlightingCheckBox.Checked = true;
			this.enableHighlightingCheckBox.CheckState = System.Windows.Forms.CheckState.Checked;
			this.enableHighlightingCheckBox.Location = new System.Drawing.Point(3, 5);
			this.enableHighlightingCheckBox.Margin = new System.Windows.Forms.Padding(3, 5, 3, 3);
			this.enableHighlightingCheckBox.MinimumSize = new System.Drawing.Size(120, 0);
			this.enableHighlightingCheckBox.Name = "enableHighlightingCheckBox";
			this.enableHighlightingCheckBox.Size = new System.Drawing.Size(120, 17);
			this.enableHighlightingCheckBox.TabIndex = 0;
			this.enableHighlightingCheckBox.Text = "Enable highlighting";
			this.enableHighlightingCheckBox.UseVisualStyleBackColor = true;
			this.enableHighlightingCheckBox.CheckedChanged += new System.EventHandler(this.enableHighlightingCheckBox_CheckedChanged);
			// 
			// addHLFilterButton
			// 
			this.addHLFilterButton.Location = new System.Drawing.Point(128, 2);
			this.addHLFilterButton.Margin = new System.Windows.Forms.Padding(2);
			this.addHLFilterButton.Name = "addHLFilterButton";
			this.addHLFilterButton.Size = new System.Drawing.Size(75, 23);
			this.addHLFilterButton.TabIndex = 31;
			this.addHLFilterButton.Text = "Add...";
			this.addHLFilterButton.UseVisualStyleBackColor = true;
			this.addHLFilterButton.Click += new System.EventHandler(this.addHLFilterButton_Click);
			// 
			// deleteHLFilterButton
			// 
			this.deleteHLFilterButton.Enabled = false;
			this.deleteHLFilterButton.Location = new System.Drawing.Point(207, 2);
			this.deleteHLFilterButton.Margin = new System.Windows.Forms.Padding(2);
			this.deleteHLFilterButton.Name = "deleteHLFilterButton";
			this.deleteHLFilterButton.Size = new System.Drawing.Size(75, 23);
			this.deleteHLFilterButton.TabIndex = 32;
			this.deleteHLFilterButton.Text = "Remove";
			this.deleteHLFilterButton.UseVisualStyleBackColor = true;
			this.deleteHLFilterButton.Click += new System.EventHandler(this.removeHLButton_Click);
			// 
			// moveHLFilterUpButton
			// 
			this.moveHLFilterUpButton.Enabled = false;
			this.moveHLFilterUpButton.Location = new System.Drawing.Point(286, 2);
			this.moveHLFilterUpButton.Margin = new System.Windows.Forms.Padding(2);
			this.moveHLFilterUpButton.Name = "moveHLFilterUpButton";
			this.moveHLFilterUpButton.Size = new System.Drawing.Size(75, 23);
			this.moveHLFilterUpButton.TabIndex = 33;
			this.moveHLFilterUpButton.Text = "Move Up";
			this.moveHLFilterUpButton.UseVisualStyleBackColor = true;
			this.moveHLFilterUpButton.Click += new System.EventHandler(this.moveHLFilterUpButton_Click);
			// 
			// moveHLFilterDownButton
			// 
			this.moveHLFilterDownButton.Enabled = false;
			this.moveHLFilterDownButton.Location = new System.Drawing.Point(365, 2);
			this.moveHLFilterDownButton.Margin = new System.Windows.Forms.Padding(2);
			this.moveHLFilterDownButton.Name = "moveHLFilterDownButton";
			this.moveHLFilterDownButton.Size = new System.Drawing.Size(75, 23);
			this.moveHLFilterDownButton.TabIndex = 34;
			this.moveHLFilterDownButton.Text = "Move Down";
			this.moveHLFilterDownButton.UseVisualStyleBackColor = true;
			this.moveHLFilterDownButton.Click += new System.EventHandler(this.moveHLFilterUpButton_Click);
			// 
			// prevHightlightedButton
			// 
			this.prevHightlightedButton.Location = new System.Drawing.Point(457, 2);
			this.prevHightlightedButton.Margin = new System.Windows.Forms.Padding(15, 2, 2, 2);
			this.prevHightlightedButton.Name = "prevHightlightedButton";
			this.prevHightlightedButton.Size = new System.Drawing.Size(75, 23);
			this.prevHightlightedButton.TabIndex = 35;
			this.prevHightlightedButton.Text = "<< Prev";
			this.prevHightlightedButton.UseVisualStyleBackColor = true;
			this.prevHightlightedButton.Click += new System.EventHandler(this.prevHightlightedButton_Click);
			// 
			// nextHightlightedButton
			// 
			this.nextHightlightedButton.Location = new System.Drawing.Point(536, 2);
			this.nextHightlightedButton.Margin = new System.Windows.Forms.Padding(2);
			this.nextHightlightedButton.Name = "nextHightlightedButton";
			this.nextHightlightedButton.Size = new System.Drawing.Size(75, 23);
			this.nextHightlightedButton.TabIndex = 36;
			this.nextHightlightedButton.Text = "Next >>";
			this.nextHightlightedButton.UseVisualStyleBackColor = true;
			this.nextHightlightedButton.Click += new System.EventHandler(this.nextHightlightedButton_Click);
			// 
			// hlFiltersListView
			// 
			this.hlFiltersListView.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom)
						| System.Windows.Forms.AnchorStyles.Left)
						| System.Windows.Forms.AnchorStyles.Right)));
			this.hlFiltersListView.Font = new System.Drawing.Font("Tahoma", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(204)));
			this.hlFiltersListView.Location = new System.Drawing.Point(5, 31);
			this.hlFiltersListView.Name = "hlFiltersListView";
			this.hlFiltersListView.Size = new System.Drawing.Size(651, 79);
			this.hlFiltersListView.TabIndex = 2;
			// 
			// searchTabPage
			// 
			this.searchTabPage.Controls.Add(this.respectFilteringRulesCheckBox);
			this.searchTabPage.Controls.Add(this.searchAmongLoadedMessagesRadioButton);
			this.searchTabPage.Controls.Add(this.searchAllOccurancesRadioButton);
			this.searchTabPage.Controls.Add(this.searchWithinCurrentThreadCheckbox);
			this.searchTabPage.Controls.Add(this.panel1);
			this.searchTabPage.Controls.Add(this.doSearchButton);
			this.searchTabPage.Controls.Add(this.regExpCheckBox);
			this.searchTabPage.Controls.Add(this.searchUpCheckbox);
			this.searchTabPage.Controls.Add(this.wholeWordCheckbox);
			this.searchTabPage.Controls.Add(this.matchCaseCheckbox);
			this.searchTabPage.Controls.Add(this.searchTextBox);
			this.searchTabPage.Location = new System.Drawing.Point(4, 22);
			this.searchTabPage.Margin = new System.Windows.Forms.Padding(2);
			this.searchTabPage.Name = "searchTabPage";
			this.searchTabPage.Padding = new System.Windows.Forms.Padding(2);
			this.searchTabPage.Size = new System.Drawing.Size(661, 104);
			this.searchTabPage.TabIndex = 2;
			this.searchTabPage.Text = "Search";
			this.searchTabPage.UseVisualStyleBackColor = true;
			// 
			// respectFilteringRulesCheckBox
			// 
			this.respectFilteringRulesCheckBox.AutoSize = true;
			this.respectFilteringRulesCheckBox.Location = new System.Drawing.Point(435, 64);
			this.respectFilteringRulesCheckBox.Margin = new System.Windows.Forms.Padding(2);
			this.respectFilteringRulesCheckBox.Name = "respectFilteringRulesCheckBox";
			this.respectFilteringRulesCheckBox.Size = new System.Drawing.Size(127, 17);
			this.respectFilteringRulesCheckBox.TabIndex = 28;
			this.respectFilteringRulesCheckBox.Text = "Respect filtering rules";
			this.respectFilteringRulesCheckBox.UseVisualStyleBackColor = true;
			// 
			// searchAmongLoadedMessagesRadioButton
			// 
			this.searchAmongLoadedMessagesRadioButton.AutoSize = true;
			this.searchAmongLoadedMessagesRadioButton.Checked = true;
			this.searchAmongLoadedMessagesRadioButton.Location = new System.Drawing.Point(414, 28);
			this.searchAmongLoadedMessagesRadioButton.Name = "searchAmongLoadedMessagesRadioButton";
			this.searchAmongLoadedMessagesRadioButton.Size = new System.Drawing.Size(179, 17);
			this.searchAmongLoadedMessagesRadioButton.TabIndex = 25;
			this.searchAmongLoadedMessagesRadioButton.TabStop = true;
			this.searchAmongLoadedMessagesRadioButton.Text = "Search among loaded messages";
			this.searchAmongLoadedMessagesRadioButton.UseVisualStyleBackColor = true;
			this.searchAmongLoadedMessagesRadioButton.CheckedChanged += new System.EventHandler(this.searchModeRadioButtonChecked);
			// 
			// searchAllOccurancesRadioButton
			// 
			this.searchAllOccurancesRadioButton.AutoSize = true;
			this.searchAllOccurancesRadioButton.Location = new System.Drawing.Point(414, 46);
			this.searchAllOccurancesRadioButton.Name = "searchAllOccurancesRadioButton";
			this.searchAllOccurancesRadioButton.Size = new System.Drawing.Size(131, 17);
			this.searchAllOccurancesRadioButton.TabIndex = 27;
			this.searchAllOccurancesRadioButton.Text = "Search all occurances";
			this.searchAllOccurancesRadioButton.UseVisualStyleBackColor = true;
			this.searchAllOccurancesRadioButton.CheckedChanged += new System.EventHandler(this.searchModeRadioButtonChecked);
			// 
			// searchWithinCurrentThreadCheckbox
			// 
			this.searchWithinCurrentThreadCheckbox.AutoSize = true;
			this.searchWithinCurrentThreadCheckbox.Location = new System.Drawing.Point(109, 46);
			this.searchWithinCurrentThreadCheckbox.Margin = new System.Windows.Forms.Padding(2);
			this.searchWithinCurrentThreadCheckbox.Name = "searchWithinCurrentThreadCheckbox";
			this.searchWithinCurrentThreadCheckbox.Size = new System.Drawing.Size(159, 17);
			this.searchWithinCurrentThreadCheckbox.TabIndex = 23;
			this.searchWithinCurrentThreadCheckbox.Text = "Search within current thread";
			this.searchWithinCurrentThreadCheckbox.UseVisualStyleBackColor = true;
			// 
			// panel1
			// 
			this.panel1.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom)
						| System.Windows.Forms.AnchorStyles.Left)));
			this.panel1.Controls.Add(this.messageTypesCheckedListBox);
			this.panel1.Location = new System.Drawing.Point(290, 30);
			this.panel1.Name = "panel1";
			this.panel1.Size = new System.Drawing.Size(115, 76);
			this.panel1.TabIndex = 24;
			// 
			// messageTypesCheckedListBox
			// 
			this.messageTypesCheckedListBox.CheckOnClick = true;
			this.messageTypesCheckedListBox.Dock = System.Windows.Forms.DockStyle.Fill;
			this.messageTypesCheckedListBox.FormattingEnabled = true;
			this.messageTypesCheckedListBox.IntegralHeight = false;
			this.messageTypesCheckedListBox.Items.AddRange(new object[] {
            "Errors",
            "Warnings",
            "Infos",
            "Frames"});
			this.messageTypesCheckedListBox.Location = new System.Drawing.Point(0, 0);
			this.messageTypesCheckedListBox.Margin = new System.Windows.Forms.Padding(2);
			this.messageTypesCheckedListBox.Name = "messageTypesCheckedListBox";
			this.messageTypesCheckedListBox.Size = new System.Drawing.Size(115, 76);
			this.messageTypesCheckedListBox.TabIndex = 25;
			// 
			// doSearchButton
			// 
			this.doSearchButton.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
			this.doSearchButton.Location = new System.Drawing.Point(607, 5);
			this.doSearchButton.Margin = new System.Windows.Forms.Padding(2);
			this.doSearchButton.Name = "doSearchButton";
			this.doSearchButton.Size = new System.Drawing.Size(51, 22);
			this.doSearchButton.TabIndex = 26;
			this.doSearchButton.Text = "Find";
			this.doSearchButton.UseVisualStyleBackColor = true;
			this.doSearchButton.Click += new System.EventHandler(this.doSearchButton_Click);
			// 
			// regExpCheckBox
			// 
			this.regExpCheckBox.AutoSize = true;
			this.regExpCheckBox.Location = new System.Drawing.Point(109, 28);
			this.regExpCheckBox.Margin = new System.Windows.Forms.Padding(2);
			this.regExpCheckBox.Name = "regExpCheckBox";
			this.regExpCheckBox.Size = new System.Drawing.Size(63, 17);
			this.regExpCheckBox.TabIndex = 22;
			this.regExpCheckBox.Text = "Regexp";
			this.regExpCheckBox.UseVisualStyleBackColor = true;
			// 
			// searchUpCheckbox
			// 
			this.searchUpCheckbox.AutoSize = true;
			this.searchUpCheckbox.Location = new System.Drawing.Point(5, 64);
			this.searchUpCheckbox.Margin = new System.Windows.Forms.Padding(2);
			this.searchUpCheckbox.Name = "searchUpCheckbox";
			this.searchUpCheckbox.Size = new System.Drawing.Size(75, 17);
			this.searchUpCheckbox.TabIndex = 21;
			this.searchUpCheckbox.Text = "Search up";
			this.searchUpCheckbox.UseVisualStyleBackColor = true;
			// 
			// wholeWordCheckbox
			// 
			this.wholeWordCheckbox.AutoSize = true;
			this.wholeWordCheckbox.Location = new System.Drawing.Point(5, 46);
			this.wholeWordCheckbox.Margin = new System.Windows.Forms.Padding(2);
			this.wholeWordCheckbox.Name = "wholeWordCheckbox";
			this.wholeWordCheckbox.Size = new System.Drawing.Size(83, 17);
			this.wholeWordCheckbox.TabIndex = 20;
			this.wholeWordCheckbox.Text = "Whole word";
			this.wholeWordCheckbox.UseVisualStyleBackColor = true;
			// 
			// matchCaseCheckbox
			// 
			this.matchCaseCheckbox.AutoSize = true;
			this.matchCaseCheckbox.Location = new System.Drawing.Point(5, 28);
			this.matchCaseCheckbox.Margin = new System.Windows.Forms.Padding(2);
			this.matchCaseCheckbox.Name = "matchCaseCheckbox";
			this.matchCaseCheckbox.Size = new System.Drawing.Size(82, 17);
			this.matchCaseCheckbox.TabIndex = 19;
			this.matchCaseCheckbox.Text = "Match case";
			this.matchCaseCheckbox.UseVisualStyleBackColor = true;
			// 
			// searchTextBox
			// 
			this.searchTextBox.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
						| System.Windows.Forms.AnchorStyles.Right)));
			this.searchTextBox.FormattingEnabled = true;
			this.searchTextBox.Location = new System.Drawing.Point(5, 5);
			this.searchTextBox.Margin = new System.Windows.Forms.Padding(2);
			this.searchTextBox.Name = "searchTextBox";
			this.searchTextBox.Size = new System.Drawing.Size(598, 21);
			this.searchTextBox.TabIndex = 18;
			this.searchTextBox.DropDown += new System.EventHandler(this.searchTextBox_DropDown);
			// 
			// navigationTabPage
			// 
			this.navigationTabPage.Controls.Add(this.prevBookmarkButton);
			this.navigationTabPage.Controls.Add(this.deleteAllBookmarksButton);
			this.navigationTabPage.Controls.Add(this.nextBookmarkButton);
			this.navigationTabPage.Controls.Add(this.toggleBookmarkButton);
			this.navigationTabPage.Controls.Add(this.bookmarksView);
			this.navigationTabPage.Location = new System.Drawing.Point(4, 22);
			this.navigationTabPage.Margin = new System.Windows.Forms.Padding(2);
			this.navigationTabPage.Name = "navigationTabPage";
			this.navigationTabPage.Padding = new System.Windows.Forms.Padding(2);
			this.navigationTabPage.Size = new System.Drawing.Size(661, 104);
			this.navigationTabPage.TabIndex = 3;
			this.navigationTabPage.Text = "Bookmarks";
			this.navigationTabPage.UseVisualStyleBackColor = true;
			// 
			// bookmarksView
			// 
			this.bookmarksView.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom)
						| System.Windows.Forms.AnchorStyles.Left)
						| System.Windows.Forms.AnchorStyles.Right)));
			this.bookmarksView.Font = new System.Drawing.Font("Tahoma", 8.5F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
			this.bookmarksView.Location = new System.Drawing.Point(74, 5);
			this.bookmarksView.Margin = new System.Windows.Forms.Padding(0);
			this.bookmarksView.Name = "bookmarksView";
			this.bookmarksView.Size = new System.Drawing.Size(583, 104);
			this.bookmarksView.TabIndex = 3;
			// 
			// splitContainer1
			// 
			this.splitContainer1.BorderStyle = System.Windows.Forms.BorderStyle.Fixed3D;
			this.splitContainer1.Dock = System.Windows.Forms.DockStyle.Fill;
			this.splitContainer1.FixedPanel = System.Windows.Forms.FixedPanel.Panel1;
			this.splitContainer1.Location = new System.Drawing.Point(0, 0);
			this.splitContainer1.Margin = new System.Windows.Forms.Padding(2);
			this.splitContainer1.Name = "splitContainer1";
			// 
			// splitContainer1.Panel1
			// 
			this.splitContainer1.Panel1.Controls.Add(this.timeLineControl);
			this.splitContainer1.Panel1.Controls.Add(this.timelineControlPanel);
			// 
			// splitContainer1.Panel2
			// 
			this.splitContainer1.Panel2.Controls.Add(this.splitContainer3);
			this.splitContainer1.Size = new System.Drawing.Size(669, 340);
			this.splitContainer1.SplitterDistance = 133;
			this.splitContainer1.SplitterWidth = 3;
			this.splitContainer1.TabIndex = 2;
			// 
			// timeLineControl
			// 
			this.timeLineControl.BackColor = System.Drawing.Color.White;
			this.timeLineControl.Dock = System.Windows.Forms.DockStyle.Fill;
			this.timeLineControl.Location = new System.Drawing.Point(0, 16);
			this.timeLineControl.Margin = new System.Windows.Forms.Padding(2);
			this.timeLineControl.MinimumSize = new System.Drawing.Size(10, 40);
			this.timeLineControl.Name = "timeLineControl";
			this.timeLineControl.Size = new System.Drawing.Size(129, 320);
			this.timeLineControl.TabIndex = 15;
			this.timeLineControl.Text = "timeLineControl1";
			this.timeLineControl.Navigate += new System.EventHandler<LogJoint.UI.TimeNavigateEventArgs>(this.timeLineControl1_Navigate);
			// 
			// timelineControlPanel
			// 
			this.timelineControlPanel.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
			this.timelineControlPanel.Dock = System.Windows.Forms.DockStyle.Top;
			this.timelineControlPanel.Location = new System.Drawing.Point(0, 0);
			this.timelineControlPanel.Margin = new System.Windows.Forms.Padding(0);
			this.timelineControlPanel.Name = "timelineControlPanel";
			this.timelineControlPanel.Size = new System.Drawing.Size(129, 16);
			this.timelineControlPanel.TabIndex = 13;
			// 
			// splitContainer3
			// 
			this.splitContainer3.Dock = System.Windows.Forms.DockStyle.Fill;
			this.splitContainer3.Location = new System.Drawing.Point(0, 0);
			this.splitContainer3.Name = "splitContainer3";
			this.splitContainer3.Orientation = System.Windows.Forms.Orientation.Horizontal;
			// 
			// splitContainer3.Panel1
			// 
			this.splitContainer3.Panel1.Controls.Add(this.logViewerControl);
			// 
			// splitContainer3.Panel2
			// 
			this.splitContainer3.Panel2.Controls.Add(this.searchResultViewer);
			this.splitContainer3.Panel2.Controls.Add(this.panel3);
			this.splitContainer3.Size = new System.Drawing.Size(529, 336);
			this.splitContainer3.SplitterDistance = 168;
			this.splitContainer3.SplitterWidth = 5;
			this.splitContainer3.TabIndex = 12;
			// 
			// logViewerControl
			// 
			this.logViewerControl.BackColor = System.Drawing.Color.White;
			this.logViewerControl.Dock = System.Windows.Forms.DockStyle.Fill;
			this.logViewerControl.Location = new System.Drawing.Point(0, 0);
			this.logViewerControl.Margin = new System.Windows.Forms.Padding(2);
			this.logViewerControl.Name = "logViewerControl";
			this.logViewerControl.ShowMilliseconds = false;
			this.logViewerControl.ShowTime = false;
			this.logViewerControl.Size = new System.Drawing.Size(529, 168);
			this.logViewerControl.TabIndex = 11;
			this.logViewerControl.Text = "logViewerControl1";
			// 
			// searchResultViewer
			// 
			this.searchResultViewer.BackColor = System.Drawing.Color.White;
			this.searchResultViewer.Dock = System.Windows.Forms.DockStyle.Fill;
			this.searchResultViewer.Location = new System.Drawing.Point(0, 23);
			this.searchResultViewer.Margin = new System.Windows.Forms.Padding(2);
			this.searchResultViewer.Name = "searchResultViewer";
			this.searchResultViewer.ShowMilliseconds = false;
			this.searchResultViewer.ShowTime = false;
			this.searchResultViewer.Size = new System.Drawing.Size(529, 140);
			this.searchResultViewer.TabIndex = 12;
			this.searchResultViewer.Text = "logViewerControl1";
			// 
			// panel3
			// 
			this.panel3.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
			this.panel3.Controls.Add(this.searchProgressBar);
			this.panel3.Controls.Add(this.searchResultLabel);
			this.panel3.Controls.Add(this.label1);
			this.panel3.Controls.Add(this.closeSearchResultButton);
			this.panel3.Dock = System.Windows.Forms.DockStyle.Top;
			this.panel3.Location = new System.Drawing.Point(0, 0);
			this.panel3.Margin = new System.Windows.Forms.Padding(0);
			this.panel3.Name = "panel3";
			this.panel3.Size = new System.Drawing.Size(529, 23);
			this.panel3.TabIndex = 13;
			// 
			// searchProgressBar
			// 
			this.searchProgressBar.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
			this.searchProgressBar.Location = new System.Drawing.Point(338, 4);
			this.searchProgressBar.Name = "searchProgressBar";
			this.searchProgressBar.Size = new System.Drawing.Size(162, 13);
			this.searchProgressBar.TabIndex = 3;
			// 
			// searchResultLabel
			// 
			this.searchResultLabel.AutoSize = true;
			this.searchResultLabel.Font = new System.Drawing.Font("Tahoma", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(204)));
			this.searchResultLabel.Location = new System.Drawing.Point(79, 3);
			this.searchResultLabel.Name = "searchResultLabel";
			this.searchResultLabel.Size = new System.Drawing.Size(14, 13);
			this.searchResultLabel.TabIndex = 2;
			this.searchResultLabel.Text = "0";
			// 
			// label1
			// 
			this.label1.AutoSize = true;
			this.label1.Location = new System.Drawing.Point(4, 3);
			this.label1.Name = "label1";
			this.label1.Size = new System.Drawing.Size(74, 13);
			this.label1.TabIndex = 1;
			this.label1.Text = "Search result:";
			// 
			// closeSearchResultButton
			// 
			this.closeSearchResultButton.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
			this.closeSearchResultButton.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
			this.closeSearchResultButton.Image = ((System.Drawing.Image)(resources.GetObject("closeSearchResultButton.Image")));
			this.closeSearchResultButton.Location = new System.Drawing.Point(507, 2);
			this.closeSearchResultButton.Margin = new System.Windows.Forms.Padding(0);
			this.closeSearchResultButton.Name = "closeSearchResultButton";
			this.closeSearchResultButton.Size = new System.Drawing.Size(17, 17);
			this.closeSearchResultButton.TabIndex = 0;
			this.closeSearchResultButton.UseVisualStyleBackColor = true;
			this.closeSearchResultButton.Click += new System.EventHandler(this.closeSearchResultButton_Click);
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
			this.button6.Click += new System.EventHandler(this.moveHLFilterUpButton_Click);
			// 
			// panel2
			// 
			this.panel2.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
			this.panel2.Controls.Add(this.aboutLinkLabel);
			this.panel2.Location = new System.Drawing.Point(605, 3);
			this.panel2.Name = "panel2";
			this.panel2.Size = new System.Drawing.Size(60, 15);
			this.panel2.TabIndex = 13;
			// 
			// saveFileDialog1
			// 
			this.saveFileDialog1.CheckPathExists = false;
			// 
			// MainForm
			// 
			this.AllowDrop = true;
			this.AutoScaleDimensions = new System.Drawing.SizeF(96F, 96F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Dpi;
			this.ClientSize = new System.Drawing.Size(669, 496);
			this.Controls.Add(this.panel2);
			this.Controls.Add(this.splitContainer2);
			this.Controls.Add(this.statusStrip1);
			this.Font = new System.Drawing.Font("Tahoma", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(204)));
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
			this.splitContainer2.Panel1.ResumeLayout(false);
			this.splitContainer2.Panel2.ResumeLayout(false);
			((System.ComponentModel.ISupportInitialize)(this.splitContainer2)).EndInit();
			this.splitContainer2.ResumeLayout(false);
			this.menuTabControl.ResumeLayout(false);
			this.sourcesTabPage.ResumeLayout(false);
			this.sourcesTabPage.PerformLayout();
			this.threadsTabPage.ResumeLayout(false);
			this.filtersTabPage.ResumeLayout(false);
			this.flowLayoutPanel1.ResumeLayout(false);
			this.flowLayoutPanel1.PerformLayout();
			this.highlightTabPage.ResumeLayout(false);
			this.flowLayoutPanel2.ResumeLayout(false);
			this.flowLayoutPanel2.PerformLayout();
			this.searchTabPage.ResumeLayout(false);
			this.searchTabPage.PerformLayout();
			this.panel1.ResumeLayout(false);
			this.navigationTabPage.ResumeLayout(false);
			this.splitContainer1.Panel1.ResumeLayout(false);
			this.splitContainer1.Panel2.ResumeLayout(false);
			((System.ComponentModel.ISupportInitialize)(this.splitContainer1)).EndInit();
			this.splitContainer1.ResumeLayout(false);
			this.splitContainer3.Panel1.ResumeLayout(false);
			this.splitContainer3.Panel2.ResumeLayout(false);
			((System.ComponentModel.ISupportInitialize)(this.splitContainer3)).EndInit();
			this.splitContainer3.ResumeLayout(false);
			this.panel3.ResumeLayout(false);
			this.panel3.PerformLayout();
			this.panel2.ResumeLayout(false);
			this.ResumeLayout(false);
			this.PerformLayout();

		}

		#endregion

		private UI.LogViewerControl logViewerControl;
		private System.Windows.Forms.ImageList imageList1;
		private System.Windows.Forms.OpenFileDialog openFileDialog1;
		private System.Windows.Forms.Timer updateViewTimer;
		private System.Windows.Forms.ContextMenuStrip mruContextMenuStrip;
		private System.Windows.Forms.TabPage sourcesTabPage;
		private System.Windows.Forms.Button deleteButton;
		private System.Windows.Forms.Button recentButton;
		private System.Windows.Forms.Button addNewLogButton;
		private System.Windows.Forms.TabPage threadsTabPage;
		private System.Windows.Forms.TabPage searchTabPage;
		private System.Windows.Forms.Button doSearchButton;
		private System.Windows.Forms.CheckedListBox messageTypesCheckedListBox;
		private System.Windows.Forms.CheckBox regExpCheckBox;
		private System.Windows.Forms.CheckBox searchUpCheckbox;
		private System.Windows.Forms.CheckBox wholeWordCheckbox;
		private System.Windows.Forms.CheckBox matchCaseCheckbox;
		private UI.TimeLineControl timeLineControl;
		private System.Windows.Forms.SplitContainer splitContainer1;
		private System.Windows.Forms.StatusStrip statusStrip1;
		private System.Windows.Forms.ToolStripStatusLabel toolStripStatusLabel;
		private System.Windows.Forms.TabPage navigationTabPage;
		private System.Windows.Forms.Button prevBookmarkButton;
		private System.Windows.Forms.Button deleteAllBookmarksButton;
		private System.Windows.Forms.Button nextBookmarkButton;
		private System.Windows.Forms.Button toggleBookmarkButton;
		private LogJoint.UI.ThreadsListView threadsListView;
		private LogJoint.UI.BookmarksView bookmarksView;
		private System.Windows.Forms.Panel panel1;
		private UI.SourcesListView sourcesListView;
		private System.Windows.Forms.ToolStripDropDownButton cancelShiftingDropDownButton;
		private System.Windows.Forms.ToolStripStatusLabel cancelShiftingLabel;
		private System.Windows.Forms.Button deleteDisplayFilterButton;
		private System.Windows.Forms.Button addDiplayFilterButton;
		private System.Windows.Forms.Button moveDisplayFilterDownButton;
		private System.Windows.Forms.Button moveDisplayFilterUpButton;
		private System.Windows.Forms.SplitContainer splitContainer2;
		private System.Windows.Forms.LinkLabel aboutLinkLabel;
		private System.Windows.Forms.ToolStripStatusLabel toolStripStatusImage;
		private System.Windows.Forms.CheckBox trackChangesCheckBox;
        private System.Windows.Forms.ToolStripStatusLabel toolStripAnalizingImage;
        private System.Windows.Forms.ToolStripStatusLabel toolStripAnalizingLabel;
		private System.Windows.Forms.TabPage highlightTabPage;
		private System.Windows.Forms.Button moveHLFilterDownButton;
		private System.Windows.Forms.Button moveHLFilterUpButton;
		private System.Windows.Forms.Button deleteHLFilterButton;
		private System.Windows.Forms.Button addHLFilterButton;
		private FiltersListView hlFiltersListView;
		private System.Windows.Forms.Button button2;
		private System.Windows.Forms.Button button3;
		private System.Windows.Forms.Button button4;
		private System.Windows.Forms.Button button5;
		private System.Windows.Forms.CheckBox enableFilteringCheckBox;
		private System.Windows.Forms.FlowLayoutPanel flowLayoutPanel1;
		private System.Windows.Forms.FlowLayoutPanel flowLayoutPanel2;
		private System.Windows.Forms.CheckBox enableHighlightingCheckBox;
		private LogJoint.UI.SearchTextBox searchTextBox;
		internal FiltersListView displayFiltersListView;
		internal System.Windows.Forms.TabPage filtersTabPage;
		internal System.Windows.Forms.TabControl menuTabControl;
		private System.Windows.Forms.ToolTip toolTip1;
		private TimelineControlPanel timelineControlPanel;
		private System.Windows.Forms.Button prevHightlightedButton;
		private System.Windows.Forms.Button nextHightlightedButton;
		private System.Windows.Forms.Button button6;
		private System.Windows.Forms.CheckBox searchWithinCurrentThreadCheckbox;
		private System.Windows.Forms.Panel panel2;
		private System.Windows.Forms.SaveFileDialog saveFileDialog1;
		private System.Windows.Forms.SplitContainer splitContainer3;
		private LogViewerControl searchResultViewer;
		private System.Windows.Forms.Panel panel3;
		private System.Windows.Forms.Label searchResultLabel;
		private System.Windows.Forms.Label label1;
		private System.Windows.Forms.Button closeSearchResultButton;
		private System.Windows.Forms.CheckBox respectFilteringRulesCheckBox;
		private System.Windows.Forms.RadioButton searchAmongLoadedMessagesRadioButton;
		private System.Windows.Forms.RadioButton searchAllOccurancesRadioButton;
		private System.Windows.Forms.ProgressBar searchProgressBar;

	}
}

