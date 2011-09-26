//------------------------------------------------------------------------------
// <copyright file="RegexGroupCollection.cs" company="Microsoft">
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

// The GroupCollection lists the captured Capture numbers
// contained in a compiled Regex.

namespace System.Text.RegularExpressions.LogJointVersion {

    using System.Collections;

    /// <devdoc>
    ///    <para>
    ///       Represents a sequence of capture substrings. The object is used
    ///       to return the set of captures done by a single capturing group.
    ///    </para>
    /// </devdoc>
    [ Serializable() ]
    public class GroupCollection : ICollection {
        internal readonly Match _match;
        internal readonly Hashtable _captureMap;

        // cache of Group objects fed to the user
        internal Group[]             _groups;
        internal bool _groupsAreValid;

        /*
         * Nonpublic constructor
         */
        internal GroupCollection(Match match, Hashtable caps) {
            _match = match;
            _captureMap = caps;
        }

        /*
         * The object on which to synchronize
         */
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        public Object SyncRoot {
            get {
                return _match;
            }
        }

        /*
         * ICollection
         */
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        public bool IsSynchronized {
            get {
                return false;
            }
        }

        /*
         * ICollection
         */
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        public bool IsReadOnly {
            get {
                return true;
            }
        }

        /// <devdoc>
        ///    <para>
        ///       Returns the number of groups.
        ///    </para>
        /// </devdoc>
        public int Count {
            get {
                return _match._matchcount.Length;
            }
        }

        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        public Group this[int groupnum]
        {
            get {
                return GetGroup(groupnum);
            }
        }

        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        public Group this[String groupname] {
            get {
                if (_match._regex == null)
                    return Group._emptygroup;

                return GetGroup(_match._regex.GroupNumberFromName(groupname));
            }
        }

        internal Group GetGroup(int groupnum) {
            if (_captureMap != null) {
                Object o;

                o = _captureMap[groupnum];
                if (o == null)
                    return Group._emptygroup;
                    //throw new ArgumentOutOfRangeException("groupnum");

                return GetGroupImpl((int)o);
            }
            else {
                //if (groupnum >= _match._regex.CapSize || groupnum < 0)
                //   throw new ArgumentOutOfRangeException("groupnum");
                if (groupnum >= _match._matchcount.Length || groupnum < 0)
                    return Group._emptygroup;

                return GetGroupImpl(groupnum);
            }
        }


        /*
         * Caches the group objects
         */
        internal Group GetGroupImpl(int groupnum) {
            if (groupnum == 0)
                return _match;

            // Construct all the Group objects the first time GetGroup is called

            if (_groups == null) {
                _groups = new Group[_match._matchcount.Length - 1];
                for (int i = 0; i < _groups.Length; i++) {
                    _groups[i] = new Group(_match._text, _match._matches[i + 1], _match._matchcount[i + 1]);
                }
                _groupsAreValid = true;
            }
            if (!_groupsAreValid) {
                for (int i = 0; i < _groups.Length; i++) {
                    var g = _groups[i];
                    g._text = _match._text;
                    g._caps = _match._matches[i + 1];
                    g._capcount = _match._matchcount[i + 1];
                    g._index = g._capcount == 0 ? 0 : g._caps[(g._capcount - 1) * 2];
                    g._length = g._capcount == 0 ? 0 : g._caps[(g._capcount * 2) - 1];
                    if (g._capcoll != null)
                        g._capcoll._capturesAreValid = false;
                }
            }

            return _groups[groupnum - 1];
        }

        /*
         * As required by ICollection
         */
        /// <devdoc>
        ///    <para>
        ///       Copies all the elements of the collection to the given array
        ///       beginning at the given index.
        ///    </para>
        /// </devdoc>
        public void CopyTo(Array array, int arrayIndex) {
            if (array == null)
                throw new ArgumentNullException("array");

            for (int i = arrayIndex, j = 0; j < Count; i++, j++) {
                array.SetValue(this[j], i);
            }
        }

        /*
         * As required by ICollection
         */
        /// <devdoc>
        ///    <para>
        ///       Provides an enumerator in the same order as Item[].
        ///    </para>
        /// </devdoc>
        public IEnumerator GetEnumerator() {
            return new GroupEnumerator(this);
        }
    }


    /*
     * This non-public enumerator lists all the captures
     * Should it be public?
     */
    internal class GroupEnumerator : IEnumerator {
        internal GroupCollection _rgc;
        internal int _curindex;

        /*
         * Nonpublic constructor
         */
        internal GroupEnumerator(GroupCollection rgc) {
            _curindex = -1;
            _rgc = rgc;
        }

        /*
         * As required by IEnumerator
         */
        public bool MoveNext() {
            int size = _rgc.Count;

            if (_curindex >= size)
                return false;

            _curindex++;

            return(_curindex < size);
        }

        /*
         * As required by IEnumerator
         */
        public Object Current {
            get { return Capture;}
        }

        /*
         * Returns the current capture
         */
        public Capture Capture {
            get {
                if (_curindex < 0 || _curindex >= _rgc.Count)
                    throw new InvalidOperationException(SR.GetString(SR.EnumNotStarted));

                return _rgc[_curindex];
            }
        }

        /*
         * Reset to before the first item
         */
        public void Reset() {
            _curindex = -1;
        }
    }

}
