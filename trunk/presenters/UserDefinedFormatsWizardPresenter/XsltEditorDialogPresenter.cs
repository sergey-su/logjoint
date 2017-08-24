using System.Collections.Generic;
using System.Xml;
using System.Text.RegularExpressions;
using System;
using System.Linq;
using System.IO;

namespace LogJoint.UI.Presenters.FormatsWizard.XsltEditorDialog
{
	internal class Presenter : IPresenter, IDisposable, IViewEvents
	{
		readonly IView dialog;
		readonly Help.IPresenter help;
		readonly IAlertPopup alerts;
		readonly ITempFilesManager tempFilesManager;
		readonly IObjectFactory objectsFactory;
		XmlNode currentFormatRoot;
		ISampleLogAccess sampleLogAccess;

		public Presenter(
			IView view, 
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
				"XSL transformation code that normalizes your XML log messages", "Help");
		}

		void IDisposable.Dispose ()
		{
			dialog.Dispose();
		}

		void IViewEvents.OnCancelClicked ()
		{
			dialog.Close();
		}

		void IViewEvents.OnHelpLinkClicked ()
		{
			help.ShowHelp("HowXmlParsingWorks.htm#xslt");
		}

		void IViewEvents.OnOkClicked ()
		{
			if (!SaveTo(currentFormatRoot))
				return;
			
			dialog.Close();
		}

		bool SaveTo(XmlNode formatNode)
		{
			var doc = new XmlDocument();
			try
			{
				doc.LoadXml(StringUtils.NormalizeLinebreakes(dialog.CodeTextBoxValue.Trim()));
			}
			catch (Exception e)
			{
				alerts.ShowPopup("Error", e.Message, AlertFlags.WarningIcon);
				return false;
			}
			var nsMgr = XmlFormat.UserDefinedFormatFactory.NamespaceManager;

			if (doc.SelectSingleNode("xsl:stylesheet", nsMgr) == null)
			{
				alerts.ShowPopup("Error", "The transformation must have xsl:stylesheet on top level", AlertFlags.WarningIcon);
				return false;
			}
			if (doc.SelectSingleNode("xsl:stylesheet/xsl:output[@method='xml']", nsMgr) == null)
			{
				alerts.ShowPopup("Error", "The transformation must output xml. Add <xsl:output method=\"xml\"/>", AlertFlags.WarningIcon);
				return false;
			}

			formatNode.SelectNodes("xml/xsl:stylesheet", nsMgr)
				.OfType<XmlNode>().ToList().ForEach(n => n.ParentNode.RemoveChild(n));

			formatNode.SelectSingleNode("xml")?.AppendChild(
				formatNode.OwnerDocument.ImportNode(doc.DocumentElement, true));			

			return true;
		}

		void IViewEvents.OnTestButtonClicked ()
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
				"xml"
			);
		}

		void IPresenter.ShowDialog (
			XmlNode root,
			ISampleLogAccess sampleLogAccess
		)
		{
			this.currentFormatRoot = root;
			this.sampleLogAccess = sampleLogAccess;

			using (var sw = new StringWriter())
			using (var xw = new XmlTextWriter(sw) { Formatting = Formatting.Indented })
			{
				var nsMgr = XmlFormat.UserDefinedFormatFactory.NamespaceManager;
				root.SelectSingleNode("xml/xsl:stylesheet", nsMgr)?.WriteTo(xw);
				dialog.CodeTextBoxValue = sw.ToString();
			}

			dialog.Show();
		}
	};
};