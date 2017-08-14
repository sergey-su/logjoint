// WARNING
//
// This file has been generated automatically by Visual Studio to store outlets and
// actions made in the UI designer. If it is removed, they will be lost.
// Manual changes to this file may not be handled correctly.
//
using Foundation;
using System.CodeDom.Compiler;

namespace LogJoint.UI
{
	[Register ("FieldsMappingDialogController")]
	partial class FieldsMappingDialogController
	{
		[Outlet]
		LogJoint.UI.NSLinkLabel availableInputFieldsContainer { get; set; }

		[Outlet]
		AppKit.NSTextView codeTextBox { get; set; }

		[Outlet]
		AppKit.NSPopUpButton codeTypeComboxBox { get; set; }

		[Outlet]
		AppKit.NSScroller fieldsLinksHScroller { get; set; }

		[Outlet]
		AppKit.NSTableView fieldsTable { get; set; }

		[Outlet]
		LogJoint.UI.NSLinkLabel helpLinkLabel { get; set; }

		[Outlet]
		AppKit.NSComboBox nameComboBox { get; set; }

		[Outlet]
		AppKit.NSButton removeFieldButton { get; set; }

		[Action ("OnAddFieldClicked:")]
		partial void OnAddFieldClicked (Foundation.NSObject sender);

		[Action ("OnCancelClicked:")]
		partial void OnCancelClicked (Foundation.NSObject sender);

		[Action ("OnCodeTypeChanged:")]
		partial void OnCodeTypeChanged (Foundation.NSObject sender);

		[Action ("OnNameComboxBoxChanged:")]
		partial void OnNameComboxBoxChanged (Foundation.NSObject sender);

		[Action ("OnOkClicked:")]
		partial void OnOkClicked (Foundation.NSObject sender);

		[Action ("OnRemoveFieldClicked:")]
		partial void OnRemoveFieldClicked (Foundation.NSObject sender);

		[Action ("OnTestClicked:")]
		partial void OnTestClicked (Foundation.NSObject sender);
		
		void ReleaseDesignerOutlets ()
		{
			if (availableInputFieldsContainer != null) {
				availableInputFieldsContainer.Dispose ();
				availableInputFieldsContainer = null;
			}

			if (codeTextBox != null) {
				codeTextBox.Dispose ();
				codeTextBox = null;
			}

			if (codeTypeComboxBox != null) {
				codeTypeComboxBox.Dispose ();
				codeTypeComboxBox = null;
			}

			if (fieldsLinksHScroller != null) {
				fieldsLinksHScroller.Dispose ();
				fieldsLinksHScroller = null;
			}

			if (fieldsTable != null) {
				fieldsTable.Dispose ();
				fieldsTable = null;
			}

			if (helpLinkLabel != null) {
				helpLinkLabel.Dispose ();
				helpLinkLabel = null;
			}

			if (nameComboBox != null) {
				nameComboBox.Dispose ();
				nameComboBox = null;
			}

			if (removeFieldButton != null) {
				removeFieldButton.Dispose ();
				removeFieldButton = null;
			}
		}
	}
}
