using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using System;
using System.IO;
using System.Linq;
using Microsoft.Extensions.DependencyInjection;
using System.Text;

namespace LogJoint.Wasm
{
    public class ChromeExtensionIntegration
    {
        public static void Init(JsInterop jsInterop, WebAssemblyHost wasmHost)
        {
            jsInterop.ChromeExtension.OnOpen += async (sender, evt) =>
            {
                Console.WriteLine("Opening blob id: '{0}', displayName: '{1}'", evt.Id, evt.DisplayName);
                var model = wasmHost.Services.GetService<ModelObjects>();
                using var stream = new MemoryStream();
                using (var writer = new StreamWriter(stream, Encoding.ASCII, 1024, leaveOpen: true))
                    writer.Write(evt.LogText);
                stream.Position = 0;
                await model.ExpensibilityEntryPoint.WebContentCache.SetValue(new Uri(evt.Url), stream);
                var task = model.LogSourcesPreprocessings.Preprocess(
                    new[] { model.PreprocessingStepsFactory.CreateLocationTypeDetectionStep(
                        new LogJoint.Preprocessing.PreprocessingStepParams(evt.Url, displayName: evt.DisplayName)) },
                    "Processing file"
                );
                await task;
            };

            jsInterop.ChromeExtension.OnAddSource = async (evt) =>
            {
                Console.WriteLine("Adding source id: '{0}'", evt.Url);
                var model = wasmHost.Services.GetService<ModelObjects>();
                Preprocessing.IPreprocessingStep step =
                    model.ExpensibilityEntryPoint.Preprocessing.StepsFactory.CreateURLTypeDetectionStep(
                        new LogJoint.Preprocessing.PreprocessingStepParams(evt.Url)
                    );
                await model.LogSourcesPreprocessings.Preprocess(new[] { step }, "Processing");
            };
        }
    }
}
