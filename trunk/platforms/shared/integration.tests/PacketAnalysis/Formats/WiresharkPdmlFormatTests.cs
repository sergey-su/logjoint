﻿using NSubstitute;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using PA = LogJoint.PacketAnalysis;
using LogJoint.UI.Presenters.Postprocessing.MainWindowTabPage;
using LogJoint.Extensibility;

namespace LogJoint.Tests.Integration.PacketAnalysis
{
	class Tests<T>: UndiscoverableByNUnit<T>
	{
		readonly IPluginManifest manifest;
		readonly SamplesUtils samples = new SamplesUtils();
		TestAppInstance app;

		PA.UI.Presenters.Factory.IViewsFactory viewsFactory;
		PA.UI.Presenters.MessagePropertiesDialog.IViewModel messagePropertiesViewModel;
		object messagePropertiesOSView;

		public Tests(IPluginManifest manifest)
		{
			this.manifest = manifest;
		}

		public async Task Before()
		{
			viewsFactory = Substitute.For<PA.UI.Presenters.Factory.IViewsFactory>();
			viewsFactory.CreateMessageContentView().SetViewModel(
				Arg.Do<PA.UI.Presenters.MessagePropertiesDialog.IViewModel>(x => messagePropertiesViewModel = x));
			messagePropertiesOSView = new object();
			viewsFactory.CreateMessageContentView().OSView.Returns(messagePropertiesOSView);

			app = await TestAppInstance.Create();
			app.Model.PluginFormatsManager.RegisterPluginFormats(manifest);
			PA.UI.Presenters.Factory.Create(
				PA.Factory.Create(app.Model.ExpensibilityEntryPoint),
				app.Presentation.ExpensibilityEntryPoint,
				app.Model.ExpensibilityEntryPoint,
				viewsFactory
			);

			await app.EmulateFileDragAndDrop(await samples.GetSampleAsLocalFile("network_trace_with_keys_1.tar.gz"));
			await app.WaitFor(() => !app.ViewModel.LoadedMessagesLogViewer.ViewLines.IsEmpty);
		}

		public async Task After()
		{
			await app.Dispose();
		}

		public async Task FormatIsDetectedAndLoaded()
		{
			await app.SynchronizationContext.Invoke(() =>
			{
				Assert.AreEqual("            Expert Info (Warning/Sequence): Connection reset (RST)", app.ViewModel.LoadedMessagesLogViewer.ViewLines[4].TextLineValue);
			});
		}

		public async Task PostprocessorsEnabled()
		{
			await app.SynchronizationContext.Invoke(() =>
			{
				app.ViewModel.MainForm.OnTabChanging(app.ViewModel.PostprocessingTabPageId);
				var postprocessorsControls = app.ViewModel.PostprocessingTabPage.ControlsState;
				Assert.IsFalse(postprocessorsControls[ViewControlId.Timeline].Disabled);
			});
		}

		public async Task MessagePropertiesDialogExtensionIsRegistered()
		{
			await app.SynchronizationContext.Invoke(() =>
			{
				var lv = app.ViewModel.LoadedMessagesLogViewer;
				lv.OnMessageMouseEvent(lv.ViewLines[0], 0, UI.Presenters.LogViewer.MessageMouseEventFlag.None, new object());

				var menu = lv.OnMenuOpening();
				Assert.IsTrue((menu.VisibleItems & UI.Presenters.LogViewer.ContextMenuItem.DefaultAction) != 0);
				lv.OnMenuItemClicked(UI.Presenters.LogViewer.ContextMenuItem.DefaultAction);

				var dlg = app.ViewModel.MessagePropertiesDialog;

				Assert.AreEqual(3, dlg.Data.ContentViewModes.Count);
				Assert.AreEqual("Packet protocols", dlg.Data.ContentViewModes[2]);

				dlg.OnContentViewModeChange(2);
				Assert.AreEqual(2, dlg.Data.ContentViewModeIndex);
				Assert.IsNotNull(dlg.Data.CustomView);
				Assert.AreSame(messagePropertiesOSView, dlg.Data.CustomView);

				Assert.AreEqual(4, messagePropertiesViewModel.Root.Children.Count);
			});
		}
	};

	[TestFixture]
	class WiresharkPdmlFormatTests
	{
		readonly PluginLoader pluginLoader = new PluginLoader();
		Tests<int> tests;

		[OneTimeSetUp]
		public async Task Before()
		{
			tests = new Tests<int>(pluginLoader.Manifest);
			await tests.Before();
		}

		[OneTimeTearDown]
		public Task After() => tests.After();

		[Test]
		public Task FormatIsDetectedAndLoaded() => tests.FormatIsDetectedAndLoaded();

		[Test]
		public Task PostprocessorsEnabled() => tests.PostprocessorsEnabled();

		[Test]
		public Task MessagePropertiesDialogExtensionIsRegistered() => tests.MessagePropertiesDialogExtensionIsRegistered();
	}
}