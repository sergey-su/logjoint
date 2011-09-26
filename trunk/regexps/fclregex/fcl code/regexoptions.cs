//------------------------------------------------------------------------------
// <copyright file="RegexOptions.cs" company="Microsoft">
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


namespace System.Text.RegularExpressions.LogJointVersion {

using System;

    /// <devdoc>
    ///    <para>[To be supplied.]</para>
    /// </devdoc>
    [Flags]
    public enum RegexOptions {
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        None =                     0x0000,  

        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        IgnoreCase =               0x0001,      // "i"

        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        Multiline =                0x0002,      // "m"
        
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        ExplicitCapture =          0x0004,      // "n"
        
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        Compiled =                 0x0008,      // "c"
        
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        Singleline =               0x0010,      // "s"
        
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        IgnorePatternWhitespace =  0x0020,      // "x"
        
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        RightToLeft =              0x0040,      // "r"

#if DBG
        /// <internalonly/>
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        Debug=                     0x0080,      // "d"
#endif

        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        ECMAScript =                  0x0100,      // "e"

        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        CultureInvariant =                  0x0200,
    }


}

