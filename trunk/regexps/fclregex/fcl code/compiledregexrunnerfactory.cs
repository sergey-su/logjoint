//------------------------------------------------------------------------------
// <copyright file="CompiledRegexRunnerFactory.cs" company="Microsoft">
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

using System.Reflection.Emit;
using System.Diagnostics;
using System.Security.Permissions;

namespace System.Text.RegularExpressions.LogJointVersion {

    
    internal sealed class CompiledRegexRunnerFactory : RegexRunnerFactory {
        DynamicMethod goMethod;
        DynamicMethod findFirstCharMethod;
        DynamicMethod initTrackCountMethod;

        internal CompiledRegexRunnerFactory (DynamicMethod go, DynamicMethod firstChar, DynamicMethod trackCount) {
            this.goMethod = go;
            this.findFirstCharMethod = firstChar;
            this.initTrackCountMethod = trackCount;
            //Debug.Assert(goMethod != null && findFirstCharMethod != null && initTrackCountMethod != null, "can't be null");
        }
        
        protected internal override RegexRunner CreateInstance() {
            CompiledRegexRunner runner = new CompiledRegexRunner();

            new ReflectionPermission(PermissionState.Unrestricted).Assert();
            runner.SetDelegates((NoParamDelegate)       goMethod.CreateDelegate(typeof(NoParamDelegate)),
                                (FindFirstCharDelegate) findFirstCharMethod.CreateDelegate(typeof(FindFirstCharDelegate)),
                                (NoParamDelegate)       initTrackCountMethod.CreateDelegate(typeof(NoParamDelegate)));

            return runner;
        }
    }

    internal delegate RegexRunner CreateInstanceDelegate();
}
