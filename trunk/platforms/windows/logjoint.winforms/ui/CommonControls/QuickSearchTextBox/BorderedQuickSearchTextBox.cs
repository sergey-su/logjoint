using System;
using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;

namespace LogJoint.UI.QuickSearchTextBox
{
	public partial class BorderedQuickSearchTextBox : UserControl
	{
		QuickSearchTextBox textBox;

		public BorderedQuickSearchTextBox()
		{
			Control container = new ContainerControl()
			{
				Dock = DockStyle.Fill,
				Padding = new Padding(-1)
			};
			textBox = new QuickSearchTextBox()
			{
				BorderStyle = BorderStyle.FixedSingle,
				Location = new Point(-1, -1),
				Anchor = AnchorStyles.Top | AnchorStyles.Bottom |
						 AnchorStyles.Left | AnchorStyles.Right,
				Width = container.Width + 2
			};
			container.Controls.Add(textBox);
			this.Controls.Add(container);

			DefaultBorderColor = Color.DarkGray;
			FocusedBorderColor = Color.Blue;
			BackColor = DefaultBorderColor;
			Padding = new Padding(1);
			Size = textBox.Size;
		}

		[DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
		public Color DefaultBorderColor { get; set; }

		[DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
		public Color FocusedBorderColor { get; set; }

		public QuickSearchTextBox InnerTextBox
		{
			get { return textBox; }
		}

		protected override void OnEnter(EventArgs e)
		{
			BackColor = FocusedBorderColor;
			base.OnEnter(e);
		}

		protected override void OnLeave(EventArgs e)
		{
			BackColor = DefaultBorderColor;
			base.OnLeave(e);
		}

		protected override void SetBoundsCore(int x, int y,
			int width, int height, BoundsSpecified specified)
		{
			base.SetBoundsCore(x, y, width, textBox.PreferredHeight, specified);
		}
	}
}
