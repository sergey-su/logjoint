namespace LogJoint.UI.QuickSearchTextBox
{
    partial class QuickSearchTextBox
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
            if (disposing)
            {
                components?.Dispose();
                subscription?.Dispose();
                viewModel?.SetView(null);
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
            components = new System.ComponentModel.Container();
            imageList = new System.Windows.Forms.ImageList(components);
            SuspendLayout();
            // 
            // imageList1
            // 
            imageList.ColorDepth = System.Windows.Forms.ColorDepth.Depth32Bit;
            imageList.ImageSize = new System.Drawing.Size(16, 16);
            imageList.TransparentColor = System.Drawing.Color.Transparent;
            // 
            // QuickSearchTextBox
            // 
            BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            ResumeLayout(false);
        }

        #endregion

        private System.Windows.Forms.ImageList imageList;
    }
}
