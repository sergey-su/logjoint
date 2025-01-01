using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Drawing.Drawing2D;
using System.Threading;

namespace LogJoint.UI
{
    public partial class InfoPopupControl : UserControl
    {
        public InfoPopupControl()
        {
            InitializeComponent();
            Visible = false;
        }

        public enum State
        {
            Showing,
            Shown,
            Hiding,
            Hidden
        };

        public State PopupState { get { return state; } }

        public class MessagePart
        {
            public readonly string Text;
            public MessagePart(string text) { Text = text; }
        };
        public class Link : MessagePart
        {
            public readonly Action Click;
            public Link(string text, Action click) : base(text) { Click = click; }
        };

        public void ShowPopup(string caption, string txt, Point location)
        {
            ShowPopup(caption, Enumerable.Repeat(new MessagePart(txt), 1), location);
        }

        public void ShowPopup(string caption, IEnumerable<MessagePart> parts, Point location)
        {
            StringBuilder text = new StringBuilder();
            contentLinkLabel.Links.Clear();
            foreach (var part in parts)
            {
                if (text.Length > 0)
                    text.Append(' ');
                Link link = part as Link;
                if (link != null)
                    contentLinkLabel.Links.Add(text.Length, link.Text.Length, link.Click);
                text.Append(part.Text);
            }
            captionLabel.Text = caption;
            contentLinkLabel.Text = text.ToString();
            Location = new Point(location.X - Width, location.Y - Height);

            switch (state)
            {
                case State.Hidden:
                    SetState(State.Showing);
                    SetCurrentAnimationStep(1);
                    Visible = true;
                    UpdateAnimationTimer();
                    break;
                case State.Hiding:
                case State.Showing:
                case State.Shown:
                    SetState(State.Showing);
                    SetCurrentAnimationStep(1);
                    UpdateAnimationTimer();
                    break;
            }
        }

        public void HidePopup()
        {
            switch (state)
            {
                case State.Showing:
                case State.Shown:
                    SetState(State.Hiding);
                    UpdateAnimationTimer();
                    break;
            }
        }

        void UpdateAnimationTimer()
        {
            animationTimer.Enabled = state == State.Hiding || state == State.Showing;
        }

        static GraphicsPath RoundRect(RectangleF rectangle, float roundRadius)
        {
            RectangleF innerRect = RectangleF.Inflate(rectangle, -roundRadius, -roundRadius);
            GraphicsPath path = new GraphicsPath();
            path.StartFigure();
            path.AddArc(RoundBounds(innerRect.Right - 1, innerRect.Bottom - 1, roundRadius), 0, 90);
            path.AddArc(RoundBounds(innerRect.Left, innerRect.Bottom - 1, roundRadius), 90, 90);
            path.AddArc(RoundBounds(innerRect.Left, innerRect.Top, roundRadius), 180, 90);
            path.AddArc(RoundBounds(innerRect.Right - 1, innerRect.Top, roundRadius), 270, 90);
            path.CloseFigure();
            return path;
        }

        private static RectangleF RoundBounds(float x, float y, float rounding)
        {
            return new RectangleF(x - rounding, y - rounding, 2 * rounding, 2 * rounding);
        }

        void UpdateRegion()
        {
            //using (var regionPath = RoundRect(ClientRectangle, internalPadding))
            //	this.Region = new System.Drawing.Region(regionPath);
        }

        private void InfoPopupForm_Resize(object sender, EventArgs e)
        {
            UpdateRegion();
        }

        private void linkLabel1_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            var action = e.Link.LinkData as Action;
            if (action != null)
                action();
        }

        private void animationTimer_Tick(object sender, EventArgs e)
        {
            switch (state)
            {
                case State.Showing:
                    if (animationStep == totalAnimationSteps)
                        SetState(State.Shown);
                    else
                        SetCurrentAnimationStep(animationStep + 1);
                    break;
                case State.Hiding:
                    if (animationStep == 1)
                    {
                        SetState(State.Hidden);
                        Visible = false;
                        contentLinkLabel.Links.Clear(); // release references to links' click handlers
                    }
                    else
                    {
                        SetCurrentAnimationStep(animationStep - 1);
                    }
                    break;
            }
            UpdateAnimationTimer();
        }

        private void InfoPopupControl_Load(object sender, EventArgs e)
        {
            UpdateRegion();
            UpdateBounds();
        }

        new void UpdateBounds()
        {
            int newWidth = containerPanel.Width + internalPadding * 2;
            int newHeight = containerPanel.Height * animationStep / totalAnimationSteps + internalPadding * 2;
            SetBounds(Bounds.Right - newWidth, Bounds.Bottom - newHeight, newWidth, newHeight);
        }

        void SetCurrentAnimationStep(int i)
        {
            animationStep = i;
            UpdateBounds();
        }

        void SetState(State newState)
        {
            state = newState;
        }

        State state = State.Hidden;
        const int totalAnimationSteps = 4;
        const int internalPadding = 9;
        int animationStep = totalAnimationSteps;
    }
}
