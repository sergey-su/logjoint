//------------------------------------------------------------------------------
// <copyright file="RegexTree.cs" company="Microsoft">
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

// RegexTree is just a wrapper for a node tree with some
// global information attached.

namespace System.Text.RegularExpressions.LogJointVersion {

    using System.Collections;

    internal sealed class RegexTree {
        internal RegexTree(RegexNode root, Hashtable caps, Object[] capnumlist, int captop, Hashtable capnames, String[] capslist, RegexOptions opts) {
            _root = root;
            _caps = caps;
            _capnumlist = capnumlist;
            _capnames = capnames;
            _capslist = capslist;
            _captop = captop;
            _options = opts;
        }

        internal RegexNode _root;
        internal Hashtable _caps;
        internal Object[]  _capnumlist;
        internal Hashtable _capnames;
        internal String[]  _capslist;
        internal RegexOptions _options;
        internal int       _captop;

#if DBG
        internal void Dump() {
            _root.Dump();
        }

        internal bool Debug {
            get {
                return(_options & RegexOptions.Debug) != 0;
            }
        }
#endif
    }
}
