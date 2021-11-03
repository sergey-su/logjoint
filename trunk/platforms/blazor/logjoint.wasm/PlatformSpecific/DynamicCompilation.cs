using Microsoft.CodeAnalysis;
using Microsoft.JSInterop;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Reflection;
using System.Threading.Tasks;
using LogJoint.FieldsProcessor;

namespace LogJoint.Wasm
{
    class AssemblyLoader : IAssemblyLoader
    {
        Assembly IAssemblyLoader.Load(byte[] image)
        {
            var context = System.Runtime.Loader.AssemblyLoadContext.Default;
            using var ms = new MemoryStream(image);
            return context.LoadFromStream(ms);
        }
    };

    class MetadataReferencesProvider : IMetadataReferencesProvider
    {
        readonly List<MetadataReference> references = new();

        public async Task Init(IJSRuntime jsRuntime)
        {
            var httpClient = new HttpClient();
            async Task<MetadataReference> resolve(string asmName) => MetadataReference.CreateFromStream(
                await httpClient.GetStreamAsync(
                    await jsRuntime.InvokeAsync<string>("logjoint.getResourceUrl", $"_framework/{asmName}")));
            references.AddRange(await Task.WhenAll(
                resolve("System.Runtime.dll"),
                resolve("System.Private.CoreLib.dll"),
                resolve("netstandard.dll"),
                resolve("logjoint.model.dll"),
                resolve("logjoint.model.sdk.dll")
            ));
        }

        IReadOnlyList<MetadataReference> IMetadataReferencesProvider.GetMetadataReferences() => references;
    };
}
