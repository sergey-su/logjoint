//------------------------------------------------------------------------------
// <copyright file="InvariantComparer.cs" company="Microsoft">
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

namespace System {
    using System;
    using System.Collections;
    using System.Globalization;
 
    [Serializable]
    internal class InvariantComparer : IComparer {
        private CompareInfo m_compareInfo;
        internal static readonly InvariantComparer Default = new InvariantComparer();
        
        internal InvariantComparer() {
            m_compareInfo = CultureInfo.InvariantCulture.CompareInfo;
        }
  
        public int Compare(Object a, Object b) {
            String sa = a as String;
            String sb = b as String;
            if (sa != null && sb != null)
                return m_compareInfo.Compare(sa, sb);
            else
                return Comparer.Default.Compare(a,b);
        }
    }
}

