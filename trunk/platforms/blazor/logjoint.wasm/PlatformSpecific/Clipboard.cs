﻿using LogJoint.UI.Presenters;
using Microsoft.JSInterop;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace LogJoint.Wasm
{
    public class Clipboard : IClipboardAccess
    {
        readonly IJSRuntime jsRuntime;

        public Clipboard(IJSRuntime jsRuntime)
        {
            this.jsRuntime = jsRuntime;
        }

        void IClipboardAccess.SetClipboard(string value)
        {
            WriteText(value);
        }

        void IClipboardAccess.SetClipboard(string plainText, string html)
        {
            WriteTextAndHtml(plainText, html);
        }

        async void WriteText(string text)
        {
            await jsRuntime.InvokeVoidAsync("logjoint.clipboard.setText", text);
        }

        async void WriteTextAndHtml(string text, string html)
        {
            await jsRuntime.InvokeVoidAsync("logjoint.clipboard.setTextAndHtml", text, html);
        }
    }
}
