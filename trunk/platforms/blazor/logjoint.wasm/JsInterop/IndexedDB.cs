using Microsoft.JSInterop;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace LogJoint.Wasm
{
    public class IndexedDB
    {
        readonly IJSRuntime jsRuntime;

        public IndexedDB(IJSRuntime jsRuntime)
        {
            this.jsRuntime = jsRuntime;
        }

        public ValueTask<T> Get<T>(string storeName, string key)
        {
            return jsRuntime.InvokeAsync<T>("logjoint.db.get", storeName, key);
        }

        public ValueTask Set(string storeName, object value, string key)
        {
            return jsRuntime.InvokeVoidAsync("logjoint.db.set", storeName, value, key);
        }

        public ValueTask<string[]> Keys(string storeName, string prefix = null)
        {
            return jsRuntime.InvokeAsync<string[]>("logjoint.db.keys", storeName, prefix);
        }

        public ValueTask<long> EstimateSize(string storeName)
        {
            return jsRuntime.InvokeAsync<long>("logjoint.db.estimateSize", storeName);
        }

        public ValueTask Delete(string storeName, string key)
        {
            return jsRuntime.InvokeVoidAsync("logjoint.db.delete", storeName, key);
        }

        public ValueTask DeleteByPrefix(string storeName, string prefix)
        {
            return jsRuntime.InvokeVoidAsync("logjoint.db.deleteByPrefix", storeName, prefix);
        }
    }
}
