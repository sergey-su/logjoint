using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;

namespace LogJoint.UI
{
	public partial class ChooseColorDialog : Form
	{
		public ChooseColorDialog(ColorTableBase colorsTable)
		{
			InitializeComponent();

			this.colorsTable = colorsTable;

			EventHandler boxClicked = new EventHandler(boxClick);
			EventHandler boxDblClicked = new EventHandler(boxDoubleClick);
			Size boxSz = new Size(29, 29);
			Size spacing = new Size(4, 4);
			int colCount = containerPanel.Width / (boxSz.Width + spacing.Width);
			int i = 0;
			foreach (Color cl in colorsTable.Items)
			{
				int col = i % colCount;
				int row = i / colCount;
				ColorBox box = new ColorBox(cl);
				boxes.Add(box);
				box.Bounds = new Rectangle(
					col * (boxSz.Width + spacing.Width),
					row * (boxSz.Height + spacing.Height),
					boxSz.Width,
					boxSz.Height
				);
				box.Visible = true;
				box.Parent = containerPanel;
				box.Click += boxClicked;
				box.DoubleClick += boxDblClicked;
				boxes.Add(box);
				++i;
			}
			Height = (colorsTable.Count / colCount + 1) * (boxSz.Height + spacing.Height) + 70;
		}

		void SelectCurrentColor()
		{
			foreach (ColorBox box in boxes)
			{
				box.Selected = box.Color == currentColor;
			}
		}

		public bool Execute(ref Color selectedColor)
		{
			currentColor = selectedColor;
			SelectCurrentColor();
			if (ShowDialog() != DialogResult.OK)
				return false;
			selectedColor = currentColor;
			return true;
		}

		void boxDoubleClick(object sender, EventArgs e)
		{
			DialogResult = DialogResult.OK;
		}

		void boxClick(object sender, EventArgs e)
		{
			foreach (ColorBox box in boxes)
			{
				box.Selected = Object.ReferenceEquals(box, sender);
				if (box.Selected)
					currentColor = box.Color;
			}
		}

		protected override bool ProcessDialogKey(Keys keyData)
		{
			if (keyData == Keys.Enter
			 && this.ActiveControl is ColorBox)
			{
				this.DialogResult = DialogResult.OK;
				return true;
			}
			return base.ProcessDialogKey(keyData);
		}

		private void selectRecommendedLinkLabel_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
		{
			ColorTableBase.ColorTableEntry p = colorsTable.GetNextColor(false);
			currentColor = p.Color;
			SelectCurrentColor();
		}

		readonly ColorTableBase colorsTable;
		readonly List<ColorBox> boxes = new List<ColorBox>();
		Color currentColor;
	}

	public class ColorBox: Control
	{
		public ColorBox(Color color)
		{
			TabStop = true;
			Cursor = Cursors.Hand;
			this.color = color;
			this.colorBrush = new SolidBrush(this.color);
		}

		public Color Color
		{
			get { return color; }
		}

		protected override void Dispose(bool disposing)
		{
			if (disposing)
				colorBrush.Dispose();
			base.Dispose(disposing);
		}

		public bool Selected
		{
			get { return selected; }
			set { selected = value; Invalidate(); }
		}

		protected override void OnPaintBackground(PaintEventArgs pevent)
		{
			Rectangle fillRect = ClientRectangle;
			fillRect.Inflate(-3, -3);
			pevent.Graphics.FillRectangle(colorBrush, fillRect);
			ControlPaint.DrawBorder3D(pevent.Graphics, fillRect,
				Border3DStyle.SunkenOuter);
			pevent.Graphics.ExcludeClip(fillRect);

			pevent.Graphics.FillRectangle(SystemBrushes.Control, ClientRectangle);

			if (Focused || selected)
			{
				Rectangle highlighRect = ClientRectangle;
				highlighRect.Inflate(-1, -1);

				if (selected)
					pevent.Graphics.DrawRectangle(selectedBoxPen, highlighRect);
				else if (Focused)
					ControlPaint.DrawFocusRectangle(pevent.Graphics, highlighRect);
			}
		}

		protected override void OnEnter(EventArgs e)
		{
			Invalidate();
			base.OnEnter(e);
		}

		protected override void OnLeave(EventArgs e)
		{
			Invalidate();
			base.OnLeave(e);
		}

		protected override void OnMouseClick(MouseEventArgs e)
		{
			Focus();
			base.OnMouseClick(e);
		}

		protected override void OnKeyDown(KeyEventArgs e)
		{
			if (e.KeyCode == Keys.Space)
			{
				OnClick(new EventArgs());
			}
			base.OnKeyDown(e);
		}

		bool selected;
		readonly Brush colorBrush;
		readonly Color color;
		static readonly Pen selectedBoxPen = new Pen(Color.DarkBlue, 2);
	};
}