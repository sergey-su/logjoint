using System;

using Foundation;
using AppKit;

using LogJoint.UI.Presenters.SearchesManagerDialog;
using System.Collections.Generic;
using System.Linq;

namespace LogJoint.UI
{
	public partial class SearchesManagerDialogController : NSWindowController, IDialogView
	{
		IDialogViewEvents eventsHandler;
		DataSource dataSource = new DataSource();
		Dictionary<ViewControl, NSButton> controls;

		public SearchesManagerDialogController (IntPtr handle) : base (handle)
		{
		}

		[Export ("initWithCoder:")]
		public SearchesManagerDialogController (NSCoder coder) : base (coder)
		{
		}

		public SearchesManagerDialogController (IDialogViewEvents eventsHandler) : base ("SearchesManagerDialog")
		{
			this.eventsHandler = eventsHandler;
		}

		public override void AwakeFromNib ()
		{
			base.AwakeFromNib ();

			controls = new Dictionary<ViewControl, NSButton>()
			{
				{ ViewControl.AddButton, addButton },
				{ ViewControl.DeleteButton, removeButton },
				{ ViewControl.EditButton, propertiesButton },
				{ ViewControl.Export, exportButton },
				{ ViewControl.Import, importButton },
			};

			outlineView.DataSource = dataSource;
			outlineView.Delegate = new Delegate() { owner = this };

			Window.WillClose += (object sender, EventArgs e) =>
			{
				NSApplication.SharedApplication.AbortModal();
			};
		}

		void IDialogView.SetItems (ViewItem [] items)
		{
			Window.GetType();

			dataSource.Items = items.Select(i => new Item()
			{
				PresentationObject = i
			}).ToList();
			outlineView.ReloadData();
		}

		ViewItem [] IDialogView.SelectedItems 
		{
			get 
			{
				return
					UIUtils.GetSelectedItems(outlineView)
					.OfType<Item>()
					.Select(n => n.PresentationObject)
					.ToArray();
			}
			set 
			{
				UIUtils.SelectAndScrollInView(
					outlineView, 
					value
						.Select(i => dataSource.Items.FirstOrDefault(j => j.PresentationObject == i))
						.Where(i => i != null)
						.ToArray(),
					_ => null
				);
			}
		}

		void IDialogView.EnableControl (ViewControl id, bool value)
		{
			Window.GetType();
			NSButton btn;
			if (controls.TryGetValue(id, out btn))
				btn.Enabled = value;
		}

		void IDialogView.OpenModal ()
		{
			NSApplication.SharedApplication.RunModalForWindow(Window);
		}

		void IDialogView.CloseModal ()
		{
			NSApplication.SharedApplication.StopModal();
			Window.Close();
		}

		void IDialogView.SetCloseButtonText(string text)
		{
			closeButton.Title = text;
		}

		public new SearchesManagerDialog Window 
		{
			get { return (SearchesManagerDialog)base.Window; }
		}

		partial void OnCloseClicked (Foundation.NSObject sender)
		{
			eventsHandler.OnCloseClicked();
		}

		partial void OnAddClicked (Foundation.NSObject sender)
		{
			eventsHandler.OnAddClicked();
		}

		partial void OnExportClicked (Foundation.NSObject sender)
		{
			eventsHandler.OnExportClicked();
		}

		partial void OnImportClicked (Foundation.NSObject sender)
		{
			eventsHandler.OnImportClicked();
		}

		partial void OnPropertiesClicked (Foundation.NSObject sender)
		{
			eventsHandler.OnEditClicked();
		}

		partial void OnRemoveClicked (Foundation.NSObject sender)
		{
			eventsHandler.OnDeleteClicked();
		}

		class Item: NSObject
		{
			public ViewItem PresentationObject;
		};

		class DataSource: NSOutlineViewDataSource
		{
			public List<Item> Items = new List<Item>();

			public override nint GetChildrenCount (NSOutlineView outlineView, NSObject item)
			{
				return item == null ? Items.Count : 0;
			}

			public override NSObject GetChild (NSOutlineView outlineView, nint childIndex, NSObject item)
			{
				return item == null ? Items [(int)childIndex] : null;
			}

			public override bool ItemExpandable (NSOutlineView outlineView, NSObject item)
			{
				return false;
			}
		};

		class Delegate: NSOutlineViewDelegate
		{
			public SearchesManagerDialogController owner;

			public override NSView GetView (NSOutlineView outlineView, NSTableColumn tableColumn, NSObject item)
			{
				var presentationItem = (item as Item)?.PresentationObject;

				var view = NSLinkLabel.CreateLabel();
				view.LinkClicked = (sender, e) => 
				{
					if (e.NativeEvent.ClickCount == 2)
						owner.eventsHandler.OnEditClicked();
				};
				view.StringValue = presentationItem?.Caption ?? "";
				return view;
			}

			public override void SelectionDidChange (NSNotification notification)
			{
				owner.eventsHandler.OnSelectionChanged();
			}
		};
	}

	public class SearchesManagerDialogView: IView
	{
		IDialogView IView.CreateDialog (IDialogViewEvents eventsHandler)
		{
			return new SearchesManagerDialogController(eventsHandler);
		}
	};
}
