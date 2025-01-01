﻿using System.Linq;
using LogJoint.Postprocessing;
using System;

namespace LogJoint.Chromium
{
    public interface IPostprocessorsRegistry
    {
        LogSourceMetadata ChromeDebugLog { get; }
        LogSourceMetadata WebRtcInternalsDump { get; }
        LogSourceMetadata ChromeDriver { get; }
        LogSourceMetadata HttpArchive { get; }
    };

    public class PostprocessorsInitializer : IPostprocessorsRegistry
    {
        private readonly IUserDefinedFactory chromeDebugLogFormat, webRtcInternalsDumpFormat, chromeDriverLogFormat, httpArchiveFormat;
        private readonly LogSourceMetadata chromeDebugLogMeta, webRtcInternalsDumpMeta, chromeDriverLogMeta, httpArchiveMeta;


        public PostprocessorsInitializer(
            IManager postprocessorsManager,
            IUserDefinedFormatsManager userDefinedFormatsManager,
            StateInspector.IPostprocessorsFactory stateInspectorPostprocessorsFactory,
            TimeSeries.IPostprocessorsFactory timeSeriesPostprocessorsFactory,
            Correlator.IPostprocessorsFactory correlatorPostprocessorsFactory,
            Timeline.IPostprocessorsFactory timelinePostprocessorsFactory,
            SequenceDiagram.IPostprocessorsFactory sequenceDiagramPostprocessorsFactory
        )
        {
            IUserDefinedFactory findFormat(string company, string formatName)
            {
                var ret = userDefinedFormatsManager.Items.FirstOrDefault(
                    f => f.CompanyName == company && f.FormatName == formatName);
                if (ret == null)
                    throw new Exception(string.Format("Log format {0} is not registered in LogJoint", formatName));
                return ret;
            }

            this.chromeDebugLogFormat = findFormat("Google", "Chrome debug log");
            this.webRtcInternalsDumpFormat = findFormat("Google", "Chrome WebRTC internals dump as log");
            this.chromeDriverLogFormat = findFormat("Google", "chromedriver");
            this.httpArchiveFormat = findFormat("W3C", "HTTP Archive (HAR)");

            this.chromeDebugLogMeta = new LogSourceMetadata(
                chromeDebugLogFormat,
                stateInspectorPostprocessorsFactory.CreateChromeDebugPostprocessor(),
                timeSeriesPostprocessorsFactory.CreateChromeDebugPostprocessor(),
                timelinePostprocessorsFactory.CreateChromeDebugPostprocessor(),
                sequenceDiagramPostprocessorsFactory.CreateChromeDebugPostprocessor(),
                correlatorPostprocessorsFactory.CreateChromeDebugPostprocessor()
            );
            postprocessorsManager.Register(this.chromeDebugLogMeta);

            this.webRtcInternalsDumpMeta = new LogSourceMetadata(
                webRtcInternalsDumpFormat,
                stateInspectorPostprocessorsFactory.CreateWebRtcInternalsDumpPostprocessor(),
                timeSeriesPostprocessorsFactory.CreateWebRtcInternalsDumpPostprocessor(),
                correlatorPostprocessorsFactory.CreateWebRtcInternalsPostprocessor()
            );
            postprocessorsManager.Register(this.webRtcInternalsDumpMeta);

            this.chromeDriverLogMeta = new LogSourceMetadata(
                chromeDriverLogFormat,
                timelinePostprocessorsFactory.CreateChromeDriverPostprocessor(),
                correlatorPostprocessorsFactory.CreateChromeDriverPostprocessor()
            );
            postprocessorsManager.Register(this.chromeDriverLogMeta);

            this.httpArchiveMeta = new LogSourceMetadata(
                httpArchiveFormat,
                timelinePostprocessorsFactory.CreateHttpArchivePostprocessor(),
                sequenceDiagramPostprocessorsFactory.CreateHttpArchivePostprocessor()
            );
            postprocessorsManager.Register(this.httpArchiveMeta);
        }

        LogSourceMetadata IPostprocessorsRegistry.ChromeDebugLog
        {
            get { return chromeDebugLogMeta; }
        }

        LogSourceMetadata IPostprocessorsRegistry.WebRtcInternalsDump
        {
            get { return webRtcInternalsDumpMeta; }
        }

        LogSourceMetadata IPostprocessorsRegistry.ChromeDriver
        {
            get { return chromeDriverLogMeta; }
        }

        LogSourceMetadata IPostprocessorsRegistry.HttpArchive
        {
            get { return httpArchiveMeta; }
        }
    };
}
