using System;
using System.Collections.Generic;
using System.Text;
using System.Xml;
using System.Windows.Forms;

namespace LogJoint.UI
{
	public class OperationOverExistingFormatScenario : IFormatsWizardScenario
	{
		public OperationOverExistingFormatScenario(IWizardScenarioHost host)
		{
			this.host = host;
			choosePage = new ChooseExistingFormatPage(host);
		}

		public bool Next()
		{
			if (currentScenario != null)
				return currentScenario.Next();

			if (!choosePage.ValidateInput())
				return false;

			if (choosePage.deleteFmtRadioButton.Checked)
			{
				currentScenario = deleteScenario ?? (deleteScenario = new DeleteFormatScenario(host));
				deleteScenario.SetFormat(choosePage.GetSelectedFormat());
			}
			else if (choosePage.changeFmtRadioButton.Checked)
			{
				if (choosePage.GetSelectedFormat() is LogJoint.RegularGrammar.UserDefinedFormatFactory)
				{
					currentScenario = modifyReScenario ?? (modifyReScenario = new ModifyRegexBasedFormatScenario(host));
					modifyReScenario.SetFormat(choosePage.GetSelectedFormat());
				}
			}

			return true;
		}

		public bool Prev()
		{
			if (currentScenario != null)
			{
				if (!currentScenario.Prev())
					currentScenario = null;
				return true;
			}
			else
			{
				return false;
			}

		}

		public Control Current
		{
			get { return currentScenario != null ? currentScenario.Current : choosePage; }
		}
		public WizardScenarioFlag Flags
		{
			get { return currentScenario != null ? currentScenario.Flags : WizardScenarioFlag.NextIsActive | WizardScenarioFlag.BackIsActive; }
		}

		IWizardScenarioHost host;
		ChooseExistingFormatPage choosePage;
		IFormatsWizardScenario currentScenario;
		DeleteFormatScenario deleteScenario;
		ModifyRegexBasedFormatScenario modifyReScenario;
	};

	public class DeleteFormatScenario: IFormatsWizardScenario
	{
		public DeleteFormatScenario(IWizardScenarioHost host)
		{
			this.host = host;
			confirmPage = new FormatDeleteConfirmPage();
		}

		public void SetFormat(IUserDefinedFactory format)
		{
			this.format = format;
			confirmPage.UpdateView(format);
		}

		public bool Next()
		{
			try
			{
				System.IO.File.Delete(format.Location);
			}
			catch (Exception e)
			{
				MessageBox.Show(e.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
				return false;
			}
			host.Finish();
			return false;
		}

		public bool Prev()
		{
			return false;
		}

		public Control Current
		{
			get { return confirmPage; }
		}

		public WizardScenarioFlag Flags
		{
			get { return WizardScenarioFlag.NextIsActive | WizardScenarioFlag.NextIsFinish | WizardScenarioFlag.BackIsActive; }
		}

		IUserDefinedFactory format;
		IWizardScenarioHost host;
		FormatDeleteConfirmPage confirmPage;
	}

	public class ModifyRegexBasedFormatScenario : IFormatsWizardScenario
	{
		public ModifyRegexBasedFormatScenario(IWizardScenarioHost host)
		{
			this.host = host;
			formatDoc = new XmlDocument();
			formatDoc.LoadXml("<format><regular-grammar/></format>");
			regexPage = new RegexBasedFormatPage(host.Help, host.TempFilesManager, host.LogViewerPresenterFactory);
			identityPage = new FormatIdentityPage(host.LogProviderFactoryRegistry, false);
			optionsPage = new FormatAdditionalOptionsPage(host.Help);
			savePage = new SaveFormatPage(host.UserDefinedFormatsManager.Repository, false);
			ResetFormatDocument();
		}

		void ResetFormatDocument()
		{
			regexPage.SetFormatRoot(formatDoc.DocumentElement);
			identityPage.SetFormatRoot(formatDoc.DocumentElement);
			optionsPage.SetFormatRoot(formatDoc.SelectSingleNode("format/regular-grammar"));
			savePage.SetDocument(formatDoc);
		}

		string GetFormatFileNameBasis(IUserDefinedFactory factory)
		{
			string fname = System.IO.Path.GetFileName(factory.Location);

			string suffix = ".format.xml";

			if (fname.EndsWith(suffix, StringComparison.InvariantCultureIgnoreCase))
				fname = fname.Remove(fname.Length - suffix.Length);

			return fname;
		}

		public void SetFormat(IUserDefinedFactory factory)
		{
			formatDoc.Load(factory.Location);
			ResetFormatDocument();
			savePage.FileNameBasis = GetFormatFileNameBasis(factory);
		}

		public bool Next()
		{
			if (stage == 1)
			{
				if (savePage.FileNameBasis == "")
					savePage.FileNameBasis = identityPage.GetDefaultFileNameBasis();
			}
			if (stage == 3)
			{
				host.Finish();
				return false;
			}
			++stage;
			return stage < 3;
		}

		public bool Prev()
		{
			if (stage == 0)
				return false;
			--stage;
			return true;
		}

		public Control Current
		{
			get
			{
				switch (stage)
				{
					case 0:
						return regexPage;
					case 1:
						return identityPage;
					case 2:
						return optionsPage;
					case 3:
						return savePage;
				}
				return null;
			}
		}
		public WizardScenarioFlag Flags
		{
			get 
			{ 
				WizardScenarioFlag f = WizardScenarioFlag.NextIsActive | WizardScenarioFlag.BackIsActive;
				if (stage == 3)
					f |= WizardScenarioFlag.NextIsFinish;
				return f;
			}
		}

		IWizardScenarioHost host;
		int stage;
		XmlDocument formatDoc;
		RegexBasedFormatPage regexPage;
		FormatIdentityPage identityPage;
		FormatAdditionalOptionsPage optionsPage;
		SaveFormatPage savePage;
	};
}
