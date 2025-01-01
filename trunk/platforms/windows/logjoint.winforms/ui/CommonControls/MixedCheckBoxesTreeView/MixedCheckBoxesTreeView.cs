using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace System.Windows.Forms
{
    /// <summary>
    /// Represents a node with hidden checkbox
    /// </summary>
    public class HiddenCheckBoxTreeNode : TreeNode
    {
        public HiddenCheckBoxTreeNode() { }
        public HiddenCheckBoxTreeNode(string text) : base(text) { }
        public HiddenCheckBoxTreeNode(string text, TreeNode[] children) : base(text, children) { }
        public HiddenCheckBoxTreeNode(string text, int imageIndex, int selectedImageIndex) : base(text, imageIndex, selectedImageIndex) { }
        public HiddenCheckBoxTreeNode(string text, int imageIndex, int selectedImageIndex, TreeNode[] children) : base(text, imageIndex, selectedImageIndex, children) { }
        protected HiddenCheckBoxTreeNode(SerializationInfo serializationInfo, StreamingContext context) : base(serializationInfo, context) { }
    }

    /// <summary>
    /// From http://dotnetfollower.com/wordpress/2011/05/winforms-treeview-hide-checkbox-of-treenode/
    /// </summary>
    public class MixedCheckBoxesTreeView : TreeView
    {
        IntPtr hidingItemHandle;

        /// <summary>
        /// Specifies the attributes of a node
        /// </summary>
        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
        public struct TV_ITEM
        {
            public int Mask;
            public IntPtr ItemHandle;
            public int State;
            public int StateMask;
            public IntPtr TextPtr;
            public int TextMax;
            public int Image;
            public int SelectedImage;
            public int Children;
            public IntPtr LParam;
        }

        public const int TVIF_STATE = 0x8;
        public const int TVIS_STATEIMAGEMASK = 0xF000;

        public const int TVM_SETITEMA = 0x110d;
        public const int TVM_SETITEM = 0x110d;
        public const int TVM_SETITEMW = 0x113f;

        public const int TVM_GETITEM = 0x110C;

        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        public static extern IntPtr SendMessage(HandleRef hWnd, int msg, int wParam, ref TV_ITEM lParam);

        protected override void WndProc(ref Message m)
        {
            if (m.Msg == 0x0203) // WM_LBUTTONDBLCLK. dblclick is misbehaving with checkboxes.
            {
                m.Result = IntPtr.Zero;
                return;
            }

            base.WndProc(ref m);

            // trap TVM_SETITEM message
            if (m.Msg == TVM_SETITEM || m.Msg == TVM_SETITEMA || m.Msg == TVM_SETITEMW)
                // check if CheckBoxes are turned on
                if (CheckBoxes)
                {
                    // get information about the node
                    TV_ITEM tv_item = (TV_ITEM)m.GetLParam(typeof(TV_ITEM));
                    HideCheckBox(tv_item);
                }
        }

        protected void HideCheckBox(TV_ITEM tv_item)
        {
            if (tv_item.ItemHandle != IntPtr.Zero && tv_item.ItemHandle != hidingItemHandle)
            {
                // get TreeNode-object, that corresponds to TV_ITEM-object
                TreeNode currentTN = TreeNode.FromHandle(this, tv_item.ItemHandle);

                HiddenCheckBoxTreeNode hiddenCheckBoxTreeNode = currentTN as HiddenCheckBoxTreeNode;
                // check if it's HiddenCheckBoxTreeNode and
                // if its checkbox already has been hidden

                if (hiddenCheckBoxTreeNode != null)
                {
                    HandleRef treeHandleRef = new HandleRef(this, Handle);

                    // check if checkbox already has been hidden
                    TV_ITEM currentTvItem = new TV_ITEM();
                    currentTvItem.ItemHandle = tv_item.ItemHandle;
                    currentTvItem.StateMask = TVIS_STATEIMAGEMASK;
                    currentTvItem.State = 0;

                    IntPtr res = SendMessage(treeHandleRef, TVM_GETITEM, 0, ref currentTvItem);
                    bool needToHide = res.ToInt32() > 0 && currentTvItem.State != 0;

                    if (needToHide)
                    {
                        // specify attributes to update
                        TV_ITEM updatedTvItem = new TV_ITEM();
                        updatedTvItem.ItemHandle = tv_item.ItemHandle;
                        updatedTvItem.Mask = TVIF_STATE;
                        updatedTvItem.StateMask = TVIS_STATEIMAGEMASK;
                        updatedTvItem.State = 0;

                        hidingItemHandle = tv_item.ItemHandle;
                        try
                        {
                            // send TVM_SETITEM message
                            SendMessage(treeHandleRef, TVM_SETITEM, 0, ref updatedTvItem);
                        }
                        finally
                        {
                            hidingItemHandle = IntPtr.Zero;
                        }
                    }
                }
            }
        }

        protected override void OnBeforeCheck(TreeViewCancelEventArgs e)
        {
            base.OnBeforeCheck(e);

            // prevent checking/unchecking of HiddenCheckBoxTreeNode,
            // otherwise, we will have to repeat checkbox hiding
            if (e.Node is HiddenCheckBoxTreeNode)
                e.Cancel = true;
        }
    }
}