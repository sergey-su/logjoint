using System;
using System.Collections.Generic;
using System.Text;
using LogJoint;
using LogJoint.SkypeFormats;
using LogJoint.SkypeFormats.Call;

namespace LogJoint
{
	public class Plugin : PluginBase
	{
		public Plugin()
		{
			tabPage = new SkypeFormats.TabPage(this);
			parsedObjectsForm = new LoggedObjectsForm(this);
		}

		public ILogJointApplication Application
		{
			get { return application; }
		}

		internal LogSourceToParserOutputMapping LogSourcesMap
		{
			get { return logSourcesMap; }
		}

		internal LoggedObjectsForm ParsedObjectsForm
		{
			get { return parsedObjectsForm; }
		}

		public override void Init(ILogJointApplication app)
		{
			application = app;
			logSourcesMap = new LogSourceToParserOutputMapping(app.Model);
			app.RegisterToolForm(parsedObjectsForm);

			app.FocusedMessageChanged += delegate (object sender, EventArgs e)
			{
				parsedObjectsForm.RefreshAliveObjects();
			};

			app.SourcesChanged += delegate (object sender, EventArgs e)
			{
				LogSourcesMap.Refresh();
			};

			logSourcesMap.OnChanged += delegate(object sender, EventArgs e)
			{
				tabPage.RefreshView();
				ParsedObjectsForm.RefreshView();
			};
		}

		public override void Dispose()
		{
			parsedObjectsForm.Dispose();
			tabPage.Dispose();
		}

		public override IEnumerable<IMainFormTagExtension> MainFormTagExtensions
		{
			get
			{
				yield return tabPage;
			}
		}

		ILogJointApplication application;
		TabPage tabPage;
		LogSourceToParserOutputMapping logSourcesMap;
		LoggedObjectsForm parsedObjectsForm;
	}
}
