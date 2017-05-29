namespace LogJoint.UI.Postprocessing.StateInspector
{
	partial class StateInspectorForm
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
			System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(StateInspectorForm));
			this.splitContainer1 = new System.Windows.Forms.SplitContainer();
			this.objectsTreeView = new System.Windows.Forms.MyTreeView();
			this.panel2 = new System.Windows.Forms.Panel();
			this.flowLayoutPanel1 = new System.Windows.Forms.FlowLayoutPanel();
			this.aliveObjectsComment = new System.Windows.Forms.Label();
			this.deletedObjectsComment = new System.Windows.Forms.Label();
			this.yetToBeCreatedObjectsComment = new System.Windows.Forms.Label();
			this.label1 = new System.Windows.Forms.Label();
            this.splitContainer3 = new System.Windows.Forms.ExtendedSplitContainer();
			this.propertiesDataGridView = new System.Windows.Forms.MyDataGridView();
			this.propertiesCaptionPanel = new System.Windows.Forms.FlowLayoutPanel();
			this.propertiesLabel = new System.Windows.Forms.Label();
			this.currentTimeLabel = new System.Windows.Forms.Label();
			this.contextMenuStrip = new System.Windows.Forms.ContextMenuStrip(this.components);
			this.selectedObjectStateHistoryControl = new LogJoint.UI.Postprocessing.StateInspector.InspectedObjectEventsHistoryControl();
			((System.ComponentModel.ISupportInitialize)(this.splitContainer1)).BeginInit();
			this.splitContainer1.Panel1.SuspendLayout();
			this.splitContainer1.Panel2.SuspendLayout();
			this.splitContainer1.SuspendLayout();
			this.panel2.SuspendLayout();
			this.flowLayoutPanel1.SuspendLayout();
			((System.ComponentModel.ISupportInitialize)(this.splitContainer3)).BeginInit();
			this.splitContainer3.Panel1.SuspendLayout();
			this.splitContainer3.Panel2.SuspendLayout();
			this.splitContainer3.SuspendLayout();
			((System.ComponentModel.ISupportInitialize)(this.propertiesDataGridView)).BeginInit();
			this.propertiesCaptionPanel.SuspendLayout();
			this.SuspendLayout();
			// 
			// splitContainer1
			// 
			this.splitContainer1.Dock = System.Windows.Forms.DockStyle.Fill;
			this.splitContainer1.Location = new System.Drawing.Point(0, 0);
			this.splitContainer1.Margin = new System.Windows.Forms.Padding(0);
			this.splitContainer1.Name = "splitContainer1";
			// 
			// splitContainer1.Panel1
			// 
			this.splitContainer1.Panel1.Controls.Add(this.objectsTreeView);
			this.splitContainer1.Panel1.Controls.Add(this.panel2);
			this.splitContainer1.Panel1.Padding = new System.Windows.Forms.Padding(3, 3, 0, 3);
			// 
			// splitContainer1.Panel2
			// 
			this.splitContainer1.Panel2.Controls.Add(this.splitContainer3);
			this.splitContainer1.Panel2.Padding = new System.Windows.Forms.Padding(0, 3, 3, 3);
			this.splitContainer1.Size = new System.Drawing.Size(835, 527);
			this.splitContainer1.SplitterDistance = 400;
			this.splitContainer1.TabIndex = 6;
			// 
			// objectsTreeView
			// 
			this.objectsTreeView.Dock = System.Windows.Forms.DockStyle.Fill;
			this.objectsTreeView.DrawMode = System.Windows.Forms.TreeViewDrawMode.OwnerDrawText;
			this.objectsTreeView.FullRowSelect = true;
			this.objectsTreeView.HideSelection = false;
			this.objectsTreeView.Location = new System.Drawing.Point(3, 22);
			this.objectsTreeView.Margin = new System.Windows.Forms.Padding(0);
			this.objectsTreeView.Name = "objectsTreeView";
			this.objectsTreeView.SelectedNodes = ((System.Collections.Generic.IEnumerable<System.Windows.Forms.TreeNode>)(resources.GetObject("objectsTreeView.SelectedNodes")));
			this.objectsTreeView.Size = new System.Drawing.Size(397, 502);
			this.objectsTreeView.TabIndex = 3;
			this.objectsTreeView.SelectedNodesChanged += new System.EventHandler(this.objectsTreeView_SelectedNodesChanged);
			this.objectsTreeView.DrawNode += new System.Windows.Forms.DrawTreeNodeEventHandler(this.objectsTreeView_DrawNode);
			this.objectsTreeView.KeyDown += new System.Windows.Forms.KeyEventHandler(this.objectsTreeView_KeyDown);
			this.objectsTreeView.ContextMenuStrip = contextMenuStrip;
			// 
			// panel2
			// 
			this.panel2.AutoSize = true;
			this.panel2.Controls.Add(this.flowLayoutPanel1);
			this.panel2.Controls.Add(this.label1);
			this.panel2.Dock = System.Windows.Forms.DockStyle.Top;
			this.panel2.Location = new System.Drawing.Point(3, 3);
			this.panel2.Margin = new System.Windows.Forms.Padding(0);
			this.panel2.Name = "panel2";
			this.panel2.Padding = new System.Windows.Forms.Padding(2);
			this.panel2.Size = new System.Drawing.Size(397, 19);
			this.panel2.TabIndex = 4;
			// 
			// flowLayoutPanel1
			// 
			this.flowLayoutPanel1.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
			this.flowLayoutPanel1.AutoSize = true;
			this.flowLayoutPanel1.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
			this.flowLayoutPanel1.Controls.Add(this.aliveObjectsComment);
			this.flowLayoutPanel1.Controls.Add(this.deletedObjectsComment);
			this.flowLayoutPanel1.Controls.Add(this.yetToBeCreatedObjectsComment);
			this.flowLayoutPanel1.Location = new System.Drawing.Point(176, 0);
			this.flowLayoutPanel1.Margin = new System.Windows.Forms.Padding(0);
			this.flowLayoutPanel1.Name = "flowLayoutPanel1";
			this.flowLayoutPanel1.Size = new System.Drawing.Size(218, 17);
			this.flowLayoutPanel1.TabIndex = 2;
			this.flowLayoutPanel1.Visible = false;
			// 
			// aliveObjectsComment
			// 
			this.aliveObjectsComment.AutoSize = true;
			this.aliveObjectsComment.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(64)))), ((int)(((byte)(64)))), ((int)(((byte)(64)))));
			this.aliveObjectsComment.Location = new System.Drawing.Point(0, 0);
			this.aliveObjectsComment.Margin = new System.Windows.Forms.Padding(0, 0, 5, 0);
			this.aliveObjectsComment.Name = "aliveObjectsComment";
			this.aliveObjectsComment.Size = new System.Drawing.Size(35, 17);
			this.aliveObjectsComment.TabIndex = 1;
			this.aliveObjectsComment.Text = "Alive";
			// 
			// deletedObjectsComment
			// 
			this.deletedObjectsComment.AutoSize = true;
			this.deletedObjectsComment.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(64)))), ((int)(((byte)(64)))), ((int)(((byte)(64)))));
			this.deletedObjectsComment.Location = new System.Drawing.Point(40, 0);
			this.deletedObjectsComment.Margin = new System.Windows.Forms.Padding(0, 0, 5, 0);
			this.deletedObjectsComment.Name = "deletedObjectsComment";
			this.deletedObjectsComment.Size = new System.Drawing.Size(54, 17);
			this.deletedObjectsComment.TabIndex = 1;
			this.deletedObjectsComment.Text = "Deleted";
			// 
			// yetToBeCreatedObjectsComment
			// 
			this.yetToBeCreatedObjectsComment.AutoSize = true;
			this.yetToBeCreatedObjectsComment.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(64)))), ((int)(((byte)(64)))), ((int)(((byte)(64)))));
			this.yetToBeCreatedObjectsComment.Location = new System.Drawing.Point(99, 0);
			this.yetToBeCreatedObjectsComment.Margin = new System.Windows.Forms.Padding(0, 0, 5, 0);
			this.yetToBeCreatedObjectsComment.Name = "yetToBeCreatedObjectsComment";
			this.yetToBeCreatedObjectsComment.Size = new System.Drawing.Size(114, 17);
			this.yetToBeCreatedObjectsComment.TabIndex = 1;
			this.yetToBeCreatedObjectsComment.Text = "Yet to be created";
			// 
			// label1
			// 
			this.label1.AutoSize = true;
			this.label1.Location = new System.Drawing.Point(0, 0);
			this.label1.Margin = new System.Windows.Forms.Padding(0, 0, 3, 0);
			this.label1.Name = "label1";
			this.label1.Size = new System.Drawing.Size(60, 17);
			this.label1.TabIndex = 1;
			this.label1.Text = "Objects:";
			// 
			// splitContainer3
			// 
			this.splitContainer3.Dock = System.Windows.Forms.DockStyle.Fill;
			this.splitContainer3.Location = new System.Drawing.Point(0, 3);
			this.splitContainer3.Name = "splitContainer3";
			this.splitContainer3.Orientation = System.Windows.Forms.Orientation.Horizontal;
			// 
			// splitContainer3.Panel1
			// 
			this.splitContainer3.Panel1.Controls.Add(this.propertiesDataGridView);
			this.splitContainer3.Panel1.Controls.Add(this.propertiesCaptionPanel);
			// 
			// splitContainer3.Panel2
			// 
			this.splitContainer3.Panel2.Controls.Add(this.selectedObjectStateHistoryControl);
			this.splitContainer3.Size = new System.Drawing.Size(428, 521);
			this.splitContainer3.SplitterDistance = 260;
			this.splitContainer3.TabIndex = 7;
			// 
			// propertiesDataGridView
			// 
			this.propertiesDataGridView.AllowUserToAddRows = false;
			this.propertiesDataGridView.AllowUserToDeleteRows = false;
			this.propertiesDataGridView.AllowUserToResizeRows = false;
			this.propertiesDataGridView.AutoSizeColumnsMode = System.Windows.Forms.DataGridViewAutoSizeColumnsMode.AllCells;
			this.propertiesDataGridView.AutoSizeRowsMode = System.Windows.Forms.DataGridViewAutoSizeRowsMode.AllCells;
			this.propertiesDataGridView.BackgroundColor = System.Drawing.SystemColors.Window;
			this.propertiesDataGridView.BorderStyle = System.Windows.Forms.BorderStyle.Fixed3D;
			this.propertiesDataGridView.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
			this.propertiesDataGridView.ColumnHeadersVisible = false;
			this.propertiesDataGridView.Dock = System.Windows.Forms.DockStyle.Fill;
			this.propertiesDataGridView.Location = new System.Drawing.Point(0, 21);
			this.propertiesDataGridView.Margin = new System.Windows.Forms.Padding(0);
			this.propertiesDataGridView.MultiSelect = false;
			this.propertiesDataGridView.Name = "propertiesDataGridView";
			this.propertiesDataGridView.ReadOnly = true;
			this.propertiesDataGridView.RowHeadersVisible = false;
			this.propertiesDataGridView.RowTemplate.Height = 24;
			this.propertiesDataGridView.SelectionMode = System.Windows.Forms.DataGridViewSelectionMode.FullRowSelect;
			this.propertiesDataGridView.Size = new System.Drawing.Size(428, 239);
			this.propertiesDataGridView.TabIndex = 7;
			this.propertiesDataGridView.CellContentClick += new System.Windows.Forms.DataGridViewCellEventHandler(this.propertiesDataGridView_CellContentClick);
			this.propertiesDataGridView.CellDoubleClick += new System.Windows.Forms.DataGridViewCellEventHandler(this.dataGridView1_CellDoubleClick);
			this.propertiesDataGridView.DataBindingComplete += new System.Windows.Forms.DataGridViewBindingCompleteEventHandler(this.propertiesDataGridView_DataBindingComplete);
			// 
			// propertiesCaptionPanel
			// 
			this.propertiesCaptionPanel.AutoSize = true;
			this.propertiesCaptionPanel.Controls.Add(this.propertiesLabel);
			this.propertiesCaptionPanel.Controls.Add(this.currentTimeLabel);
			this.propertiesCaptionPanel.Dock = System.Windows.Forms.DockStyle.Top;
			this.propertiesCaptionPanel.Location = new System.Drawing.Point(0, 0);
			this.propertiesCaptionPanel.Margin = new System.Windows.Forms.Padding(0);
			this.propertiesCaptionPanel.Name = "propertiesCaptionPanel";
			this.propertiesCaptionPanel.Padding = new System.Windows.Forms.Padding(2);
			this.propertiesCaptionPanel.Size = new System.Drawing.Size(428, 21);
			this.propertiesCaptionPanel.TabIndex = 8;
			this.propertiesCaptionPanel.WrapContents = false;
			// 
			// propertiesLabel
			// 
			this.propertiesLabel.AutoSize = true;
			this.propertiesLabel.Dock = System.Windows.Forms.DockStyle.Left;
			this.propertiesLabel.Location = new System.Drawing.Point(2, 2);
			this.propertiesLabel.Margin = new System.Windows.Forms.Padding(0, 0, 3, 0);
			this.propertiesLabel.Name = "propertiesLabel";
			this.propertiesLabel.Size = new System.Drawing.Size(74, 17);
			this.propertiesLabel.TabIndex = 3;
			this.propertiesLabel.Text = "Properties:";
			// 
			// currentTimeLabel
			// 
			this.currentTimeLabel.AutoSize = true;
			this.currentTimeLabel.ForeColor = System.Drawing.Color.Gray;
			this.currentTimeLabel.Location = new System.Drawing.Point(82, 2);
			this.currentTimeLabel.Name = "currentTimeLabel";
			this.currentTimeLabel.Size = new System.Drawing.Size(20, 17);
			this.currentTimeLabel.TabIndex = 5;
			this.currentTimeLabel.Text = "at";
			// 
			// selectedObjectStateHistoryControl
			// 
			this.selectedObjectStateHistoryControl.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
			this.selectedObjectStateHistoryControl.Dock = System.Windows.Forms.DockStyle.Fill;
			this.selectedObjectStateHistoryControl.Font = new System.Drawing.Font("Tahoma", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(204)));
			this.selectedObjectStateHistoryControl.Location = new System.Drawing.Point(0, 0);
			this.selectedObjectStateHistoryControl.Margin = new System.Windows.Forms.Padding(4);
			this.selectedObjectStateHistoryControl.MinimumSize = new System.Drawing.Size(200, 100);
			this.selectedObjectStateHistoryControl.Name = "selectedObjectStateHistoryControl";
			this.selectedObjectStateHistoryControl.Size = new System.Drawing.Size(428, 257);
			this.selectedObjectStateHistoryControl.TabIndex = 12;
			// 
			// contextMenuStrip
			// 
			this.contextMenuStrip.ImageScalingSize = new System.Drawing.Size(20, 20);
			this.contextMenuStrip.Name = "contextMenuStrip";
			this.contextMenuStrip.Size = new System.Drawing.Size(182, 32);
			this.contextMenuStrip.Opening += new System.ComponentModel.CancelEventHandler(this.contextMenuStrip_Opening);
			// 
			// StateInspectorForm
			// 
			this.AutoScaleDimensions = new System.Drawing.SizeF(120F, 120F);
			this.ClientSize = new System.Drawing.Size(835, 527);
			this.Controls.Add(this.splitContainer1);
			this.Name = "StateInspectorForm";
			this.Text = "StateInspector";
			this.VisibleChanged += new System.EventHandler(this.CallObjectsForm_VisibleChanged);
			this.splitContainer1.Panel1.ResumeLayout(false);
			this.splitContainer1.Panel1.PerformLayout();
			this.splitContainer1.Panel2.ResumeLayout(false);
			((System.ComponentModel.ISupportInitialize)(this.splitContainer1)).EndInit();
			this.splitContainer1.ResumeLayout(false);
			this.panel2.ResumeLayout(false);
			this.panel2.PerformLayout();
			this.flowLayoutPanel1.ResumeLayout(false);
			this.flowLayoutPanel1.PerformLayout();
			this.splitContainer3.Panel1.ResumeLayout(false);
			this.splitContainer3.Panel1.PerformLayout();
			this.splitContainer3.Panel2.ResumeLayout(false);
			((System.ComponentModel.ISupportInitialize)(this.splitContainer3)).EndInit();
			this.splitContainer3.ResumeLayout(false);
			((System.ComponentModel.ISupportInitialize)(this.propertiesDataGridView)).EndInit();
			this.propertiesCaptionPanel.ResumeLayout(false);
			this.propertiesCaptionPanel.PerformLayout();
			this.ResumeLayout(false);

		}

		#endregion

		private System.Windows.Forms.SplitContainer splitContainer1;
		private System.Windows.Forms.MyTreeView objectsTreeView;
		private System.Windows.Forms.Panel panel2;
		private System.Windows.Forms.FlowLayoutPanel flowLayoutPanel1;
		private System.Windows.Forms.Label aliveObjectsComment;
		private System.Windows.Forms.Label deletedObjectsComment;
		private System.Windows.Forms.Label yetToBeCreatedObjectsComment;
		private System.Windows.Forms.Label label1;
		private System.Windows.Forms.ExtendedSplitContainer splitContainer3;
		private System.Windows.Forms.MyDataGridView propertiesDataGridView;
		private System.Windows.Forms.FlowLayoutPanel propertiesCaptionPanel;
		private System.Windows.Forms.Label propertiesLabel;
		private System.Windows.Forms.Label currentTimeLabel;
		private InspectedObjectEventsHistoryControl selectedObjectStateHistoryControl;
		private System.Windows.Forms.ContextMenuStrip contextMenuStrip;
	}
}