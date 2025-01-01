using System.Threading.Tasks;
using System.Linq;
using System;
using System.IO;
using System.Collections.Generic;
using LogJoint.Preprocessing;
using HarSharp;
using System.Threading;
using LogJoint.Postprocessing;
using System.Reflection;

namespace LogJoint.Chromium.HttpArchive
{
    public class TextConversionPreprocessingStep : IPreprocessingStep, IUnpackPreprocessingStep
    {
        internal static readonly string stepName = "har.to_text";
        readonly IStepsFactory preprocessingStepsFactory;
        readonly PreprocessingStepParams sourceFile;
        readonly ILogProviderFactory harLogsFactory;

        internal TextConversionPreprocessingStep(
            IStepsFactory preprocessingStepsFactory,
            ILogProviderFactory harLogsFactory,
            PreprocessingStepParams srcFile
        )
        {
            this.preprocessingStepsFactory = preprocessingStepsFactory;
            this.sourceFile = srcFile;
            this.harLogsFactory = harLogsFactory;
        }

        async Task IPreprocessingStep.Execute(IPreprocessingStepCallback callback)
        {
            await ExecuteInternal(callback, p =>
            {
                var cp = ((IFileBasedLogProviderFactory)harLogsFactory).CreateParams(p.Location);
                p.DumpToConnectionParams(cp);
                callback.YieldLogProvider(new YieldedProvider()
                {
                    Factory = harLogsFactory,
                    ConnectionParams = cp,
                    DisplayName = p.DisplayName,
                });
            });
        }

        async Task<PreprocessingStepParams> IPreprocessingStep.ExecuteLoadedStep(IPreprocessingStepCallback callback)
        {
            PreprocessingStepParams ret = null;
            await ExecuteInternal(callback, x => { ret = x; });
            return ret;
        }

        async Task ExecuteInternal(IPreprocessingStepCallback callback, Action<PreprocessingStepParams> onNext)
        {
            await callback.BecomeLongRunning();

            callback.TempFilesCleanupList.Add(sourceFile.Location);

            string tmpFileName = callback.TempFilesManager.GenerateNewName();

            callback.SetStepDescription(string.Format("{0}: converting to text", sourceFile.FullPath));
            var harTask = Task.Run(() =>
            {
                Assembly dependencyResolveHandler(object s, ResolveEventArgs e)
                {
                    if (new AssemblyName(e.Name).Name == "Newtonsoft.Json") // HarConvert needs Newtonsoft.Json v6, map it to whatever modern version shipped with the plugin
                    {
                        return typeof(Newtonsoft.Json.JsonReaderException).Assembly;
                    }
                    return null;
                }
                AppDomain.CurrentDomain.AssemblyResolve += dependencyResolveHandler;
                try
                {
                    return HarConvert.DeserializeFromFile(sourceFile.Location);
                }
                catch (Newtonsoft.Json.JsonReaderException)
                {
                    string fixedJsonFileName = callback.TempFilesManager.GenerateNewName();
                    try
                    {
                        TryFixJson(sourceFile.Location, fixedJsonFileName);
                        return HarConvert.DeserializeFromFile(fixedJsonFileName);
                    }
                    catch (Newtonsoft.Json.JsonReaderException e)
                    {
                        throw new Exception(string.Format("HTTP archive is broken"), e);
                    }
                    finally
                    {
                        if (File.Exists(fixedJsonFileName))
                            File.Delete(fixedJsonFileName);
                    }
                }
                finally
                {
                    AppDomain.CurrentDomain.AssemblyResolve -= dependencyResolveHandler;
                }
            });
            if (await Task.WhenAny(ToTask(callback.Cancellation), harTask) != harTask)
                return;

            await (new Writer()).Write(
                () => new FileStream(tmpFileName, FileMode.Create),
                s => s.Dispose(),
                ToText(harTask.Result, callback.Cancellation)
            );

            onNext(new PreprocessingStepParams(tmpFileName, string.Format("{0}\\text", sourceFile.FullPath),
                sourceFile.PreprocessingHistory.Add(new PreprocessingHistoryItem(stepName)), sourceFile.FullPath));
        }

        static async Task ToTask(CancellationToken cancellation)
        {
            var taskSource = new TaskCompletionSource<int>();
            using (var cancellationRegistration = cancellation.Register(() => taskSource.TrySetResult(1)))
            {
                if (cancellation.IsCancellationRequested)
                    taskSource.TrySetResult(1);
                await taskSource.Task;
            }
        }

        IEnumerableAsync<Message[]> ToText(Har har, CancellationToken cancellation)
        {
            var buffer = new List<Message>();

            int lastEntryId = 0;
            foreach (var e in har.Log.Entries)
            {
                if (e.Request == null || e.Timings == null)
                    continue;
                var entryId = (++lastEntryId).ToString();
                Action<double?, string, string, string> add = (timeOffset, sev, msgType, msg) =>
                {
                    buffer.Add(new Message(buffer.Count, 0, e.StartedDateTime.AddMilliseconds(timeOffset ?? 0),
                        new StringSlice(Message.ENTRY), new StringSlice(entryId), new StringSlice(msgType),
                        new StringSlice(sev ?? Message.INFO), msg));
                };
                Func<string, string> trimValue = s =>
                {
                    var limit = 4000;
                    if (s.Length <= limit)
                        return s;
                    return string.Format("{0}...(trimmed)", s.Substring(0, limit));
                };

                add(0, null, Message.START,
                    string.Format("{0} {1} {2}", e.Request.Method, e.Request.Url, e.Request.HttpVersion));

                foreach (var h in e.Request.Headers)
                    add(0, null, Message.HEADER, string.Format("{0}: {1}", h.Name, trimValue(h.Value)));
                add(0, null, Message.META, string.Format("headersSize: {0}", e.Request.HeadersSize));
                add(0, null, Message.META, string.Format("connection: {0}", e.Connection));
                add(0, null, Message.META, string.Format("serverIPAddress: {0}", e.ServerIPAddress));
                if (e.Request.PostData != null)
                {
                    add(0, null, Message.BODY, e.Request.PostData.Text);
                }

                Predicate<double?> isGoodTimeOffset = v => v != null && v >= 0;

                double lastPhaseOffset = 0;
                Action<double?, string> tryAddStage = (timeOffset, stage) =>
                {
                    if (isGoodTimeOffset(timeOffset))
                    {
                        lastPhaseOffset += timeOffset.Value;
                        add(lastPhaseOffset, null, stage, "");
                    }
                };
                tryAddStage(e.Timings.Blocked, Message.BLOCKED);
                tryAddStage(e.Timings.Dns, Message.DNS);
                if (isGoodTimeOffset(e.Timings.Ssl) && isGoodTimeOffset(e.Timings.Connect))
                {
                    tryAddStage(e.Timings.Ssl, Message.SSL);
                    tryAddStage(e.Timings.Connect.Value - e.Timings.Ssl.Value, Message.CONNECT);
                }
                else
                {
                    tryAddStage(e.Timings.Connect, Message.CONNECT);
                }
                tryAddStage(e.Timings.Send, Message.SEND);
                tryAddStage(e.Timings.Wait, Message.WAIT);

                if (e.Response != null)
                {
                    var rspTime = e.Timings.Receive + lastPhaseOffset;
                    lastPhaseOffset = rspTime;
                    add(
                        rspTime,
                        e.Response.Status >= 200 && e.Response.Status < 400 ? Message.INFO : Message.WARN,
                        Message.RECEIVE, string.Format("{0} {1} {2}", e.Response.HttpVersion, e.Response.Status, e.Response.StatusText)
                    );
                    foreach (var h in e.Response.Headers)
                        add(rspTime, null, Message.HEADER, string.Format("{0}: {1}", h.Name, trimValue(h.Value)));
                    add(rspTime, null, Message.META, string.Format("headersSize: {0}", e.Response.HeadersSize));
                    add(rspTime, null, Message.META, string.Format("bodySize: {0}", e.Response.BodySize));
                    if (e.Response.Content != null && !string.IsNullOrEmpty(e.Response.Content.Text))
                    {
                        add(rspTime, null, Message.BODY, trimValue(e.Response.Content.Text));
                    }
                }
                add(lastPhaseOffset, null, Message.END, "");
            }


            buffer.Sort((m1, m2) =>
            {
                int c = DateTime.Compare(m1.Timestamp, m2.Timestamp);
                if (c != 0)
                    return c;
                return m1.Index - m2.Index;
            });
            return (new[] { buffer.ToArray() }).ToAsync();
        }

        void TryFixJson(string inputFile, string outputFile)
        {
            using (var jsonTextReader = new StreamReader(inputFile))
            using (var jsonReader = new Newtonsoft.Json.JsonTextReader(jsonTextReader))
            using (var jsonTextWriter = new StreamWriter(outputFile))
            using (var jsonWriter = new Newtonsoft.Json.JsonTextWriter(jsonTextWriter)
            {
                AutoCompleteOnClose = true,
                Formatting = Newtonsoft.Json.Formatting.Indented
            })
            {
                try
                {
                    jsonWriter.WriteToken(jsonReader, writeChildren: true);
                }
                catch (Newtonsoft.Json.JsonReaderException)
                {
                }
            }
        }
    };
}
