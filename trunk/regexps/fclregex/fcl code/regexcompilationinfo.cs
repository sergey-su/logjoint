//------------------------------------------------------------------------------
// <copyright file="RegexCompilationInfo.cs" company="Microsoft">
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
    ///    <para>
    ///       [To be supplied]
    ///    </para>
    /// </devdoc>
    [ Serializable() ] 
    public class RegexCompilationInfo { 
        private String           pattern;
        private RegexOptions     options;
        private String           name;
        private String           nspace;
        private bool             isPublic;

        /// <devdoc>
        ///    <para>
        ///       [To be supplied]
        ///    </para>
        /// </devdoc>
        public RegexCompilationInfo(String pattern, RegexOptions options, String name, String fullnamespace, bool ispublic) {
            Pattern = pattern;
            Name = name;
            Namespace = fullnamespace;
            this.options = options;
            isPublic = ispublic;
        }

        /// <devdoc>
        ///    <para>
        ///       [To be supplied]
        ///    </para>
        /// </devdoc>
        public String Pattern {
            get { return pattern; }
            set { 
                if (value == null)
                    throw new ArgumentNullException("value");
                pattern = value;
            }
        }

        /// <devdoc>
        ///    <para>
        ///       [To be supplied]
        ///    </para>
        /// </devdoc>
        public RegexOptions Options {
            get { return options; }
            set { options = value;}
        }

        /// <devdoc>
        ///    <para>
        ///       [To be supplied]
        ///    </para>
        /// </devdoc>
        public String Name {
            get { return name; }
            set { 
                if (value == null) {
                    throw new ArgumentNullException("value");
                }
				
                if (value.Length == 0) {
                	throw new ArgumentException(SR.GetString(SR.InvalidNullEmptyArgument, "value"), "value");					
                }

                name = value;
            }
        }

        /// <devdoc>
        ///    <para>
        ///       [To be supplied]
        ///    </para>
        /// </devdoc>
        public String Namespace {
            get { return nspace; }
            set { 
                if (value == null)
                    throw new ArgumentNullException("value");
                nspace = value;
            }
        }

        /// <devdoc>
        ///    <para>
        ///       [To be supplied]
        ///    </para>
        /// </devdoc>
        public bool IsPublic {
            get { return isPublic; }
            set { isPublic = value;}
        }
    }
}


