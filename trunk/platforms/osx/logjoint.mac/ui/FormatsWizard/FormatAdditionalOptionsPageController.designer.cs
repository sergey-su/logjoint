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
	[Register ("FormatAdditionalOptionsPageController")]
	partial class FormatAdditionalOptionsPageController
	{
		[Outlet]
		AppKit.NSButton addExtensionButton { get; set; }

		[Outlet]
		AppKit.NSView bufferSizeStepperPlaceholder { get; set; }

		[Outlet]
		AppKit.NSButton enableBufferCheckbox { get; set; }

		[Outlet]
		AppKit.NSPopUpButton encodingCombobox { get; set; }

		[Outlet]
		AppKit.NSTableView extensionsTable { get; set; }

		[Outlet]
		AppKit.NSTextField newExtensionTextBox { get; set; }

		[Outlet]
		AppKit.NSButton removeExtensionButton { get; set; }

		[Action ("OnAddButtonClicked:")]
		partial void OnAddButtonClicked (Foundation.NSObject sender);

		[Action ("OnEnableBufferCheckBoxClicked:")]
		partial void OnEnableBufferCheckBoxClicked (Foundation.NSObject sender);

		[Action ("OnExtensionTextBoxChanged:")]
		partial void OnExtensionTextBoxChanged (Foundation.NSObject sender);

		[Action ("OnRemoveButtonClicked:")]
		partial void OnRemoveButtonClicked (Foundation.NSObject sender);
		
		void ReleaseDesignerOutlets ()
		{
			if (encodingCombobox != null) {
				encodingCombobox.Dispose ();
				encodingCombobox = null;
			}

			if (enableBufferCheckbox != null) {
				enableBufferCheckbox.Dispose ();
				enableBufferCheckbox = null;
			}

			if (removeExtensionButton != null) {
				removeExtensionButton.Dispose ();
				removeExtensionButton = null;
			}

			if (bufferSizeStepperPlaceholder != null) {
				bufferSizeStepperPlaceholder.Dispose ();
				bufferSizeStepperPlaceholder = null;
			}

			if (addExtensionButton != null) {
				addExtensionButton.Dispose ();
				addExtensionButton = null;
			}

			if (extensionsTable != null) {
				extensionsTable.Dispose ();
				extensionsTable = null;
			}

			if (newExtensionTextBox != null) {
				newExtensionTextBox.Dispose ();
				newExtensionTextBox = null;
			}
		}
	}
}
