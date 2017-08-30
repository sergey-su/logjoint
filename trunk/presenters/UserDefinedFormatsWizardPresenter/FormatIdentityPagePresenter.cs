using System.Xml;

namespace LogJoint.UI.Presenters.FormatsWizard.FormatIdentityPage
{
	internal class Presenter : IPresenter, IViewEvents
	{
		readonly IView view;
		readonly IWizardScenarioHost host;
		readonly IAlertPopup alerts;

		XmlNode formatRoot;
		readonly bool newFormatMode;
		readonly ILogProviderFactoryRegistry registry;

		public Presenter(
			IView view, 
			IWizardScenarioHost host,
			IAlertPopup alerts,
			ILogProviderFactoryRegistry registry,
			bool newFormatMode
		)
		{
			this.view = view;
			this.view.SetEventsHandler(this);
			this.host = host;
			this.alerts = alerts;
			this.newFormatMode = newFormatMode;
			this.registry = registry;
			this.view[ControlId.HeaderLabel] = newFormatMode ? "New format properties:" : "Format properties";
		}

		bool IWizardPagePresenter.ExitPage(bool movingForward)
		{
			if (movingForward && !ValidateInput())
				return false;

			XmlElement idNode = formatRoot.SelectSingleNode("id") as XmlElement;
			if (idNode == null)
				idNode = formatRoot.AppendChild(formatRoot.OwnerDocument.CreateElement("id")) as XmlElement;
			idNode.SetAttribute("company", view[ControlId.CompanyNameEdit]);
			idNode.SetAttribute("name", view[ControlId.FormatNameEdit]);

			XmlNode descNode = formatRoot.SelectSingleNode("description");
			if (descNode == null)
				descNode = formatRoot.AppendChild(formatRoot.OwnerDocument.CreateElement("description"));
			descNode.InnerText = view[ControlId.DescriptionEdit];

			return true;
		}

		object IWizardPagePresenter.ViewObject => view;

		bool ValidateInput()
		{
			string msg = null;
			if (view[ControlId.FormatNameEdit] == "")
			{
				msg = "Format name is mandatory";
				view.SetFocus(ControlId.FormatNameEdit);
			}
			if (newFormatMode && registry.Find(view[ControlId.CompanyNameEdit], view[ControlId.FormatNameEdit]) != null)
			{
				msg = "Format with this company name/format name combination already exists";
				view.SetFocus(ControlId.FormatNameEdit);
			}
			if (msg != null)
			{
				alerts.ShowPopup("Validation", msg, AlertFlags.Ok | AlertFlags.WarningIcon);
				return false;
			}
			return true;
		}

		string IPresenter.GetDefaultFileNameBasis()
		{
			if (view[ControlId.CompanyNameEdit].Length > 0)
				return view[ControlId.CompanyNameEdit] + " - " + view[ControlId.FormatNameEdit];
			return view[ControlId.FormatNameEdit];
		}

		void IPresenter.SetFormatRoot(XmlNode formatRoot)
		{
			this.formatRoot = formatRoot;
			XmlNode n;
			n = formatRoot.SelectSingleNode("id/@company");
			if (n != null)
				view[ControlId.CompanyNameEdit] = n.Value;

			n = formatRoot.SelectSingleNode("id/@name");
			if (n != null)
				view[ControlId.FormatNameEdit] = n.Value;

			n = formatRoot.SelectSingleNode("description");
			if (n != null)
				view[ControlId.DescriptionEdit] = StringUtils.NormalizeLinebreakes(n.InnerText);
		}
	};
};