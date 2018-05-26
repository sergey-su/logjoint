using System.Collections.Generic;
using System.Xml;
using System;
using System.Linq;
using System.IO;
using JUST;
using Newtonsoft.Json.Linq;
using ICCEViewEvents = LogJoint.UI.Presenters.FormatsWizard.CustomCodeEditorDialog.IViewEvents;
using ICCEView = LogJoint.UI.Presenters.FormatsWizard.CustomCodeEditorDialog.IView;

namespace LogJoint.UI.Presenters.FormatsWizard.JUSTEditorDialog
{
	internal class Presenter : IPresenter, IDisposable, ICCEViewEvents
	{
		readonly ICCEView dialog;
		readonly Help.IPresenter help;
		readonly IAlertPopup alerts;
		readonly ITempFilesManager tempFilesManager;
		readonly IObjectFactory objectsFactory;
		XmlNode currentFormatRoot;
		ISampleLogAccess sampleLogAccess;

		public Presenter(
			ICCEView view, 
			Help.IPresenter help, 
			IAlertPopup alerts,
			ITempFilesManager tempFilesManager,
			IObjectFactory objectFactory
		)
		{
			this.dialog = view;
			this.dialog.SetEventsHandler(this);
			this.help = help;
			this.alerts = alerts;
			this.objectsFactory = objectFactory;
			this.tempFilesManager = tempFilesManager;
			this.dialog.InitStaticControls(
				"JUST editor", "JUST transformation code that normalizes your JSON log messages", "Help");
		}

		void IDisposable.Dispose ()
		{
			dialog.Dispose();
		}

		void ICCEViewEvents.OnCancelClicked ()
		{
			dialog.Close();
		}

		void ICCEViewEvents.OnHelpLinkClicked ()
		{
			help.ShowHelp("HowJsonParsingWorks.htm#JUST");
		}

		void ICCEViewEvents.OnOkClicked ()
		{
			if (!SaveTo(currentFormatRoot))
				return;
			
			dialog.Close();
		}

		bool SaveTo(XmlNode formatNode)
		{
			var code = dialog.CodeTextBoxValue.Trim();

			try
			{
				JObject.Parse(code); // validate json syntax
			}
			catch (Exception e)
			{
				alerts.ShowPopup("Error", e.Message, AlertFlags.WarningIcon);
				return false;
			}

			formatNode.SelectNodes("json/transform")
				.OfType<XmlNode>().ToList().ForEach(n => n.ParentNode.RemoveChild(n));

			var transform = formatNode.OwnerDocument.CreateElement("transform");
			transform.AppendChild(formatNode.OwnerDocument.CreateCDataSection(code));
			formatNode.SelectSingleNode("json")?.AppendChild(transform);

			return true;
		}

		void ICCEViewEvents.OnTestButtonClicked ()
		{
			var tmpDoc = new XmlDocument();
			var tmpRoot = tmpDoc.ImportNode(currentFormatRoot, true);
			tmpDoc.AppendChild(tmpRoot);
			if (!SaveTo(tmpRoot))
				return;
			CustomFormatPageUtils.TestParsing(
				sampleLogAccess.SampleLog,
				alerts,
				tempFilesManager,
				objectsFactory,
				tmpRoot,
				"json"
			);
		}

		void IPresenter.ShowDialog (
			XmlNode root,
			ISampleLogAccess sampleLogAccess
		)
		{
			this.currentFormatRoot = root;
			this.sampleLogAccess = sampleLogAccess;

			dialog.CodeTextBoxValue = root.SelectSingleNode("json/transform")?.InnerText ?? "";
			dialog.Show();
		}
	};
};