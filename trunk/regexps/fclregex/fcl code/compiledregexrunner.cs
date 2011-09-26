//------------------------------------------------------------------------------
// <copyright file="CompiledRegexRunner.cs" company="Microsoft">
//     
//      Copyright (c) 2006 Microsoft Corporation.  All rights reserved.
//     
//      The use and distribution terms for this software are contained in the file
//      named license.txt, which can be found in the root of this distribution.
//      By using this software in any fashion, you are agreeing to be bound by the
//      terms of this license.
//     
//      You must not remove this notice, or any other, from this software.
//     
// </copyright>                                                                
//------------------------------------------------------------------------------

using System;
using System.Diagnostics;
using System.Reflection.Emit;

namespace System.Text.RegularExpressions.LogJointVersion {

    internal sealed class CompiledRegexRunner : RegexRunner {
        NoParamDelegate goMethod;
        FindFirstCharDelegate findFirstCharMethod;
        NoParamDelegate initTrackCountMethod;

        internal CompiledRegexRunner() {}

        internal void SetDelegates(NoParamDelegate go, FindFirstCharDelegate firstChar, NoParamDelegate trackCount) {
            goMethod = go;
            findFirstCharMethod = firstChar;
            initTrackCountMethod = trackCount;
        }
        
        protected override void Go() {
            goMethod(this);
        }

        protected override bool FindFirstChar() {
            return findFirstCharMethod(this);
        }

        protected override void InitTrackCount() {
            initTrackCountMethod(this);
        }
    }

    internal delegate void NoParamDelegate(RegexRunner r);
    internal delegate bool FindFirstCharDelegate(RegexRunner r);
    
}
