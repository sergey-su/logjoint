using System;
using System.Collections.Generic;
using System.Collections;
using System.Linq;
using System.Text;
using System.Runtime.Serialization;
using System.ComponentModel;
using System.Runtime;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Security.Permissions;
using System.Security;
using System.Reflection.Emit;
using System.Reflection;
using System.Threading;
using System.Runtime.InteropServices;

namespace LogJoint.RegRegularExpressions
{
	[Serializable]
	public class Regex : ISerializable
	{
		// Fields
		internal static int cacheSize = 15;
		protected internal Hashtable capnames;
		protected internal Hashtable caps;
		protected internal int capsize;
		protected internal string[] capslist;
		internal RegexCode code;
		protected internal RegexRunnerFactory factory;
		internal static LinkedList<CachedCodeEntry> livecode = new LinkedList<CachedCodeEntry>();
		internal const int MaxOptionShift = 10;
		protected internal string pattern;
		internal bool refsInitialized;
		internal SharedReference replref;
		protected internal RegexOptions roptions;
		internal ExclusiveReference runnerref;

		// Methods
		[TargetedPatchingOptOut("Performance critical to inline this type of method across NGen image boundaries")]
		protected Regex()
		{
		}

		[TargetedPatchingOptOut("Performance critical to inline this type of method across NGen image boundaries")]
		public Regex(string pattern) : this(pattern, RegexOptions.None, false)
		{
		}

		protected Regex(SerializationInfo info, StreamingContext context) : this(info.GetString("pattern"), (RegexOptions) info.GetInt32("options"))
		{
		}

		[TargetedPatchingOptOut("Performance critical to inline this type of method across NGen image boundaries")]
		public Regex(string pattern, RegexOptions options) : this(pattern, options, false)
		{
		}

		private Regex(string pattern, RegexOptions options, bool useCache)
		{
			CachedCodeEntry cachedAndUpdate = null;
			string str = null;
			if (pattern == null)
			{
				throw new ArgumentNullException("pattern");
			}
			if ((options < RegexOptions.None) || ((((int) options) >> 10) != 0))
			{
				throw new ArgumentOutOfRangeException("options");
			}
			if (((options & RegexOptions.ECMAScript) != RegexOptions.None) && ((options & ~(RegexOptions.CultureInvariant | RegexOptions.ECMAScript | RegexOptions.Compiled | RegexOptions.Multiline | RegexOptions.IgnoreCase)) != RegexOptions.None))
			{
				throw new ArgumentOutOfRangeException("options");
			}
			if ((options & RegexOptions.CultureInvariant) != RegexOptions.None)
			{
				str = CultureInfo.InvariantCulture.ToString();
			}
			else
			{
				str = CultureInfo.CurrentCulture.ToString();
			}
			string[] strArray = new string[] { ((int) options).ToString(NumberFormatInfo.InvariantInfo), ":", str, ":", pattern };
			string key = string.Concat(strArray);
			cachedAndUpdate = LookupCachedAndUpdate(key);
			this.pattern = pattern;
			this.roptions = options;
			if (cachedAndUpdate == null)
			{
				RegexTree t = RegexParser.Parse(pattern, this.roptions);
				this.capnames = t._capnames;
				this.capslist = t._capslist;
				this.code = RegexWriter.Write(t);
				this.caps = this.code._caps;
				this.capsize = this.code._capsize;
				this.InitializeReferences();
				t = null;
				if (useCache)
				{
					cachedAndUpdate = this.CacheCode(key);
				}
			}
			else
			{
				this.caps = cachedAndUpdate._caps;
				this.capnames = cachedAndUpdate._capnames;
				this.capslist = cachedAndUpdate._capslist;
				this.capsize = cachedAndUpdate._capsize;
				this.code = cachedAndUpdate._code;
				this.factory = cachedAndUpdate._factory;
				this.runnerref = cachedAndUpdate._runnerref;
				this.replref = cachedAndUpdate._replref;
				this.refsInitialized = true;
			}
			if (this.UseOptionC() && (this.factory == null))
			{
				this.factory = this.Compile(this.code, this.roptions);
				if (useCache && (cachedAndUpdate != null))
				{
					cachedAndUpdate.AddCompiled(this.factory);
				}
				this.code = null;
			}
		}

		private CachedCodeEntry CacheCode(string key)
		{
			CachedCodeEntry entry = null;
			lock (livecode)
			{
				for (LinkedListNode<CachedCodeEntry> node = livecode.First; node != null; node = node.Next)
				{
					if (node.Value._key == key)
					{
						livecode.Remove(node);
						livecode.AddFirst(node);
						return node.Value;
					}
				}
				if (cacheSize != 0)
				{
					entry = new CachedCodeEntry(key, this.capnames, this.capslist, this.code, this.caps, this.capsize, this.runnerref, this.replref);
					livecode.AddFirst(entry);
					if (livecode.Count > cacheSize)
					{
						livecode.RemoveLast();
					}
				}
			}
			return entry;
		}

		[MethodImpl(MethodImplOptions.NoInlining), HostProtection(SecurityAction.LinkDemand, MayLeakOnAbort=true)]
		internal RegexRunnerFactory Compile(RegexCode code, RegexOptions roptions)
		{
			return RegexCompiler.Compile(code, roptions);
		}

		[TargetedPatchingOptOut("Performance critical to inline this type of method across NGen image boundaries"), HostProtection(SecurityAction.LinkDemand, MayLeakOnAbort=true)]
		public static void CompileToAssembly(RegexCompilationInfo[] regexinfos, AssemblyName assemblyname)
		{
			CompileToAssemblyInternal(regexinfos, assemblyname, null, null);
		}

		[TargetedPatchingOptOut("Performance critical to inline this type of method across NGen image boundaries"), HostProtection(SecurityAction.LinkDemand, MayLeakOnAbort=true)]
		public static void CompileToAssembly(RegexCompilationInfo[] regexinfos, AssemblyName assemblyname, CustomAttributeBuilder[] attributes)
		{
			CompileToAssemblyInternal(regexinfos, assemblyname, attributes, null);
		}

		[TargetedPatchingOptOut("Performance critical to inline this type of method across NGen image boundaries"), HostProtection(SecurityAction.LinkDemand, MayLeakOnAbort=true)]
		public static void CompileToAssembly(RegexCompilationInfo[] regexinfos, AssemblyName assemblyname, CustomAttributeBuilder[] attributes, string resourceFile)
		{
			CompileToAssemblyInternal(regexinfos, assemblyname, attributes, resourceFile);
		}

		private static void CompileToAssemblyInternal(RegexCompilationInfo[] regexinfos, AssemblyName assemblyname, CustomAttributeBuilder[] attributes, string resourceFile)
		{
			if (assemblyname == null)
			{
				throw new ArgumentNullException("assemblyname");
			}
			if (regexinfos == null)
			{
				throw new ArgumentNullException("regexinfos");
			}
			RegexCompiler.CompileToAssembly(regexinfos, assemblyname, attributes, resourceFile);
		}

		public static string Escape(string str)
		{
			if (str == null)
			{
				throw new ArgumentNullException("str");
			}
			return RegexParser.Escape(str);
		}

		public string[] GetGroupNames()
		{
			string[] strArray;
			if (this.capslist == null)
			{
				int capsize = this.capsize;
				strArray = new string[capsize];
				for (int i = 0; i < capsize; i++)
				{
					strArray[i] = Convert.ToString(i, CultureInfo.InvariantCulture);
				}
				return strArray;
			}
			strArray = new string[this.capslist.Length];
			Array.Copy(this.capslist, 0, strArray, 0, this.capslist.Length);
			return strArray;
		}

		public int[] GetGroupNumbers()
		{
			int[] numArray;
			if (this.caps == null)
			{
				int capsize = this.capsize;
				numArray = new int[capsize];
				for (int i = 0; i < capsize; i++)
				{
					numArray[i] = i;
				}
				return numArray;
			}
			numArray = new int[this.caps.Count];
			IDictionaryEnumerator enumerator = this.caps.GetEnumerator();
			while (enumerator.MoveNext())
			{
				numArray[(int) enumerator.Value] = (int) enumerator.Key;
			}
			return numArray;
		}

		public string GroupNameFromNumber(int i)
		{
			if (this.capslist == null)
			{
				if ((i >= 0) && (i < this.capsize))
				{
					return i.ToString(CultureInfo.InvariantCulture);
				}
				return string.Empty;
			}
			if (this.caps != null)
			{
				object obj2 = this.caps[i];
				if (obj2 == null)
				{
					return string.Empty;
				}
				i = (int) obj2;
			}
			if ((i >= 0) && (i < this.capslist.Length))
			{
				return this.capslist[i];
			}
			return string.Empty;
		}

		public int GroupNumberFromName(string name)
		{
			int num = -1;
			if (name == null)
			{
				throw new ArgumentNullException("name");
			}
			if (this.capnames != null)
			{
				object obj2 = this.capnames[name];
				if (obj2 == null)
				{
					return -1;
				}
				return (int) obj2;
			}
			num = 0;
			for (int i = 0; i < name.Length; i++)
			{
				char ch = name[i];
				if ((ch > '9') || (ch < '0'))
				{
					return -1;
				}
				num *= 10;
				num += ch - '0';
			}
			if ((num >= 0) && (num < this.capsize))
			{
				return num;
			}
			return -1;
		}

		protected void InitializeReferences()
		{
			if (this.refsInitialized)
			{
				throw new NotSupportedException();
			}
			this.refsInitialized = true;
			this.runnerref = new ExclusiveReference();
			this.replref = new SharedReference();
		}

		public bool IsMatch(string input)
		{
			if (input == null)
			{
				throw new ArgumentNullException("input");
			}
			return (null == this.Run(true, -1, input, 0, input.Length, this.UseOptionR() ? input.Length : 0));
		}

		public bool IsMatch(string input, int startat)
		{
			if (input == null)
			{
				throw new ArgumentNullException("input");
			}
			return (null == this.Run(true, -1, input, 0, input.Length, startat));
		}

		public static bool IsMatch(string input, string pattern)
		{
			return new Regex(pattern, RegexOptions.None, true).IsMatch(input);
		}

		public static bool IsMatch(string input, string pattern, RegexOptions options)
		{
			return new Regex(pattern, options, true).IsMatch(input);
		}

		private static CachedCodeEntry LookupCachedAndUpdate(string key)
		{
			lock (livecode)
			{
				for (LinkedListNode<CachedCodeEntry> node = livecode.First; node != null; node = node.Next)
				{
					if (node.Value._key == key)
					{
						livecode.Remove(node);
						livecode.AddFirst(node);
						return node.Value;
					}
				}
			}
			return null;
		}

		public Match Match(string input)
		{
			if (input == null)
			{
				throw new ArgumentNullException("input");
			}
			return this.Run(false, -1, input, 0, input.Length, this.UseOptionR() ? input.Length : 0);
		}

		public Match Match(string input, int startat)
		{
			if (input == null)
			{
				throw new ArgumentNullException("input");
			}
			return this.Run(false, -1, input, 0, input.Length, startat);
		}

		public static Match Match(string input, string pattern)
		{
			return new Regex(pattern, RegexOptions.None, true).Match(input);
		}

		public Match Match(string input, int beginning, int length)
		{
			if (input == null)
			{
				throw new ArgumentNullException("input");
			}
			return this.Run(false, -1, input, beginning, length, this.UseOptionR() ? (beginning + length) : beginning);
		}

		public static Match Match(string input, string pattern, RegexOptions options)
		{
			return new Regex(pattern, options, true).Match(input);
		}

		public MatchCollection Matches(string input)
		{
			if (input == null)
			{
				throw new ArgumentNullException("input");
			}
			return new MatchCollection(this, input, 0, input.Length, this.UseOptionR() ? input.Length : 0);
		}

		public MatchCollection Matches(string input, int startat)
		{
			if (input == null)
			{
				throw new ArgumentNullException("input");
			}
			return new MatchCollection(this, input, 0, input.Length, startat);
		}

		public static MatchCollection Matches(string input, string pattern)
		{
			return new Regex(pattern, RegexOptions.None, true).Matches(input);
		}

		public static MatchCollection Matches(string input, string pattern, RegexOptions options)
		{
			return new Regex(pattern, options, true).Matches(input);
		}

		public string Replace(string input, string replacement)
		{
			if (input == null)
			{
				throw new ArgumentNullException("input");
			}
			return this.Replace(input, replacement, -1, this.UseOptionR() ? input.Length : 0);
		}

		public string Replace(string input, MatchEvaluator evaluator)
		{
			if (input == null)
			{
				throw new ArgumentNullException("input");
			}
			return this.Replace(input, evaluator, -1, this.UseOptionR() ? input.Length : 0);
		}

		public string Replace(string input, string replacement, int count)
		{
			if (input == null)
			{
				throw new ArgumentNullException("input");
			}
			return this.Replace(input, replacement, count, this.UseOptionR() ? input.Length : 0);
		}

		public static string Replace(string input, string pattern, string replacement)
		{
			return new Regex(pattern, RegexOptions.None, true).Replace(input, replacement);
		}

		public static string Replace(string input, string pattern, MatchEvaluator evaluator)
		{
			return new Regex(pattern, RegexOptions.None, true).Replace(input, evaluator);
		}

		public string Replace(string input, MatchEvaluator evaluator, int count)
		{
			if (input == null)
			{
				throw new ArgumentNullException("input");
			}
			return this.Replace(input, evaluator, count, this.UseOptionR() ? input.Length : 0);
		}

		public string Replace(string input, string replacement, int count, int startat)
		{
			if (input == null)
			{
				throw new ArgumentNullException("input");
			}
			if (replacement == null)
			{
				throw new ArgumentNullException("replacement");
			}
			RegexReplacement replacement2 = (RegexReplacement) this.replref.Get();
			if ((replacement2 == null) || !replacement2.Pattern.Equals(replacement))
			{
				replacement2 = RegexParser.ParseReplacement(replacement, this.caps, this.capsize, this.capnames, this.roptions);
				this.replref.Cache(replacement2);
			}
			return replacement2.Replace(this, input, count, startat);
		}

		public static string Replace(string input, string pattern, string replacement, RegexOptions options)
		{
			return new Regex(pattern, options, true).Replace(input, replacement);
		}

		public static string Replace(string input, string pattern, MatchEvaluator evaluator, RegexOptions options)
		{
			return new Regex(pattern, options, true).Replace(input, evaluator);
		}

		public string Replace(string input, MatchEvaluator evaluator, int count, int startat)
		{
			if (input == null)
			{
				throw new ArgumentNullException("input");
			}
			return RegexReplacement.Replace(evaluator, this, input, count, startat);
		}

		internal Match Run(bool quick, int prevlen, string input, int beginning, int length, int startat)
		{
			RegexRunner runner = null;
			if ((startat < 0) || (startat > input.Length))
			{
				throw new ArgumentOutOfRangeException("start");
			}
			if ((length < 0) || (length > input.Length))
			{
				throw new ArgumentOutOfRangeException("length");
			}
			runner = (RegexRunner) this.runnerref.Get();
			if (runner == null)
			{
				if (this.factory != null)
				{
					runner = this.factory.CreateInstance();
				}
				else
				{
					runner = null;// new RegexInterpreter(this.code, this.UseOptionInvariant() ? CultureInfo.InvariantCulture : CultureInfo.CurrentCulture);
				}
			}
			Match match = runner.Scan(this, input, beginning, beginning + length, startat, prevlen, quick);
			this.runnerref.Release(runner);
			return match;
		}

		public string[] Split(string input)
		{
			if (input == null)
			{
				throw new ArgumentNullException("input");
			}
			return this.Split(input, 0, this.UseOptionR() ? input.Length : 0);
		}

		public string[] Split(string input, int count)
		{
			if (input == null)
			{
				throw new ArgumentNullException("input");
			}
			return RegexReplacement.Split(this, input, count, this.UseOptionR() ? input.Length : 0);
		}

		public static string[] Split(string input, string pattern)
		{
			return new Regex(pattern, RegexOptions.None, true).Split(input);
		}

		public string[] Split(string input, int count, int startat)
		{
			if (input == null)
			{
				throw new ArgumentNullException("input");
			}
			return RegexReplacement.Split(this, input, count, startat);
		}

		public static string[] Split(string input, string pattern, RegexOptions options)
		{
			return new Regex(pattern, options, true).Split(input);
		}

		void ISerializable.GetObjectData(SerializationInfo si, StreamingContext context)
		{
			si.AddValue("pattern", this.ToString());
			si.AddValue("options", this.Options);
		}

		public override string ToString()
		{
			return this.pattern;
		}

		public static string Unescape(string str)
		{
			if (str == null)
			{
				throw new ArgumentNullException("str");
			}
			return RegexParser.Unescape(str);
		}

		protected bool UseOptionC()
		{
			return ((this.roptions & RegexOptions.Compiled) != RegexOptions.None);
		}

		internal bool UseOptionInvariant()
		{
			return ((this.roptions & RegexOptions.CultureInvariant) != RegexOptions.None);
		}

		protected bool UseOptionR()
		{
			return ((this.roptions & RegexOptions.RightToLeft) != RegexOptions.None);
		}

		// Properties
		public static int CacheSize
		{
			[TargetedPatchingOptOut("Performance critical to inline this type of method across NGen image boundaries")]
			get
			{
				return cacheSize;
			}
			set
			{
				if (value < 0)
				{
					throw new ArgumentOutOfRangeException("value");
				}
				cacheSize = value;
				if (livecode.Count > cacheSize)
				{
					lock (livecode)
					{
						while (livecode.Count > cacheSize)
						{
							livecode.RemoveLast();
						}
					}
				}
			}
		}

		public RegexOptions Options
		{
			[TargetedPatchingOptOut("Performance critical to inline this type of method across NGen image boundaries")]
			get
			{
				return this.roptions;
			}
		}

		public bool RightToLeft
		{
			[TargetedPatchingOptOut("Performance critical to inline this type of method across NGen image boundaries")]
			get
			{
				return this.UseOptionR();
			}
		}
	}

	internal sealed class RegexCode
	{
		// Fields
		internal int _anchors;
		internal RegexBoyerMoore _bmPrefix;
		internal Hashtable _caps;
		internal int _capsize;
		internal int[] _codes;
		internal RegexPrefix _fcPrefix;
		internal bool _rightToLeft;
		internal string[] _strings;
		internal int _trackcount;
		internal const int Back = 0x80;
		internal const int Back2 = 0x100;
		internal const int Backjump = 0x23;
		internal const int Beginning = 0x12;
		internal const int Bol = 14;
		internal const int Boundary = 0x10;
		internal const int Branchcount = 0x1c;
		internal const int Branchmark = 0x18;
		internal const int Capturemark = 0x20;
		internal const int Ci = 0x200;
		internal const int ECMABoundary = 0x29;
		internal const int End = 0x15;
		internal const int EndZ = 20;
		internal const int Eol = 15;
		internal const int Forejump = 0x24;
		internal const int Getmark = 0x21;
		internal const int Goto = 0x26;
		internal const int Lazybranch = 0x17;
		internal const int Lazybranchcount = 0x1d;
		internal const int Lazybranchmark = 0x19;
		internal const int Mask = 0x3f;
		internal const int Multi = 12;
		internal const int Nonboundary = 0x11;
		internal const int NonECMABoundary = 0x2a;
		internal const int Nothing = 0x16;
		internal const int Notone = 10;
		internal const int Notonelazy = 7;
		internal const int Notoneloop = 4;
		internal const int Notonerep = 1;
		internal const int Nullcount = 0x1a;
		internal const int Nullmark = 30;
		internal const int One = 9;
		internal const int Onelazy = 6;
		internal const int Oneloop = 3;
		internal const int Onerep = 0;
		internal const int Prune = 0x27;
		internal const int Ref = 13;
		internal const int Rtl = 0x40;
		internal const int Set = 11;
		internal const int Setcount = 0x1b;
		internal const int Setjump = 0x22;
		internal const int Setlazy = 8;
		internal const int Setloop = 5;
		internal const int Setmark = 0x1f;
		internal const int Setrep = 2;
		internal const int Start = 0x13;
		internal const int Stop = 40;
		internal const int Testref = 0x25;

		// Methods
		internal RegexCode(int[] codes, List<string> stringlist, int trackcount, Hashtable caps, int capsize, RegexBoyerMoore bmPrefix, RegexPrefix fcPrefix, int anchors, bool rightToLeft)
		{
			this._codes = codes;
			this._strings = new string[stringlist.Count];
			this._trackcount = trackcount;
			this._caps = caps;
			this._capsize = capsize;
			this._bmPrefix = bmPrefix;
			this._fcPrefix = fcPrefix;
			this._anchors = anchors;
			this._rightToLeft = rightToLeft;
			stringlist.CopyTo(0, this._strings, 0, stringlist.Count);
		}

		internal static ArgumentException MakeException(string message)
		{
			return new ArgumentException(message);
		}

		internal static bool OpcodeBacktracks(int Op)
		{
			Op &= 0x3f;
			switch (Op)
			{
				case 3:
				case 4:
				case 5:
				case 6:
				case 7:
				case 8:
				case 0x17:
				case 0x18:
				case 0x19:
				case 0x1a:
				case 0x1b:
				case 0x1c:
				case 0x1d:
				case 0x1f:
				case 0x20:
				case 0x21:
				case 0x22:
				case 0x23:
				case 0x24:
				case 0x26:
					return true;
			}
			return false;
		}

		internal static int OpcodeSize(int Opcode)
		{
			Opcode &= 0x3f;
			switch (Opcode)
			{
				case 0:
				case 1:
				case 2:
				case 3:
				case 4:
				case 5:
				case 6:
				case 7:
				case 8:
				case 0x1c:
				case 0x1d:
				case 0x20:
					return 3;

				case 9:
				case 10:
				case 11:
				case 12:
				case 13:
				case 0x17:
				case 0x18:
				case 0x19:
				case 0x1a:
				case 0x1b:
				case 0x25:
				case 0x26:
				case 0x27:
					return 2;

				case 14:
				case 15:
				case 0x10:
				case 0x11:
				case 0x12:
				case 0x13:
				case 20:
				case 0x15:
				case 0x16:
				case 30:
				case 0x1f:
				case 0x21:
				case 0x22:
				case 0x23:
				case 0x24:
				case 40:
				case 0x29:
				case 0x2a:
					return 1;
			}
			throw MakeException("UnexpectedOpcode");
		}
	}

	[EditorBrowsable(EditorBrowsableState.Never)]
	public abstract class RegexRunnerFactory
	{
		// Methods
		[TargetedPatchingOptOut("Performance critical to inline this type of method across NGen image boundaries")]
		protected RegexRunnerFactory()
		{
		}

		protected internal abstract RegexRunner CreateInstance();
	}

	internal sealed class CachedCodeEntry
	{
		// Fields
		internal Hashtable _capnames;
		internal Hashtable _caps;
		internal int _capsize;
		internal string[] _capslist;
		internal RegexCode _code;
		internal RegexRunnerFactory _factory;
		internal string _key;
		internal SharedReference _replref;
		internal ExclusiveReference _runnerref;

		// Methods
		internal CachedCodeEntry(string key, Hashtable capnames, string[] capslist, RegexCode code, Hashtable caps, int capsize, ExclusiveReference runner, SharedReference repl)
		{
			this._key = key;
			this._capnames = capnames;
			this._capslist = capslist;
			this._code = code;
			this._caps = caps;
			this._capsize = capsize;
			this._runnerref = runner;
			this._replref = repl;
		}

		internal void AddCompiled(RegexRunnerFactory factory)
		{
			this._factory = factory;
			this._code = null;
		}
	}

	internal sealed class SharedReference
	{
		// Fields
		private int _locked;
		private WeakReference _ref = new WeakReference(null);

		// Methods
		internal void Cache(object obj)
		{
			if (Interlocked.Exchange(ref this._locked, 1) == 0)
			{
				this._ref.Target = obj;
				this._locked = 0;
			}
		}

		internal object Get()
		{
			if (Interlocked.Exchange(ref this._locked, 1) == 0)
			{
				object target = this._ref.Target;
				this._locked = 0;
				return target;
			}
			return null;
		}
	}

	[Flags]
	public enum RegexOptions
	{
		Compiled = 8,
		CultureInvariant = 0x200,
		ECMAScript = 0x100,
		ExplicitCapture = 4,
		IgnoreCase = 1,
		IgnorePatternWhitespace = 0x20,
		Multiline = 2,
		None = 0,
		RightToLeft = 0x40,
		Singleline = 0x10
	}


	internal sealed class ExclusiveReference
	{
		// Fields
		private int _locked;
		private object _obj;
		private RegexRunner _ref;

		// Methods
		internal object Get()
		{
			if (Interlocked.Exchange(ref this._locked, 1) != 0)
			{
				return null;
			}
			object obj2 = this._ref;
			if (obj2 == null)
			{
				this._locked = 0;
				return null;
			}
			this._obj = obj2;
			return obj2;
		}

		internal void Release(object obj)
		{
			if (obj == null)
			{
				throw new ArgumentNullException("obj");
			}
			if (this._obj == obj)
			{
				this._obj = null;
				this._locked = 0;
			}
			else if ((this._obj == null) && (Interlocked.Exchange(ref this._locked, 1) == 0))
			{
				if (this._ref == null)
				{
					this._ref = (RegexRunner) obj;
				}
				this._locked = 0;
			}
		}
	}

	[Serializable]
	public class Match : Group
	{
		// Fields
		internal bool _balancing;
		internal static Match _empty = new Match(null, 1, string.Empty, 0, 0, 0);
		internal GroupCollection _groupcoll;
		internal int[] _matchcount;
		internal int[][] _matches;
		internal Regex _regex;
		internal int _textbeg;
		internal int _textend;
		internal int _textpos;
		internal int _textstart;

		// Methods
		internal Match(Regex regex, int capcount, string text, int begpos, int len, int startpos) : base(text, new int[2], 0)
		{
			this._regex = regex;
			this._matchcount = new int[capcount];
			this._matches = new int[capcount][];
			this._matches[0] = base._caps;
			this._textbeg = begpos;
			this._textend = begpos + len;
			this._textstart = startpos;
			this._balancing = false;
		}

		internal virtual void AddMatch(int cap, int start, int len)
		{
			if (this._matches[cap] == null)
			{
				this._matches[cap] = new int[2];
			}
			int num = this._matchcount[cap];
			if (((num * 2) + 2) > this._matches[cap].Length)
			{
				int[] numArray = this._matches[cap];
				int[] numArray2 = new int[num * 8];
				for (int i = 0; i < (num * 2); i++)
				{
					numArray2[i] = numArray[i];
				}
				this._matches[cap] = numArray2;
			}
			this._matches[cap][num * 2] = start;
			this._matches[cap][(num * 2) + 1] = len;
			this._matchcount[cap] = num + 1;
		}

		internal virtual void BalanceMatch(int cap)
		{
			this._balancing = true;
			int num = this._matchcount[cap];
			int index = (num * 2) - 2;
			if (this._matches[cap][index] < 0)
			{
				index = -3 - this._matches[cap][index];
			}
			index -= 2;
			if ((index >= 0) && (this._matches[cap][index] < 0))
			{
				this.AddMatch(cap, this._matches[cap][index], this._matches[cap][index + 1]);
			}
			else
			{
				this.AddMatch(cap, -3 - index, -4 - index);
			}
		}

		internal virtual string GroupToStringImpl(int groupnum)
		{
			int num = this._matchcount[groupnum];
			if (num == 0)
			{
				return string.Empty;
			}
			int[] numArray = this._matches[groupnum];
			return base._text.Substring(numArray[(num - 1) * 2], numArray[(num * 2) - 1]);
		}

		internal virtual bool IsMatched(int cap)
		{
			return (((cap < this._matchcount.Length) && (this._matchcount[cap] > 0)) && (this._matches[cap][(this._matchcount[cap] * 2) - 1] != -2));
		}

		internal string LastGroupToStringImpl()
		{
			return this.GroupToStringImpl(this._matchcount.Length - 1);
		}

		internal virtual int MatchIndex(int cap)
		{
			int num = this._matches[cap][(this._matchcount[cap] * 2) - 2];
			if (num >= 0)
			{
				return num;
			}
			return this._matches[cap][-3 - num];
		}

		internal virtual int MatchLength(int cap)
		{
			int num = this._matches[cap][(this._matchcount[cap] * 2) - 1];
			if (num >= 0)
			{
				return num;
			}
			return this._matches[cap][-3 - num];
		}

		public Match NextMatch()
		{
			if (this._regex == null)
			{
				return this;
			}
			return this._regex.Run(false, base._length, base._text, this._textbeg, this._textend - this._textbeg, this._textpos);
		}

		internal virtual void RemoveMatch(int cap)
		{
			this._matchcount[cap]--;
		}

		internal virtual void Reset(Regex regex, string text, int textbeg, int textend, int textstart)
		{
			this._regex = regex;
			base._text = text;
			this._textbeg = textbeg;
			this._textend = textend;
			this._textstart = textstart;
			for (int i = 0; i < this._matchcount.Length; i++)
			{
				this._matchcount[i] = 0;
			}
			this._balancing = false;
		}

		public virtual string Result(string replacement)
		{
			if (replacement == null)
			{
				throw new ArgumentNullException("replacement");
			}
			if (this._regex == null)
			{
				throw new NotSupportedException("NoResultOnFailed");
			}
			RegexReplacement replacement2 = (RegexReplacement) this._regex.replref.Get();
			if ((replacement2 == null) || !replacement2.Pattern.Equals(replacement))
			{
				replacement2 = RegexParser.ParseReplacement(replacement, this._regex.caps, this._regex.capsize, this._regex.capnames, this._regex.roptions);
				this._regex.replref.Cache(replacement2);
			}
			return replacement2.Replacement(this);
		}

		[HostProtection(SecurityAction.LinkDemand, Synchronization=true)]
		public static Match Synchronized(Match inner)
		{
			if (inner == null)
			{
				throw new ArgumentNullException("inner");
			}
			int length = inner._matchcount.Length;
			for (int i = 0; i < length; i++)
			{
				Group group = inner.Groups[i];
				Group.Synchronized(group);
			}
			return inner;
		}

		internal virtual void Tidy(int textpos)
		{
			int[] numArray = this._matches[0];
			base._index = numArray[0];
			base._length = numArray[1];
			this._textpos = textpos;
			base._capcount = this._matchcount[0];
			if (this._balancing)
			{
				for (int i = 0; i < this._matchcount.Length; i++)
				{
					int num2 = this._matchcount[i] * 2;
					int[] numArray2 = this._matches[i];
					int index = 0;
					index = 0;
					while (index < num2)
					{
						if (numArray2[index] < 0)
						{
							break;
						}
						index++;
					}
					int num4 = index;
					while (index < num2)
					{
						if (numArray2[index] < 0)
						{
							num4--;
						}
						else
						{
							if (index != num4)
							{
								numArray2[num4] = numArray2[index];
							}
							num4++;
						}
						index++;
					}
					this._matchcount[i] = num4 / 2;
				}
				this._balancing = false;
			}
		}

		// Properties
		public static Match Empty
		{
			[TargetedPatchingOptOut("Performance critical to inline this type of method across NGen image boundaries")]
			get
			{
				return _empty;
			}
		}

		public virtual GroupCollection Groups
		{
			get
			{
				if (this._groupcoll == null)
				{
					this._groupcoll = new GroupCollection(this, null);
				}
				return this._groupcoll;
			}
		}
	}

	[Serializable]
	public class Group : Capture
	{
		// Fields
		internal CaptureCollection _capcoll;
		internal int _capcount;
		internal int[] _caps;
		internal static Group _emptygroup = new Group(string.Empty, new int[0], 0);

		// Methods
		internal Group(string text, int[] caps, int capcount) : base(text, (capcount == 0) ? 0 : caps[(capcount - 1) * 2], (capcount == 0) ? 0 : caps[(capcount * 2) - 1])
		{
			this._caps = caps;
			this._capcount = capcount;
		}

		[HostProtection(SecurityAction.LinkDemand, Synchronization=true)]
		public static Group Synchronized(Group inner)
		{
			if (inner == null)
			{
				throw new ArgumentNullException("inner");
			}
			CaptureCollection captures = inner.Captures;
			if (inner._capcount > 0)
			{
				Capture capture1 = captures[0];
			}
			return inner;
		}

		// Properties
		public CaptureCollection Captures
		{
			get
			{
				if (this._capcoll == null)
				{
					this._capcoll = new CaptureCollection(this);
				}
				return this._capcoll;
			}
		}

		public bool Success
		{
			get
			{
				return (this._capcount != 0);
			}
		}
	}

	[Serializable]
	public class Capture
	{
		// Fields
		internal int _index;
		internal int _length;
		internal string _text;

		// Methods
		internal Capture(string text, int i, int l)
		{
			this._text = text;
			this._index = i;
			this._length = l;
		}

		internal string GetLeftSubstring()
		{
			return this._text.Substring(0, this._index);
		}

		internal string GetOriginalString()
		{
			return this._text;
		}

		internal string GetRightSubstring()
		{
			return this._text.Substring(this._index + this._length, (this._text.Length - this._index) - this._length);
		}

		[TargetedPatchingOptOut("Performance critical to inline this type of method across NGen image boundaries")]
		public override string ToString()
		{
			return this.Value;
		}

		// Properties
		public int Index
		{
			[TargetedPatchingOptOut("Performance critical to inline this type of method across NGen image boundaries")]
			get
			{
				return this._index;
			}
		}

		public int Length
		{
			[TargetedPatchingOptOut("Performance critical to inline this type of method across NGen image boundaries")]
			get
			{
				return this._length;
			}
		}

		public string Value
		{
			get
			{
				return this._text.Substring(this._index, this._length);
			}
		}
	}

	internal sealed class RegexTree
	{
		// Fields
		internal Hashtable _capnames;
		internal int[] _capnumlist;
		internal Hashtable _caps;
		internal string[] _capslist;
		internal int _captop;
		internal RegexOptions _options;
		internal RegexNode _root;

		// Methods
		internal RegexTree(RegexNode root, Hashtable caps, int[] capnumlist, int captop, Hashtable capnames, string[] capslist, RegexOptions opts)
		{
			this._root = root;
			this._caps = caps;
			this._capnumlist = capnumlist;
			this._capnames = capnames;
			this._capslist = capslist;
			this._captop = captop;
			this._options = opts;
		}
	}

	internal sealed class RegexNode
	{
		// Fields
		internal char _ch;
		internal List<RegexNode> _children;
		internal int _m;
		internal int _n;
		internal RegexNode _next;
		internal RegexOptions _options;
		internal string _str;
		internal int _type;
		internal const int Alternate = 0x18;
		internal const int Beginning = 0x12;
		internal const int Bol = 14;
		internal const int Boundary = 0x10;
		internal const int Capture = 0x1c;
		internal const int Concatenate = 0x19;
		internal const int ECMABoundary = 0x29;
		internal const int Empty = 0x17;
		internal const int End = 0x15;
		internal const int EndZ = 20;
		internal const int Eol = 15;
		internal const int Greedy = 0x20;
		internal const int Group = 0x1d;
		internal const int Lazyloop = 0x1b;
		internal const int Loop = 0x1a;
		internal const int Multi = 12;
		internal const int Nonboundary = 0x11;
		internal const int NonECMABoundary = 0x2a;
		internal const int Nothing = 0x16;
		internal const int Notone = 10;
		internal const int Notonelazy = 7;
		internal const int Notoneloop = 4;
		internal const int One = 9;
		internal const int Onelazy = 6;
		internal const int Oneloop = 3;
		internal const int Prevent = 0x1f;
		internal const int Ref = 13;
		internal const int Require = 30;
		internal const int Set = 11;
		internal const int Setlazy = 8;
		internal const int Setloop = 5;
		internal const int Start = 0x13;
		internal const int Testgroup = 0x22;
		internal const int Testref = 0x21;

		// Methods
		internal RegexNode(int type, RegexOptions options)
		{
			this._type = type;
			this._options = options;
		}

		internal RegexNode(int type, RegexOptions options, char ch)
		{
			this._type = type;
			this._options = options;
			this._ch = ch;
		}

		internal RegexNode(int type, RegexOptions options, int m)
		{
			this._type = type;
			this._options = options;
			this._m = m;
		}

		internal RegexNode(int type, RegexOptions options, string str)
		{
			this._type = type;
			this._options = options;
			this._str = str;
		}

		internal RegexNode(int type, RegexOptions options, int m, int n)
		{
			this._type = type;
			this._options = options;
			this._m = m;
			this._n = n;
		}

		internal void AddChild(RegexNode newChild)
		{
			if (this._children == null)
			{
				this._children = new List<RegexNode>(4);
			}
			RegexNode item = newChild.Reduce();
			this._children.Add(item);
			item._next = this;
		}

		internal RegexNode Child(int i)
		{
			return this._children[i];
		}

		internal int ChildCount()
		{
			if (this._children != null)
			{
				return this._children.Count;
			}
			return 0;
		}

		internal RegexNode MakeQuantifier(bool lazy, int min, int max)
		{
			if ((min == 0) && (max == 0))
			{
				return new RegexNode(0x17, this._options);
			}
			if ((min == 1) && (max == 1))
			{
				return this;
			}
			switch (this._type)
			{
				case 9:
				case 10:
				case 11:
					this.MakeRep(lazy ? 6 : 3, min, max);
					return this;
			}
			RegexNode node = new RegexNode(lazy ? 0x1b : 0x1a, this._options, min, max);
			node.AddChild(this);
			return node;
		}

		internal void MakeRep(int type, int min, int max)
		{
			this._type += type - 9;
			this._m = min;
			this._n = max;
		}

		internal RegexNode Reduce()
		{
			switch (this.Type())
			{
				case 0x18:
					return this.ReduceAlternation();

				case 0x19:
					return this.ReduceConcatenation();

				case 0x1a:
				case 0x1b:
					return this.ReduceRep();

				case 0x1d:
					return this.ReduceGroup();

				case 11:
				case 5:
					return this.ReduceSet();
			}
			return this;
		}

		internal RegexNode ReduceAlternation()
		{
			if (this._children == null)
			{
				return new RegexNode(0x16, this._options);
			}
			bool flag = false;
			bool flag2 = false;
			RegexOptions none = RegexOptions.None;
			int num = 0;
			int index = 0;
			while (num < this._children.Count)
			{
				RegexCharClass class2;
				RegexNode node = this._children[num];
				if (index < num)
				{
					this._children[index] = node;
				}
				if (node._type == 0x18)
				{
					for (int i = 0; i < node._children.Count; i++)
					{
						node._children[i]._next = this;
					}
					this._children.InsertRange(num + 1, node._children);
					index--;
					goto Label_01C2;
				}
				if ((node._type != 11) && (node._type != 9))
				{
					goto Label_01AB;
				}
				RegexOptions options2 = node._options & (RegexOptions.RightToLeft | RegexOptions.IgnoreCase);
				if (node._type == 11)
				{
					if ((flag && (none == options2)) && (!flag2 && RegexCharClass.IsMergeable(node._str)))
					{
						goto Label_011B;
					}
					flag = true;
					flag2 = !RegexCharClass.IsMergeable(node._str);
					none = options2;
					goto Label_01C2;
				}
				if ((!flag || (none != options2)) || flag2)
				{
					flag = true;
					flag2 = false;
					none = options2;
					goto Label_01C2;
				}
			Label_011B:
				index--;
				RegexNode node2 = this._children[index];
				if (node2._type == 9)
				{
					class2 = new RegexCharClass();
					class2.AddChar(node2._ch);
				}
				else
				{
					class2 = RegexCharClass.Parse(node2._str);
				}
				if (node._type == 9)
				{
					class2.AddChar(node._ch);
				}
				else
				{
					RegexCharClass cc = RegexCharClass.Parse(node._str);
					class2.AddCharClass(cc);
				}
				node2._type = 11;
				node2._str = class2.ToStringClass();
				goto Label_01C2;
			Label_01AB:
				if (node._type == 0x16)
				{
					index--;
				}
				else
				{
					flag = false;
					flag2 = false;
				}
			Label_01C2:
				num++;
				index++;
			}
			if (index < num)
			{
				this._children.RemoveRange(index, num - index);
			}
			return this.StripEnation(0x16);
		}

		internal RegexNode ReduceConcatenation()
		{
			if (this._children == null)
			{
				return new RegexNode(0x17, this._options);
			}
			bool flag = false;
			RegexOptions none = RegexOptions.None;
			int num = 0;
			int index = 0;
			while (num < this._children.Count)
			{
				RegexNode node = this._children[num];
				if (index < num)
				{
					this._children[index] = node;
				}
				if ((node._type == 0x19) && ((node._options & RegexOptions.RightToLeft) == (this._options & RegexOptions.RightToLeft)))
				{
					for (int i = 0; i < node._children.Count; i++)
					{
						node._children[i]._next = this;
					}
					this._children.InsertRange(num + 1, node._children);
					index--;
				}
				else if ((node._type == 12) || (node._type == 9))
				{
					RegexOptions options2 = node._options & (RegexOptions.RightToLeft | RegexOptions.IgnoreCase);
					if (!flag || (none != options2))
					{
						flag = true;
						none = options2;
					}
					else
					{
						RegexNode node2 = this._children[--index];
						if (node2._type == 9)
						{
							node2._type = 12;
							node2._str = Convert.ToString(node2._ch, CultureInfo.InvariantCulture);
						}
						if ((options2 & RegexOptions.RightToLeft) == RegexOptions.None)
						{
							if (node._type == 9)
							{
								node2._str = node2._str + node._ch.ToString();
							}
							else
							{
								node2._str = node2._str + node._str;
							}
						}
						else if (node._type == 9)
						{
							node2._str = node._ch.ToString() + node2._str;
						}
						else
						{
							node2._str = node._str + node2._str;
						}
					}
				}
				else if (node._type == 0x17)
				{
					index--;
				}
				else
				{
					flag = false;
				}
				num++;
				index++;
			}
			if (index < num)
			{
				this._children.RemoveRange(index, num - index);
			}
			return this.StripEnation(0x17);
		}

		internal RegexNode ReduceGroup()
		{
			RegexNode node = this;
			while (node.Type() == 0x1d)
			{
				node = node.Child(0);
			}
			return node;
		}

		internal RegexNode ReduceRep()
		{
			RegexNode node = this;
			int num = this.Type();
			int num2 = this._m;
			int num3 = this._n;
			while (true)
			{
				if (node.ChildCount() == 0)
				{
					break;
				}
				RegexNode node2 = node.Child(0);
				if (node2.Type() != num)
				{
					int num4 = node2.Type();
					if ((((num4 < 3) || (num4 > 5)) || (num != 0x1a)) && (((num4 < 6) || (num4 > 8)) || (num != 0x1b)))
					{
						break;
					}
				}
				if (((node._m == 0) && (node2._m > 1)) || (node2._n < (node2._m * 2)))
				{
					break;
				}
				node = node2;
				if (node._m > 0)
				{
					node._m = num2 = ((0x7ffffffe / node._m) < num2) ? 0x7fffffff : (node._m * num2);
				}
				if (node._n > 0)
				{
					node._n = num3 = ((0x7ffffffe / node._n) < num3) ? 0x7fffffff : (node._n * num3);
				}
			}
			if (num2 != 0x7fffffff)
			{
				return node;
			}
			return new RegexNode(0x16, this._options);
		}

		internal RegexNode ReduceSet()
		{
			if (RegexCharClass.IsEmpty(this._str))
			{
				this._type = 0x16;
				this._str = null;
			}
			else if (RegexCharClass.IsSingleton(this._str))
			{
				this._ch = RegexCharClass.SingletonChar(this._str);
				this._str = null;
				this._type += -2;
			}
			else if (RegexCharClass.IsSingletonInverse(this._str))
			{
				this._ch = RegexCharClass.SingletonChar(this._str);
				this._str = null;
				this._type += -1;
			}
			return this;
		}

		internal RegexNode ReverseLeft()
		{
			if ((this.UseOptionR() && (this._type == 0x19)) && (this._children != null))
			{
				this._children.Reverse(0, this._children.Count);
			}
			return this;
		}

		internal RegexNode StripEnation(int emptyType)
		{
			switch (this.ChildCount())
			{
				case 0:
					return new RegexNode(emptyType, this._options);

				case 1:
					return this.Child(0);
			}
			return this;
		}

		internal int Type()
		{
			return this._type;
		}

		internal bool UseOptionR()
		{
			return ((this._options & RegexOptions.RightToLeft) != RegexOptions.None);
		}
	}

	internal sealed class RegexWriter
	{
		// Fields
		internal Hashtable _caps;
		internal int _count;
		internal bool _counting;
		internal int _curpos;
		internal int _depth;
		internal int[] _emitted = new int[0x20];
		internal int[] _intStack = new int[0x20];
		internal Dictionary<string, int> _stringhash = new Dictionary<string, int>();
		internal List<string> _stringtable = new List<string>();
		internal int _trackcount;
		internal const int AfterChild = 0x80;
		internal const int BeforeChild = 0x40;

		// Methods
		private RegexWriter()
		{
		}

		internal int CurPos()
		{
			return this._curpos;
		}

		internal void Emit(int op)
		{
			if (this._counting)
			{
				this._count++;
				if (RegexCode.OpcodeBacktracks(op))
				{
					this._trackcount++;
				}
			}
			else
			{
				this._emitted[this._curpos++] = op;
			}
		}

		internal void Emit(int op, int opd1)
		{
			if (this._counting)
			{
				this._count += 2;
				if (RegexCode.OpcodeBacktracks(op))
				{
					this._trackcount++;
				}
			}
			else
			{
				this._emitted[this._curpos++] = op;
				this._emitted[this._curpos++] = opd1;
			}
		}

		internal void Emit(int op, int opd1, int opd2)
		{
			if (this._counting)
			{
				this._count += 3;
				if (RegexCode.OpcodeBacktracks(op))
				{
					this._trackcount++;
				}
			}
			else
			{
				this._emitted[this._curpos++] = op;
				this._emitted[this._curpos++] = opd1;
				this._emitted[this._curpos++] = opd2;
			}
		}

		internal void EmitFragment(int nodetype, RegexNode node, int CurIndex)
		{
			int num = 0;
			if (nodetype <= 13)
			{
				if (node.UseOptionR())
				{
					num |= 0x40;
				}
				if ((node._options & RegexOptions.IgnoreCase) != RegexOptions.None)
				{
					num |= 0x200;
				}
			}
			switch (nodetype)
			{
				case 3:
				case 4:
				case 6:
				case 7:
					if (node._m > 0)
					{
						this.Emit((((node._type == 3) || (node._type == 6)) ? 0 : 1) | num, node._ch, node._m);
					}
					if (node._n > node._m)
					{
						this.Emit(node._type | num, node._ch, (node._n == 0x7fffffff) ? 0x7fffffff : (node._n - node._m));
					}
					return;

				case 5:
				case 8:
					if (node._m > 0)
					{
						this.Emit(2 | num, this.StringCode(node._str), node._m);
					}
					if (node._n > node._m)
					{
						this.Emit(node._type | num, this.StringCode(node._str), (node._n == 0x7fffffff) ? 0x7fffffff : (node._n - node._m));
					}
					return;

				case 9:
				case 10:
					this.Emit(node._type | num, node._ch);
					return;

				case 11:
					this.Emit(node._type | num, this.StringCode(node._str));
					return;

				case 12:
					this.Emit(node._type | num, this.StringCode(node._str));
					return;

				case 13:
					this.Emit(node._type | num, this.MapCapnum(node._m));
					return;

				case 14:
				case 15:
				case 0x10:
				case 0x11:
				case 0x12:
				case 0x13:
				case 20:
				case 0x15:
				case 0x16:
				case 0x29:
				case 0x2a:
					this.Emit(node._type);
					return;

				case 0x17:
				case 0x59:
				case 0x5d:
				case 0x99:
				case 0x9d:
					return;

				case 0x58:
					if (CurIndex < (node._children.Count - 1))
					{
						this.PushInt(this.CurPos());
						this.Emit(0x17, 0);
					}
					return;

				case 90:
				case 0x5b:
					if ((node._n >= 0x7fffffff) && (node._m <= 1))
					{
						this.Emit((node._m == 0) ? 30 : 0x1f);
					}
					else
					{
						this.Emit((node._m == 0) ? 0x1a : 0x1b, (node._m == 0) ? 0 : (1 - node._m));
					}
					if (node._m == 0)
					{
						this.PushInt(this.CurPos());
						this.Emit(0x26, 0);
					}
					this.PushInt(this.CurPos());
					return;

				case 0x5c:
					this.Emit(0x1f);
					return;

				case 0x5e:
					this.Emit(0x22);
					this.Emit(0x1f);
					return;

				case 0x5f:
					this.Emit(0x22);
					this.PushInt(this.CurPos());
					this.Emit(0x17, 0);
					return;

				case 0x60:
					this.Emit(0x22);
					return;

				case 0x61:
					if (CurIndex == 0)
					{
						this.Emit(0x22);
						this.PushInt(this.CurPos());
						this.Emit(0x17, 0);
						this.Emit(0x25, this.MapCapnum(node._m));
						this.Emit(0x24);
						return;
					}
					return;

				case 0x62:
					if (CurIndex == 0)
					{
						this.Emit(0x22);
						this.Emit(0x1f);
						this.PushInt(this.CurPos());
						this.Emit(0x17, 0);
						return;
					}
					return;

				case 0x98:
				{
					if (CurIndex >= (node._children.Count - 1))
					{
						for (int i = 0; i < CurIndex; i++)
						{
							this.PatchJump(this.PopInt(), this.CurPos());
						}
						return;
					}
					int offset = this.PopInt();
					this.PushInt(this.CurPos());
					this.Emit(0x26, 0);
					this.PatchJump(offset, this.CurPos());
					return;
				}
				case 0x9a:
				case 0x9b:
				{
					int jumpDest = this.CurPos();
					int num7 = nodetype - 0x9a;
					if ((node._n >= 0x7fffffff) && (node._m <= 1))
					{
						this.Emit(0x18 + num7, this.PopInt());
					}
					else
					{
						this.Emit(0x1c + num7, this.PopInt(), (node._n == 0x7fffffff) ? 0x7fffffff : (node._n - node._m));
					}
					if (node._m == 0)
					{
						this.PatchJump(this.PopInt(), jumpDest);
					}
					return;
				}
				case 0x9c:
					this.Emit(0x20, this.MapCapnum(node._m), this.MapCapnum(node._n));
					return;

				case 0x9e:
					this.Emit(0x21);
					this.Emit(0x24);
					return;

				case 0x9f:
					this.Emit(0x23);
					this.PatchJump(this.PopInt(), this.CurPos());
					this.Emit(0x24);
					return;

				case 160:
					this.Emit(0x24);
					return;

				case 0xa1:
					switch (CurIndex)
					{
						case 0:
						{
							int num4 = this.PopInt();
							this.PushInt(this.CurPos());
							this.Emit(0x26, 0);
							this.PatchJump(num4, this.CurPos());
							this.Emit(0x24);
							if (node._children.Count > 1)
							{
								return;
							}
							break;
						}
					}
					return;

				case 0xa2:
					switch (CurIndex)
					{
						case 0:
							this.Emit(0x21);
							this.Emit(0x24);
							return;

						case 1:
						{
							int num5 = this.PopInt();
							this.PushInt(this.CurPos());
							this.Emit(0x26, 0);
							this.PatchJump(num5, this.CurPos());
							this.Emit(0x21);
							this.Emit(0x24);
							if (node._children.Count > 2)
							{
								return;
							}
							goto Label_0312;
						}
						case 2:
							goto Label_0312;
					}
					return;

				default:
					throw this.MakeException("UnexpectedOpcode");
			}
			this.PatchJump(this.PopInt(), this.CurPos());
			return;
		Label_0312:
			this.PatchJump(this.PopInt(), this.CurPos());
		}

		internal bool EmptyStack()
		{
			return (this._depth == 0);
		}

		internal ArgumentException MakeException(string message)
		{
			return new ArgumentException(message);
		}

		internal int MapCapnum(int capnum)
		{
			if (capnum == -1)
			{
				return -1;
			}
			if (this._caps != null)
			{
				return (int) this._caps[capnum];
			}
			return capnum;
		}

		internal void PatchJump(int Offset, int jumpDest)
		{
			this._emitted[Offset + 1] = jumpDest;
		}

		internal int PopInt()
		{
			return this._intStack[--this._depth];
		}

		internal void PushInt(int I)
		{
			if (this._depth >= this._intStack.Length)
			{
				int[] destinationArray = new int[this._depth * 2];
				Array.Copy(this._intStack, 0, destinationArray, 0, this._depth);
				this._intStack = destinationArray;
			}
			this._intStack[this._depth++] = I;
		}

		internal RegexCode RegexCodeFromRegexTree(RegexTree tree)
		{
			int length;
			RegexBoyerMoore moore;
			if ((tree._capnumlist == null) || (tree._captop == tree._capnumlist.Length))
			{
				length = tree._captop;
				this._caps = null;
			}
			else
			{
				length = tree._capnumlist.Length;
				this._caps = tree._caps;
				for (int i = 0; i < tree._capnumlist.Length; i++)
				{
					this._caps[tree._capnumlist[i]] = i;
				}
			}
			this._counting = true;
		Label_007B:
			if (!this._counting)
			{
				this._emitted = new int[this._count];
			}
			RegexNode node = tree._root;
			int curIndex = 0;
			this.Emit(0x17, 0);
		Label_00A6:
			if (node._children == null)
			{
				this.EmitFragment(node._type, node, 0);
			}
			else if (curIndex < node._children.Count)
			{
				this.EmitFragment(node._type | 0x40, node, curIndex);
				node = node._children[curIndex];
				this.PushInt(curIndex);
				curIndex = 0;
				goto Label_00A6;
			}
			if (!this.EmptyStack())
			{
				curIndex = this.PopInt();
				node = node._next;
				this.EmitFragment(node._type | 0x80, node, curIndex);
				curIndex++;
				goto Label_00A6;
			}
			this.PatchJump(0, this.CurPos());
			this.Emit(40);
			if (this._counting)
			{
				this._counting = false;
				goto Label_007B;
			}
			RegexPrefix fcPrefix = RegexFCD.FirstChars(tree);
			RegexPrefix prefix2 = RegexFCD.Prefix(tree);
			bool rightToLeft = (tree._options & RegexOptions.RightToLeft) != RegexOptions.None;
			CultureInfo culture = ((tree._options & RegexOptions.CultureInvariant) != RegexOptions.None) ? CultureInfo.InvariantCulture : CultureInfo.CurrentCulture;
			if ((prefix2 != null) && (prefix2.Prefix.Length > 0))
			{
				moore = new RegexBoyerMoore(prefix2.Prefix, prefix2.CaseInsensitive, rightToLeft, culture);
			}
			else
			{
				moore = null;
			}
			return new RegexCode(this._emitted, this._stringtable, this._trackcount, this._caps, length, moore, fcPrefix, RegexFCD.Anchors(tree), rightToLeft);
		}

		internal int StringCode(string str)
		{
			if (this._counting)
			{
				return 0;
			}
			if (str == null)
			{
				str = string.Empty;
			}
			if (this._stringhash.ContainsKey(str))
			{
				return this._stringhash[str];
			}
			int count = this._stringtable.Count;
			this._stringhash[str] = count;
			this._stringtable.Add(str);
			return count;
		}

		internal static RegexCode Write(RegexTree t)
		{
			RegexWriter writer = new RegexWriter();
			return writer.RegexCodeFromRegexTree(t);
		}
	}

	internal abstract class RegexCompiler
	{
		// Fields
		internal int _anchors;
		internal int _backpos;
		internal Label _backtrack;
		internal RegexBoyerMoore _bmPrefix;
		internal static MethodInfo _captureM;
		internal static MethodInfo _charInSetM;
		internal static MethodInfo _chartolowerM;
		internal RegexCode _code;
		internal int _codepos;
		internal int[] _codes;
		internal static MethodInfo _crawlposM;
		internal static MethodInfo _ensurestorageM;
		internal RegexPrefix _fcPrefix;
		internal static MethodInfo _getcharM;
		internal static MethodInfo _getCurrentCulture;
		internal static MethodInfo _getInvariantCulture;
		internal int[] _goto;
		internal ILGenerator _ilg;
		internal static MethodInfo _isboundaryM;
		internal static MethodInfo _isECMABoundaryM;
		internal static MethodInfo _ismatchedM;
		internal Label[] _labels;
		internal static MethodInfo _matchindexM;
		internal static MethodInfo _matchlengthM;
		internal int _notecount;
		internal BacktrackNote[] _notes;
		internal RegexOptions _options;
		internal int _regexopcode;
		internal static FieldInfo _stackF;
		internal static FieldInfo _stackposF;
		internal LocalBuilder _stackposV;
		internal LocalBuilder _stackV;
		internal string[] _strings;
		internal LocalBuilder _temp2V;
		internal LocalBuilder _temp3V;
		internal LocalBuilder _tempV;
		internal static FieldInfo _textbegF;
		internal LocalBuilder _textbegV;
		internal static FieldInfo _textendF;
		internal LocalBuilder _textendV;
		internal static FieldInfo _textF;
		internal static FieldInfo _textposF;
		internal LocalBuilder _textposV;
		internal static FieldInfo _textstartF;
		internal LocalBuilder _textstartV;
		internal LocalBuilder _textV;
		internal int _trackcount;
		internal static FieldInfo _trackcountF;
		internal static FieldInfo _trackF;
		internal static FieldInfo _trackposF;
		internal LocalBuilder _trackposV;
		internal LocalBuilder _trackV;
		internal static MethodInfo _transferM;
		internal static MethodInfo _uncaptureM;
		internal int[] _uniquenote;
		internal const int branchcountback2 = 7;
		internal const int branchmarkback2 = 5;
		internal const int capback = 3;
		internal const int capback2 = 4;
		internal const int forejumpback = 9;
		internal const int lazybranchcountback2 = 8;
		internal const int lazybranchmarkback2 = 6;
		internal const int stackpop = 0;
		internal const int stackpop2 = 1;
		internal const int stackpop3 = 2;
		internal const int uniquecount = 10;

		// Methods
		static RegexCompiler()
		{
			new ReflectionPermission(PermissionState.Unrestricted).Assert();
			try
			{
				_textbegF = RegexRunnerField("runtextbeg");
				_textendF = RegexRunnerField("runtextend");
				_textstartF = RegexRunnerField("runtextstart");
				_textposF = RegexRunnerField("runtextpos");
				_textF = RegexRunnerField("runtext");
				_trackposF = RegexRunnerField("runtrackpos");
				_trackF = RegexRunnerField("runtrack");
				_stackposF = RegexRunnerField("runstackpos");
				_stackF = RegexRunnerField("runstack");
				_trackcountF = RegexRunnerField("runtrackcount");
				_ensurestorageM = RegexRunnerMethod("EnsureStorage");
				_captureM = RegexRunnerMethod("Capture");
				_transferM = RegexRunnerMethod("TransferCapture");
				_uncaptureM = RegexRunnerMethod("Uncapture");
				_ismatchedM = RegexRunnerMethod("IsMatched");
				_matchlengthM = RegexRunnerMethod("MatchLength");
				_matchindexM = RegexRunnerMethod("MatchIndex");
				_isboundaryM = RegexRunnerMethod("IsBoundary");
				_charInSetM = RegexRunnerMethod("CharInClass");
				_isECMABoundaryM = RegexRunnerMethod("IsECMABoundary");
				_crawlposM = RegexRunnerMethod("Crawlpos");
				_chartolowerM = typeof(char).GetMethod("ToLower", new Type[] { typeof(char), typeof(CultureInfo) });
				_getcharM = typeof(string).GetMethod("get_Chars", new Type[] { typeof(int) });
				_getCurrentCulture = typeof(CultureInfo).GetMethod("get_CurrentCulture");
				_getInvariantCulture = typeof(CultureInfo).GetMethod("get_InvariantCulture");
			}
			finally
			{
				CodeAccessPermission.RevertAssert();
			}
		}

		protected RegexCompiler()
		{
		}

		internal void Add()
		{
			this._ilg.Emit(OpCodes.Add);
		}

		internal void Add(bool negate)
		{
			if (negate)
			{
				this._ilg.Emit(OpCodes.Sub);
			}
			else
			{
				this._ilg.Emit(OpCodes.Add);
			}
		}

		internal int AddBacktrackNote(int flags, Label l, int codepos)
		{
			if ((this._notes == null) || (this._notecount >= this._notes.Length))
			{
				BacktrackNote[] destinationArray = new BacktrackNote[(this._notes == null) ? 0x10 : (this._notes.Length * 2)];
				if (this._notes != null)
				{
					Array.Copy(this._notes, 0, destinationArray, 0, this._notecount);
				}
				this._notes = destinationArray;
			}
			this._notes[this._notecount] = new BacktrackNote(flags, l, codepos);
			return this._notecount++;
		}

		internal int AddGoto(int destpos)
		{
			if (this._goto[destpos] == -1)
			{
				this._goto[destpos] = this.AddBacktrackNote(0, this._labels[destpos], destpos);
			}
			return this._goto[destpos];
		}

		internal int AddTrack()
		{
			return this.AddTrack(0x80);
		}

		internal int AddTrack(int flags)
		{
			return this.AddBacktrackNote(flags, this.DefineLabel(), this._codepos);
		}

		internal int AddUniqueTrack(int i)
		{
			return this.AddUniqueTrack(i, 0x80);
		}

		internal int AddUniqueTrack(int i, int flags)
		{
			if (this._uniquenote[i] == -1)
			{
				this._uniquenote[i] = this.AddTrack(flags);
			}
			return this._uniquenote[i];
		}

		internal void Advance()
		{
			this._ilg.Emit(OpCodes.Br, this.AdvanceLabel());
		}

		internal Label AdvanceLabel()
		{
			return this._labels[this.NextCodepos()];
		}

		internal void Back()
		{
			this._ilg.Emit(OpCodes.Br, this._backtrack);
		}

		internal void Beq(Label l)
		{
			this._ilg.Emit(OpCodes.Beq_S, l);
		}

		internal void BeqFar(Label l)
		{
			this._ilg.Emit(OpCodes.Beq, l);
		}

		internal void Bge(Label l)
		{
			this._ilg.Emit(OpCodes.Bge_S, l);
		}

		internal void BgeFar(Label l)
		{
			this._ilg.Emit(OpCodes.Bge, l);
		}

		internal void Bgt(Label l)
		{
			this._ilg.Emit(OpCodes.Bgt_S, l);
		}

		internal void BgtFar(Label l)
		{
			this._ilg.Emit(OpCodes.Bgt, l);
		}

		internal void Bgtun(Label l)
		{
			this._ilg.Emit(OpCodes.Bgt_Un_S, l);
		}

		internal void Ble(Label l)
		{
			this._ilg.Emit(OpCodes.Ble_S, l);
		}

		internal void BleFar(Label l)
		{
			this._ilg.Emit(OpCodes.Ble, l);
		}

		internal void Blt(Label l)
		{
			this._ilg.Emit(OpCodes.Blt_S, l);
		}

		internal void BltFar(Label l)
		{
			this._ilg.Emit(OpCodes.Blt, l);
		}

		internal void Bne(Label l)
		{
			this._ilg.Emit(OpCodes.Bne_Un_S, l);
		}

		internal void BneFar(Label l)
		{
			this._ilg.Emit(OpCodes.Bne_Un, l);
		}

		internal void Br(Label l)
		{
			this._ilg.Emit(OpCodes.Br_S, l);
		}

		internal void Brfalse(Label l)
		{
			this._ilg.Emit(OpCodes.Brfalse_S, l);
		}

		internal void BrfalseFar(Label l)
		{
			this._ilg.Emit(OpCodes.Brfalse, l);
		}

		internal void BrFar(Label l)
		{
			this._ilg.Emit(OpCodes.Br, l);
		}

		internal void BrtrueFar(Label l)
		{
			this._ilg.Emit(OpCodes.Brtrue, l);
		}

		internal void Call(MethodInfo mt)
		{
			this._ilg.Emit(OpCodes.Call, mt);
		}

		internal void CallToLower()
		{
			if ((this._options & RegexOptions.CultureInvariant) != RegexOptions.None)
			{
				this.Call(_getInvariantCulture);
			}
			else
			{
				this.Call(_getCurrentCulture);
			}
			this.Call(_chartolowerM);
		}

		internal void Callvirt(MethodInfo mt)
		{
			this._ilg.Emit(OpCodes.Callvirt, mt);
		}

		internal int Code()
		{
			return (this._regexopcode & 0x3f);
		}

		internal static RegexRunnerFactory Compile(RegexCode code, RegexOptions options)
		{
			RegexRunnerFactory factory;
			RegexLWCGCompiler compiler = new RegexLWCGCompiler();
			new ReflectionPermission(PermissionState.Unrestricted).Assert();
			try
			{
				factory = compiler.FactoryInstanceFromCode(code, options);
			}
			finally
			{
				CodeAccessPermission.RevertAssert();
			}
			return factory;
		}

		internal static void CompileToAssembly(RegexCompilationInfo[] regexes, AssemblyName an, CustomAttributeBuilder[] attribs, string resourceFile)
		{
			RegexTypeCompiler compiler = new RegexTypeCompiler(an, attribs, resourceFile);
			for (int i = 0; i < regexes.Length; i++)
			{
				string name;
				if (regexes[i] == null)
				{
					throw new ArgumentNullException("regexes");
				}
				string pattern = regexes[i].Pattern;
				RegexOptions op = regexes[i].Options;
				if (regexes[i].Namespace.Length == 0)
				{
					name = regexes[i].Name;
				}
				else
				{
					name = regexes[i].Namespace + "." + regexes[i].Name;
				}
				RegexTree t = RegexParser.Parse(pattern, op);
				RegexCode code = RegexWriter.Write(t);
				new ReflectionPermission(PermissionState.Unrestricted).Assert();
				try
				{
					Type factory = compiler.FactoryTypeFromCode(code, op, name);
					compiler.GenerateRegexType(pattern, op, name, regexes[i].IsPublic, code, t, factory);
				}
				finally
				{
					CodeAccessPermission.RevertAssert();
				}
			}
			compiler.Save();
		}

		internal LocalBuilder DeclareInt()
		{
			return this._ilg.DeclareLocal(typeof(int));
		}

		internal LocalBuilder DeclareIntArray()
		{
			return this._ilg.DeclareLocal(typeof(int[]));
		}

		internal LocalBuilder DeclareString()
		{
			return this._ilg.DeclareLocal(typeof(string));
		}

		internal Label DefineLabel()
		{
			return this._ilg.DefineLabel();
		}

		internal void DoPush()
		{
			this._ilg.Emit(OpCodes.Stelem_I4);
		}

		internal void DoReplace()
		{
			this._ilg.Emit(OpCodes.Stelem_I4);
		}

		internal void Dup()
		{
			this._ilg.Emit(OpCodes.Dup);
		}

		internal void GenerateBacktrackSection()
		{
			for (int i = 0; i < this._notecount; i++)
			{
				BacktrackNote note = this._notes[i];
				if (note._flags != 0)
				{
					this._ilg.MarkLabel(note._label);
					this._codepos = note._codepos;
					this._backpos = i;
					this._regexopcode = this._codes[note._codepos] | note._flags;
					this.GenerateOneCode();
				}
			}
		}

		internal void GenerateFindFirstChar()
		{
			this._textposV = this.DeclareInt();
			this._textV = this.DeclareString();
			this._tempV = this.DeclareInt();
			this._temp2V = this.DeclareInt();
			if ((this._anchors & 0x35) != 0)
			{
				if (!this._code._rightToLeft)
				{
					if ((this._anchors & 1) != 0)
					{
						Label l = this.DefineLabel();
						this.Ldthisfld(_textposF);
						this.Ldthisfld(_textbegF);
						this.Ble(l);
						this.Ldthis();
						this.Ldthisfld(_textendF);
						this.Stfld(_textposF);
						this.Ldc(0);
						this.Ret();
						this.MarkLabel(l);
					}
					if ((this._anchors & 4) != 0)
					{
						Label label2 = this.DefineLabel();
						this.Ldthisfld(_textposF);
						this.Ldthisfld(_textstartF);
						this.Ble(label2);
						this.Ldthis();
						this.Ldthisfld(_textendF);
						this.Stfld(_textposF);
						this.Ldc(0);
						this.Ret();
						this.MarkLabel(label2);
					}
					if ((this._anchors & 0x10) != 0)
					{
						Label label3 = this.DefineLabel();
						this.Ldthisfld(_textposF);
						this.Ldthisfld(_textendF);
						this.Ldc(1);
						this.Sub();
						this.Bge(label3);
						this.Ldthis();
						this.Ldthisfld(_textendF);
						this.Ldc(1);
						this.Sub();
						this.Stfld(_textposF);
						this.MarkLabel(label3);
					}
					if ((this._anchors & 0x20) != 0)
					{
						Label label4 = this.DefineLabel();
						this.Ldthisfld(_textposF);
						this.Ldthisfld(_textendF);
						this.Bge(label4);
						this.Ldthis();
						this.Ldthisfld(_textendF);
						this.Stfld(_textposF);
						this.MarkLabel(label4);
					}
				}
				else
				{
					if ((this._anchors & 0x20) != 0)
					{
						Label label5 = this.DefineLabel();
						this.Ldthisfld(_textposF);
						this.Ldthisfld(_textendF);
						this.Bge(label5);
						this.Ldthis();
						this.Ldthisfld(_textbegF);
						this.Stfld(_textposF);
						this.Ldc(0);
						this.Ret();
						this.MarkLabel(label5);
					}
					if ((this._anchors & 0x10) != 0)
					{
						Label label6 = this.DefineLabel();
						Label label7 = this.DefineLabel();
						this.Ldthisfld(_textposF);
						this.Ldthisfld(_textendF);
						this.Ldc(1);
						this.Sub();
						this.Blt(label6);
						this.Ldthisfld(_textposF);
						this.Ldthisfld(_textendF);
						this.Beq(label7);
						this.Ldthisfld(_textF);
						this.Ldthisfld(_textposF);
						this.Callvirt(_getcharM);
						this.Ldc(10);
						this.Beq(label7);
						this.MarkLabel(label6);
						this.Ldthis();
						this.Ldthisfld(_textbegF);
						this.Stfld(_textposF);
						this.Ldc(0);
						this.Ret();
						this.MarkLabel(label7);
					}
					if ((this._anchors & 4) != 0)
					{
						Label label8 = this.DefineLabel();
						this.Ldthisfld(_textposF);
						this.Ldthisfld(_textstartF);
						this.Bge(label8);
						this.Ldthis();
						this.Ldthisfld(_textbegF);
						this.Stfld(_textposF);
						this.Ldc(0);
						this.Ret();
						this.MarkLabel(label8);
					}
					if ((this._anchors & 1) != 0)
					{
						Label label9 = this.DefineLabel();
						this.Ldthisfld(_textposF);
						this.Ldthisfld(_textbegF);
						this.Ble(label9);
						this.Ldthis();
						this.Ldthisfld(_textbegF);
						this.Stfld(_textposF);
						this.MarkLabel(label9);
					}
				}
				this.Ldc(1);
				this.Ret();
			}
			else if ((this._bmPrefix != null) && (this._bmPrefix._negativeUnicode == null))
			{
				int num2;
				int length;
				int num4;
				LocalBuilder lt = this._tempV;
				LocalBuilder builder2 = this._tempV;
				LocalBuilder builder3 = this._temp2V;
				Label label10 = this.DefineLabel();
				Label label11 = this.DefineLabel();
				Label label12 = this.DefineLabel();
				Label label13 = this.DefineLabel();
				this.DefineLabel();
				Label label14 = this.DefineLabel();
				if (!this._code._rightToLeft)
				{
					length = -1;
					num4 = this._bmPrefix._pattern.Length - 1;
				}
				else
				{
					length = this._bmPrefix._pattern.Length;
					num4 = 0;
				}
				int i = this._bmPrefix._pattern[num4];
				this.Mvfldloc(_textF, this._textV);
				if (!this._code._rightToLeft)
				{
					this.Ldthisfld(_textendF);
				}
				else
				{
					this.Ldthisfld(_textbegF);
				}
				this.Stloc(builder3);
				this.Ldthisfld(_textposF);
				if (!this._code._rightToLeft)
				{
					this.Ldc(this._bmPrefix._pattern.Length - 1);
					this.Add();
				}
				else
				{
					this.Ldc(this._bmPrefix._pattern.Length);
					this.Sub();
				}
				this.Stloc(this._textposV);
				this.Br(label13);
				this.MarkLabel(label10);
				if (!this._code._rightToLeft)
				{
					this.Ldc(this._bmPrefix._pattern.Length);
				}
				else
				{
					this.Ldc(-this._bmPrefix._pattern.Length);
				}
				this.MarkLabel(label11);
				this.Ldloc(this._textposV);
				this.Add();
				this.Stloc(this._textposV);
				this.MarkLabel(label13);
				this.Ldloc(this._textposV);
				this.Ldloc(builder3);
				if (!this._code._rightToLeft)
				{
					this.BgeFar(label12);
				}
				else
				{
					this.BltFar(label12);
				}
				this.Rightchar();
				if (this._bmPrefix._caseInsensitive)
				{
					this.CallToLower();
				}
				this.Dup();
				this.Stloc(lt);
				this.Ldc(i);
				this.BeqFar(label14);
				this.Ldloc(lt);
				this.Ldc(this._bmPrefix._lowASCII);
				this.Sub();
				this.Dup();
				this.Stloc(lt);
				this.Ldc(this._bmPrefix._highASCII - this._bmPrefix._lowASCII);
				this.Bgtun(label10);
				Label[] labels = new Label[(this._bmPrefix._highASCII - this._bmPrefix._lowASCII) + 1];
				for (num2 = this._bmPrefix._lowASCII; num2 <= this._bmPrefix._highASCII; num2++)
				{
					if (this._bmPrefix._negativeASCII[num2] == length)
					{
						labels[num2 - this._bmPrefix._lowASCII] = label10;
					}
					else
					{
						labels[num2 - this._bmPrefix._lowASCII] = this.DefineLabel();
					}
				}
				this.Ldloc(lt);
				this._ilg.Emit(OpCodes.Switch, labels);
				for (num2 = this._bmPrefix._lowASCII; num2 <= this._bmPrefix._highASCII; num2++)
				{
					if (this._bmPrefix._negativeASCII[num2] != length)
					{
						this.MarkLabel(labels[num2 - this._bmPrefix._lowASCII]);
						this.Ldc(this._bmPrefix._negativeASCII[num2]);
						this.BrFar(label11);
					}
				}
				this.MarkLabel(label14);
				this.Ldloc(this._textposV);
				this.Stloc(builder2);
				for (num2 = this._bmPrefix._pattern.Length - 2; num2 >= 0; num2--)
				{
					int num5;
					Label label15 = this.DefineLabel();
					if (!this._code._rightToLeft)
					{
						num5 = num2;
					}
					else
					{
						num5 = (this._bmPrefix._pattern.Length - 1) - num2;
					}
					this.Ldloc(this._textV);
					this.Ldloc(builder2);
					this.Ldc(1);
					this.Sub(this._code._rightToLeft);
					this.Dup();
					this.Stloc(builder2);
					this.Callvirt(_getcharM);
					if (this._bmPrefix._caseInsensitive)
					{
						this.CallToLower();
					}
					this.Ldc(this._bmPrefix._pattern[num5]);
					this.Beq(label15);
					this.Ldc(this._bmPrefix._positive[num5]);
					this.BrFar(label11);
					this.MarkLabel(label15);
				}
				this.Ldthis();
				this.Ldloc(builder2);
				if (this._code._rightToLeft)
				{
					this.Ldc(1);
					this.Add();
				}
				this.Stfld(_textposF);
				this.Ldc(1);
				this.Ret();
				this.MarkLabel(label12);
				this.Ldthis();
				if (!this._code._rightToLeft)
				{
					this.Ldthisfld(_textendF);
				}
				else
				{
					this.Ldthisfld(_textbegF);
				}
				this.Stfld(_textposF);
				this.Ldc(0);
				this.Ret();
			}
			else if (this._fcPrefix == null)
			{
				this.Ldc(1);
				this.Ret();
			}
			else
			{
				LocalBuilder builder4 = this._temp2V;
				Label label16 = this.DefineLabel();
				Label label17 = this.DefineLabel();
				Label label18 = this.DefineLabel();
				Label label19 = this.DefineLabel();
				Label label20 = this.DefineLabel();
				this.Mvfldloc(_textposF, this._textposV);
				this.Mvfldloc(_textF, this._textV);
				if (!this._code._rightToLeft)
				{
					this.Ldthisfld(_textendF);
					this.Ldloc(this._textposV);
				}
				else
				{
					this.Ldloc(this._textposV);
					this.Ldthisfld(_textbegF);
				}
				this.Sub();
				this.Stloc(builder4);
				this.Ldloc(builder4);
				this.Ldc(0);
				this.BleFar(label19);
				this.MarkLabel(label16);
				this.Ldloc(builder4);
				this.Ldc(1);
				this.Sub();
				this.Stloc(builder4);
				if (this._code._rightToLeft)
				{
					this.Leftcharnext();
				}
				else
				{
					this.Rightcharnext();
				}
				if (this._fcPrefix.CaseInsensitive)
				{
					this.CallToLower();
				}
				if (!RegexCharClass.IsSingleton(this._fcPrefix.Prefix))
				{
					this.Ldstr(this._fcPrefix.Prefix);
					this.Call(_charInSetM);
					this.BrtrueFar(label17);
				}
				else
				{
					this.Ldc(RegexCharClass.SingletonChar(this._fcPrefix.Prefix));
					this.Beq(label17);
				}
				this.MarkLabel(label20);
				this.Ldloc(builder4);
				this.Ldc(0);
				if (!RegexCharClass.IsSingleton(this._fcPrefix.Prefix))
				{
					this.BgtFar(label16);
				}
				else
				{
					this.Bgt(label16);
				}
				this.Ldc(0);
				this.BrFar(label18);
				this.MarkLabel(label17);
				this.Ldloc(this._textposV);
				this.Ldc(1);
				this.Sub(this._code._rightToLeft);
				this.Stloc(this._textposV);
				this.Ldc(1);
				this.MarkLabel(label18);
				this.Mvlocfld(this._textposV, _textposF);
				this.Ret();
				this.MarkLabel(label19);
				this.Ldc(0);
				this.Ret();
			}
		}

		internal void GenerateForwardSection()
		{
			int num;
			this._labels = new Label[this._codes.Length];
			this._goto = new int[this._codes.Length];
			for (num = 0; num < this._codes.Length; num += RegexCode.OpcodeSize(this._codes[num]))
			{
				this._goto[num] = -1;
				this._labels[num] = this._ilg.DefineLabel();
			}
			this._uniquenote = new int[10];
			for (int i = 0; i < 10; i++)
			{
				this._uniquenote[i] = -1;
			}
			this.Mvfldloc(_textF, this._textV);
			this.Mvfldloc(_textstartF, this._textstartV);
			this.Mvfldloc(_textbegF, this._textbegV);
			this.Mvfldloc(_textendF, this._textendV);
			this.Mvfldloc(_textposF, this._textposV);
			this.Mvfldloc(_trackF, this._trackV);
			this.Mvfldloc(_trackposF, this._trackposV);
			this.Mvfldloc(_stackF, this._stackV);
			this.Mvfldloc(_stackposF, this._stackposV);
			this._backpos = -1;
			for (num = 0; num < this._codes.Length; num += RegexCode.OpcodeSize(this._codes[num]))
			{
				this.MarkLabel(this._labels[num]);
				this._codepos = num;
				this._regexopcode = this._codes[num];
				this.GenerateOneCode();
			}
		}

		internal void GenerateGo()
		{
			this._textposV = this.DeclareInt();
			this._textV = this.DeclareString();
			this._trackposV = this.DeclareInt();
			this._trackV = this.DeclareIntArray();
			this._stackposV = this.DeclareInt();
			this._stackV = this.DeclareIntArray();
			this._tempV = this.DeclareInt();
			this._temp2V = this.DeclareInt();
			this._temp3V = this.DeclareInt();
			this._textbegV = this.DeclareInt();
			this._textendV = this.DeclareInt();
			this._textstartV = this.DeclareInt();
			this._labels = null;
			this._notes = null;
			this._notecount = 0;
			this._backtrack = this.DefineLabel();
			this.GenerateForwardSection();
			this.GenerateMiddleSection();
			this.GenerateBacktrackSection();
		}

		internal void GenerateInitTrackCount()
		{
			this.Ldthis();
			this.Ldc(this._trackcount);
			this.Stfld(_trackcountF);
			this.Ret();
		}

		internal void GenerateMiddleSection()
		{
			this.DefineLabel();
			this.MarkLabel(this._backtrack);
			this.Mvlocfld(this._trackposV, _trackposF);
			this.Mvlocfld(this._stackposV, _stackposF);
			this.Ldthis();
			this.Callvirt(_ensurestorageM);
			this.Mvfldloc(_trackposF, this._trackposV);
			this.Mvfldloc(_stackposF, this._stackposV);
			this.Mvfldloc(_trackF, this._trackV);
			this.Mvfldloc(_stackF, this._stackV);
			this.PopTrack();
			Label[] labels = new Label[this._notecount];
			for (int i = 0; i < this._notecount; i++)
			{
				labels[i] = this._notes[i]._label;
			}
			this._ilg.Emit(OpCodes.Switch, labels);
		}

		internal void GenerateOneCode()
		{
			switch (this._regexopcode)
			{
				case 0:
				case 1:
				case 2:
				case 0x40:
				case 0x41:
				case 0x42:
				case 0x200:
				case 0x201:
				case 0x202:
				case 0x240:
				case 0x241:
				case 0x242:
				{
					LocalBuilder lt = this._tempV;
					Label l = this.DefineLabel();
					int i = this.Operand(1);
					if (i != 0)
					{
						this.Ldc(i);
						if (!this.IsRtl())
						{
							this.Ldloc(this._textendV);
							this.Ldloc(this._textposV);
						}
						else
						{
							this.Ldloc(this._textposV);
							this.Ldloc(this._textbegV);
						}
						this.Sub();
						this.BgtFar(this._backtrack);
						this.Ldloc(this._textposV);
						this.Ldc(i);
						this.Add(this.IsRtl());
						this.Stloc(this._textposV);
						this.Ldc(i);
						this.Stloc(lt);
						this.MarkLabel(l);
						this.Ldloc(this._textV);
						this.Ldloc(this._textposV);
						this.Ldloc(lt);
						if (this.IsRtl())
						{
							this.Ldc(1);
							this.Sub();
							this.Dup();
							this.Stloc(lt);
							this.Add();
						}
						else
						{
							this.Dup();
							this.Ldc(1);
							this.Sub();
							this.Stloc(lt);
							this.Sub();
						}
						this.Callvirt(_getcharM);
						if (this.IsCi())
						{
							this.CallToLower();
						}
						if (this.Code() == 2)
						{
							this.Ldstr(this._strings[this.Operand(0)]);
							this.Call(_charInSetM);
							this.BrfalseFar(this._backtrack);
						}
						else
						{
							this.Ldc(this.Operand(0));
							if (this.Code() == 0)
							{
								this.BneFar(this._backtrack);
							}
							else
							{
								this.BeqFar(this._backtrack);
							}
						}
						this.Ldloc(lt);
						this.Ldc(0);
						if (this.Code() == 2)
						{
							this.BgtFar(l);
							return;
						}
						this.Bgt(l);
					}
					return;
				}
				case 3:
				case 4:
				case 5:
				case 0x43:
				case 0x44:
				case 0x45:
				case 0x203:
				case 0x204:
				case 0x205:
				case 0x243:
				case 580:
				case 0x245:
				{
					LocalBuilder builder12 = this._tempV;
					LocalBuilder builder13 = this._temp2V;
					Label label18 = this.DefineLabel();
					Label label19 = this.DefineLabel();
					int num4 = this.Operand(1);
					if (num4 != 0)
					{
						if (!this.IsRtl())
						{
							this.Ldloc(this._textendV);
							this.Ldloc(this._textposV);
						}
						else
						{
							this.Ldloc(this._textposV);
							this.Ldloc(this._textbegV);
						}
						this.Sub();
						if (num4 != 0x7fffffff)
						{
							Label label20 = this.DefineLabel();
							this.Dup();
							this.Ldc(num4);
							this.Blt(label20);
							this.Pop();
							this.Ldc(num4);
							this.MarkLabel(label20);
						}
						this.Dup();
						this.Stloc(builder13);
						this.Ldc(1);
						this.Add();
						this.Stloc(builder12);
						this.MarkLabel(label18);
						this.Ldloc(builder12);
						this.Ldc(1);
						this.Sub();
						this.Dup();
						this.Stloc(builder12);
						this.Ldc(0);
						if (this.Code() == 5)
						{
							this.BleFar(label19);
						}
						else
						{
							this.Ble(label19);
						}
						if (this.IsRtl())
						{
							this.Leftcharnext();
						}
						else
						{
							this.Rightcharnext();
						}
						if (this.IsCi())
						{
							this.CallToLower();
						}
						if (this.Code() == 5)
						{
							this.Ldstr(this._strings[this.Operand(0)]);
							this.Call(_charInSetM);
							this.BrtrueFar(label18);
						}
						else
						{
							this.Ldc(this.Operand(0));
							if (this.Code() == 3)
							{
								this.Beq(label18);
							}
							else
							{
								this.Bne(label18);
							}
						}
						this.Ldloc(this._textposV);
						this.Ldc(1);
						this.Sub(this.IsRtl());
						this.Stloc(this._textposV);
						this.MarkLabel(label19);
						this.Ldloc(builder13);
						this.Ldloc(builder12);
						this.Ble(this.AdvanceLabel());
						this.ReadyPushTrack();
						this.Ldloc(builder13);
						this.Ldloc(builder12);
						this.Sub();
						this.Ldc(1);
						this.Sub();
						this.DoPush();
						this.ReadyPushTrack();
						this.Ldloc(this._textposV);
						this.Ldc(1);
						this.Sub(this.IsRtl());
						this.DoPush();
						this.Track();
					}
					return;
				}
				case 6:
				case 7:
				case 8:
				case 70:
				case 0x47:
				case 0x48:
				case 0x206:
				case 0x207:
				case 520:
				case 0x246:
				case 0x247:
				case 0x248:
				{
					LocalBuilder builder14 = this._tempV;
					int num5 = this.Operand(1);
					if (num5 != 0)
					{
						if (!this.IsRtl())
						{
							this.Ldloc(this._textendV);
							this.Ldloc(this._textposV);
						}
						else
						{
							this.Ldloc(this._textposV);
							this.Ldloc(this._textbegV);
						}
						this.Sub();
						if (num5 != 0x7fffffff)
						{
							Label label21 = this.DefineLabel();
							this.Dup();
							this.Ldc(num5);
							this.Blt(label21);
							this.Pop();
							this.Ldc(num5);
							this.MarkLabel(label21);
						}
						this.Dup();
						this.Stloc(builder14);
						this.Ldc(0);
						this.Ble(this.AdvanceLabel());
						this.ReadyPushTrack();
						this.Ldloc(builder14);
						this.Ldc(1);
						this.Sub();
						this.DoPush();
						this.PushTrack(this._textposV);
						this.Track();
					}
					return;
				}
				case 9:
				case 10:
				case 11:
				case 0x49:
				case 0x4a:
				case 0x4b:
				case 0x209:
				case 0x20a:
				case 0x20b:
				case 0x249:
				case 0x24a:
				case 0x24b:
					this.Ldloc(this._textposV);
					if (!this.IsRtl())
					{
						this.Ldloc(this._textendV);
						this.BgeFar(this._backtrack);
						this.Rightcharnext();
					}
					else
					{
						this.Ldloc(this._textbegV);
						this.BleFar(this._backtrack);
						this.Leftcharnext();
					}
					if (this.IsCi())
					{
						this.CallToLower();
					}
					if (this.Code() == 11)
					{
						this.Ldstr(this._strings[this.Operand(0)]);
						this.Call(_charInSetM);
						this.BrfalseFar(this._backtrack);
						return;
					}
					this.Ldc(this.Operand(0));
					if (this.Code() == 9)
					{
						this.BneFar(this._backtrack);
						return;
					}
					this.BeqFar(this._backtrack);
					return;

				case 12:
				case 0x20c:
				{
					string str = this._strings[this.Operand(0)];
					this.Ldc(str.Length);
					this.Ldloc(this._textendV);
					this.Ldloc(this._textposV);
					this.Sub();
					this.BgtFar(this._backtrack);
					for (int j = 0; j < str.Length; j++)
					{
						this.Ldloc(this._textV);
						this.Ldloc(this._textposV);
						if (j != 0)
						{
							this.Ldc(j);
							this.Add();
						}
						this.Callvirt(_getcharM);
						if (this.IsCi())
						{
							this.CallToLower();
						}
						this.Ldc(str[j]);
						this.BneFar(this._backtrack);
					}
					this.Ldloc(this._textposV);
					this.Ldc(str.Length);
					this.Add();
					this.Stloc(this._textposV);
					return;
				}
				case 13:
				case 0x4d:
				case 0x20d:
				case 0x24d:
				{
					LocalBuilder builder9 = this._tempV;
					LocalBuilder builder10 = this._temp2V;
					Label label16 = this.DefineLabel();
					this.Ldthis();
					this.Ldc(this.Operand(0));
					this.Callvirt(_ismatchedM);
					if ((this._options & RegexOptions.ECMAScript) != RegexOptions.None)
					{
						this.Brfalse(this.AdvanceLabel());
					}
					else
					{
						this.BrfalseFar(this._backtrack);
					}
					this.Ldthis();
					this.Ldc(this.Operand(0));
					this.Callvirt(_matchlengthM);
					this.Dup();
					this.Stloc(builder9);
					if (!this.IsRtl())
					{
						this.Ldloc(this._textendV);
						this.Ldloc(this._textposV);
					}
					else
					{
						this.Ldloc(this._textposV);
						this.Ldloc(this._textbegV);
					}
					this.Sub();
					this.BgtFar(this._backtrack);
					this.Ldthis();
					this.Ldc(this.Operand(0));
					this.Callvirt(_matchindexM);
					if (!this.IsRtl())
					{
						this.Ldloc(builder9);
						this.Add(this.IsRtl());
					}
					this.Stloc(builder10);
					this.Ldloc(this._textposV);
					this.Ldloc(builder9);
					this.Add(this.IsRtl());
					this.Stloc(this._textposV);
					this.MarkLabel(label16);
					this.Ldloc(builder9);
					this.Ldc(0);
					this.Ble(this.AdvanceLabel());
					this.Ldloc(this._textV);
					this.Ldloc(builder10);
					this.Ldloc(builder9);
					if (this.IsRtl())
					{
						this.Ldc(1);
						this.Sub();
						this.Dup();
						this.Stloc(builder9);
					}
					this.Sub(this.IsRtl());
					this.Callvirt(_getcharM);
					if (this.IsCi())
					{
						this.CallToLower();
					}
					this.Ldloc(this._textV);
					this.Ldloc(this._textposV);
					this.Ldloc(builder9);
					if (!this.IsRtl())
					{
						this.Dup();
						this.Ldc(1);
						this.Sub();
						this.Stloc(builder9);
					}
					this.Sub(this.IsRtl());
					this.Callvirt(_getcharM);
					if (this.IsCi())
					{
						this.CallToLower();
					}
					this.Beq(label16);
					this.Back();
					return;
				}
				case 14:
				{
					Label label14 = this._labels[this.NextCodepos()];
					this.Ldloc(this._textposV);
					this.Ldloc(this._textbegV);
					this.Ble(label14);
					this.Leftchar();
					this.Ldc(10);
					this.BneFar(this._backtrack);
					return;
				}
				case 15:
				{
					Label label15 = this._labels[this.NextCodepos()];
					this.Ldloc(this._textposV);
					this.Ldloc(this._textendV);
					this.Bge(label15);
					this.Rightchar();
					this.Ldc(10);
					this.BneFar(this._backtrack);
					return;
				}
				case 0x10:
				case 0x11:
					this.Ldthis();
					this.Ldloc(this._textposV);
					this.Ldloc(this._textbegV);
					this.Ldloc(this._textendV);
					this.Callvirt(_isboundaryM);
					if (this.Code() != 0x10)
					{
						this.BrtrueFar(this._backtrack);
						return;
					}
					this.BrfalseFar(this._backtrack);
					return;

				case 0x12:
					this.Ldloc(this._textposV);
					this.Ldloc(this._textbegV);
					this.BgtFar(this._backtrack);
					return;

				case 0x13:
					this.Ldloc(this._textposV);
					this.Ldthisfld(_textstartF);
					this.BneFar(this._backtrack);
					return;

				case 20:
					this.Ldloc(this._textposV);
					this.Ldloc(this._textendV);
					this.Ldc(1);
					this.Sub();
					this.BltFar(this._backtrack);
					this.Ldloc(this._textposV);
					this.Ldloc(this._textendV);
					this.Bge(this._labels[this.NextCodepos()]);
					this.Rightchar();
					this.Ldc(10);
					this.BneFar(this._backtrack);
					return;

				case 0x15:
					this.Ldloc(this._textposV);
					this.Ldloc(this._textendV);
					this.BltFar(this._backtrack);
					return;

				case 0x16:
					this.Back();
					return;

				case 0x17:
					this.PushTrack(this._textposV);
					this.Track();
					return;

				case 0x18:
				{
					LocalBuilder builder = this._tempV;
					Label label = this.DefineLabel();
					this.PopStack();
					this.Dup();
					this.Stloc(builder);
					this.PushTrack(builder);
					this.Ldloc(this._textposV);
					this.Beq(label);
					this.PushTrack(this._textposV);
					this.PushStack(this._textposV);
					this.Track();
					this.Goto(this.Operand(0));
					this.MarkLabel(label);
					this.TrackUnique2(5);
					return;
				}
				case 0x19:
				{
					LocalBuilder builder2 = this._tempV;
					Label label2 = this.DefineLabel();
					Label label3 = this.DefineLabel();
					Label label4 = this.DefineLabel();
					this.PopStack();
					this.Dup();
					this.Stloc(builder2);
					this.Ldloc(builder2);
					this.Ldc(-1);
					this.Beq(label3);
					this.PushTrack(builder2);
					this.Br(label4);
					this.MarkLabel(label3);
					this.PushTrack(this._textposV);
					this.MarkLabel(label4);
					this.Ldloc(this._textposV);
					this.Beq(label2);
					this.PushTrack(this._textposV);
					this.Track();
					this.Br(this.AdvanceLabel());
					this.MarkLabel(label2);
					this.ReadyPushStack();
					this.Ldloc(builder2);
					this.DoPush();
					this.TrackUnique2(6);
					return;
				}
				case 0x1a:
					this.ReadyPushStack();
					this.Ldc(-1);
					this.DoPush();
					this.ReadyPushStack();
					this.Ldc(this.Operand(0));
					this.DoPush();
					this.TrackUnique(1);
					return;

				case 0x1b:
					this.PushStack(this._textposV);
					this.ReadyPushStack();
					this.Ldc(this.Operand(0));
					this.DoPush();
					this.TrackUnique(1);
					return;

				case 0x1c:
				{
					LocalBuilder builder3 = this._tempV;
					LocalBuilder builder4 = this._temp2V;
					Label label5 = this.DefineLabel();
					Label label6 = this.DefineLabel();
					this.PopStack();
					this.Stloc(builder3);
					this.PopStack();
					this.Dup();
					this.Stloc(builder4);
					this.PushTrack(builder4);
					this.Ldloc(this._textposV);
					this.Bne(label5);
					this.Ldloc(builder3);
					this.Ldc(0);
					this.Bge(label6);
					this.MarkLabel(label5);
					this.Ldloc(builder3);
					this.Ldc(this.Operand(1));
					this.Bge(label6);
					this.PushStack(this._textposV);
					this.ReadyPushStack();
					this.Ldloc(builder3);
					this.Ldc(1);
					this.Add();
					this.DoPush();
					this.Track();
					this.Goto(this.Operand(0));
					this.MarkLabel(label6);
					this.PushTrack(builder3);
					this.TrackUnique2(7);
					return;
				}
				case 0x1d:
				{
					LocalBuilder builder6 = this._tempV;
					LocalBuilder builder7 = this._temp2V;
					Label label8 = this.DefineLabel();
					this.DefineLabel();
					Label label1 = this._labels[this.NextCodepos()];
					this.PopStack();
					this.Stloc(builder6);
					this.PopStack();
					this.Stloc(builder7);
					this.Ldloc(builder6);
					this.Ldc(0);
					this.Bge(label8);
					this.PushTrack(builder7);
					this.PushStack(this._textposV);
					this.ReadyPushStack();
					this.Ldloc(builder6);
					this.Ldc(1);
					this.Add();
					this.DoPush();
					this.TrackUnique2(8);
					this.Goto(this.Operand(0));
					this.MarkLabel(label8);
					this.PushTrack(builder7);
					this.PushTrack(builder6);
					this.PushTrack(this._textposV);
					this.Track();
					return;
				}
				case 30:
					this.ReadyPushStack();
					this.Ldc(-1);
					this.DoPush();
					this.TrackUnique(0);
					return;

				case 0x1f:
					this.PushStack(this._textposV);
					this.TrackUnique(0);
					return;

				case 0x20:
					if (this.Operand(1) != -1)
					{
						this.Ldthis();
						this.Ldc(this.Operand(1));
						this.Callvirt(_ismatchedM);
						this.BrfalseFar(this._backtrack);
					}
					this.PopStack();
					this.Stloc(this._tempV);
					if (this.Operand(1) != -1)
					{
						this.Ldthis();
						this.Ldc(this.Operand(0));
						this.Ldc(this.Operand(1));
						this.Ldloc(this._tempV);
						this.Ldloc(this._textposV);
						this.Callvirt(_transferM);
					}
					else
					{
						this.Ldthis();
						this.Ldc(this.Operand(0));
						this.Ldloc(this._tempV);
						this.Ldloc(this._textposV);
						this.Callvirt(_captureM);
					}
					this.PushTrack(this._tempV);
					if ((this.Operand(0) != -1) && (this.Operand(1) != -1))
					{
						this.TrackUnique(4);
						return;
					}
					this.TrackUnique(3);
					return;

				case 0x21:
					this.ReadyPushTrack();
					this.PopStack();
					this.Dup();
					this.Stloc(this._textposV);
					this.DoPush();
					this.Track();
					return;

				case 0x22:
					this.ReadyPushStack();
					this.Ldthisfld(_trackF);
					this.Ldlen();
					this.Ldloc(this._trackposV);
					this.Sub();
					this.DoPush();
					this.ReadyPushStack();
					this.Ldthis();
					this.Callvirt(_crawlposM);
					this.DoPush();
					this.TrackUnique(1);
					return;

				case 0x23:
				{
					Label label10 = this.DefineLabel();
					Label label11 = this.DefineLabel();
					this.PopStack();
					this.Ldthisfld(_trackF);
					this.Ldlen();
					this.PopStack();
					this.Sub();
					this.Stloc(this._trackposV);
					this.Dup();
					this.Ldthis();
					this.Callvirt(_crawlposM);
					this.Beq(label11);
					this.MarkLabel(label10);
					this.Ldthis();
					this.Callvirt(_uncaptureM);
					this.Dup();
					this.Ldthis();
					this.Callvirt(_crawlposM);
					this.Bne(label10);
					this.MarkLabel(label11);
					this.Pop();
					this.Back();
					return;
				}
				case 0x24:
					this.PopStack();
					this.Stloc(this._tempV);
					this.Ldthisfld(_trackF);
					this.Ldlen();
					this.PopStack();
					this.Sub();
					this.Stloc(this._trackposV);
					this.PushTrack(this._tempV);
					this.TrackUnique(9);
					return;

				case 0x25:
					this.Ldthis();
					this.Ldc(this.Operand(0));
					this.Callvirt(_ismatchedM);
					this.BrfalseFar(this._backtrack);
					return;

				case 0x26:
					this.Goto(this.Operand(0));
					return;

				case 40:
					this.Mvlocfld(this._textposV, _textposF);
					this.Ret();
					return;

				case 0x29:
				case 0x2a:
					this.Ldthis();
					this.Ldloc(this._textposV);
					this.Ldloc(this._textbegV);
					this.Ldloc(this._textendV);
					this.Callvirt(_isECMABoundaryM);
					if (this.Code() != 0x29)
					{
						this.BrtrueFar(this._backtrack);
						return;
					}
					this.BrfalseFar(this._backtrack);
					return;

				case 0x4c:
				case 0x24c:
				{
					string str2 = this._strings[this.Operand(0)];
					this.Ldc(str2.Length);
					this.Ldloc(this._textposV);
					this.Ldloc(this._textbegV);
					this.Sub();
					this.BgtFar(this._backtrack);
					int length = str2.Length;
					while (length > 0)
					{
						length--;
						this.Ldloc(this._textV);
						this.Ldloc(this._textposV);
						this.Ldc(str2.Length - length);
						this.Sub();
						this.Callvirt(_getcharM);
						if (this.IsCi())
						{
							this.CallToLower();
						}
						this.Ldc(str2[length]);
						this.BneFar(this._backtrack);
					}
					this.Ldloc(this._textposV);
					this.Ldc(str2.Length);
					this.Sub();
					this.Stloc(this._textposV);
					return;
				}
				case 0x83:
				case 0x84:
				case 0x85:
				case 0xc3:
				case 0xc4:
				case 0xc5:
				case 0x283:
				case 0x284:
				case 0x285:
				case 0x2c3:
				case 0x2c4:
				case 0x2c5:
					this.PopTrack();
					this.Stloc(this._textposV);
					this.PopTrack();
					this.Stloc(this._tempV);
					this.Ldloc(this._tempV);
					this.Ldc(0);
					this.BleFar(this.AdvanceLabel());
					this.ReadyPushTrack();
					this.Ldloc(this._tempV);
					this.Ldc(1);
					this.Sub();
					this.DoPush();
					this.ReadyPushTrack();
					this.Ldloc(this._textposV);
					this.Ldc(1);
					this.Sub(this.IsRtl());
					this.DoPush();
					this.Trackagain();
					this.Advance();
					return;

				case 0x86:
				case 0x87:
				case 0x88:
				case 0xc6:
				case 0xc7:
				case 200:
				case 0x286:
				case 0x287:
				case 0x288:
				case 710:
				case 0x2c7:
				case 0x2c8:
					this.PopTrack();
					this.Stloc(this._textposV);
					this.PopTrack();
					this.Stloc(this._temp2V);
					if (!this.IsRtl())
					{
						this.Rightcharnext();
					}
					else
					{
						this.Leftcharnext();
					}
					if (this.IsCi())
					{
						this.CallToLower();
					}
					if (this.Code() == 8)
					{
						this.Ldstr(this._strings[this.Operand(0)]);
						this.Call(_charInSetM);
						this.BrfalseFar(this._backtrack);
					}
					else
					{
						this.Ldc(this.Operand(0));
						if (this.Code() == 6)
						{
							this.BneFar(this._backtrack);
						}
						else
						{
							this.BeqFar(this._backtrack);
						}
					}
					this.Ldloc(this._temp2V);
					this.Ldc(0);
					this.BleFar(this.AdvanceLabel());
					this.ReadyPushTrack();
					this.Ldloc(this._temp2V);
					this.Ldc(1);
					this.Sub();
					this.DoPush();
					this.PushTrack(this._textposV);
					this.Trackagain();
					this.Advance();
					return;

				case 0x97:
					this.PopTrack();
					this.Stloc(this._textposV);
					this.Goto(this.Operand(0));
					return;

				case 0x98:
					this.PopTrack();
					this.Stloc(this._textposV);
					this.PopStack();
					this.Pop();
					this.TrackUnique2(5);
					this.Advance();
					return;

				case 0x99:
					this.PopTrack();
					this.Stloc(this._textposV);
					this.PushStack(this._textposV);
					this.TrackUnique2(6);
					this.Goto(this.Operand(0));
					return;

				case 0x9a:
				case 0x9b:
					this.PopDiscardStack(2);
					this.Back();
					return;

				case 0x9c:
				{
					LocalBuilder builder5 = this._tempV;
					Label label7 = this.DefineLabel();
					this.PopStack();
					this.Ldc(1);
					this.Sub();
					this.Dup();
					this.Stloc(builder5);
					this.Ldc(0);
					this.Blt(label7);
					this.PopStack();
					this.Stloc(this._textposV);
					this.PushTrack(builder5);
					this.TrackUnique2(7);
					this.Advance();
					this.MarkLabel(label7);
					this.ReadyReplaceStack(0);
					this.PopTrack();
					this.DoReplace();
					this.PushStack(builder5);
					this.Back();
					return;
				}
				case 0x9d:
				{
					Label label9 = this.DefineLabel();
					LocalBuilder builder8 = this._tempV;
					this.PopTrack();
					this.Stloc(this._textposV);
					this.PopTrack();
					this.Dup();
					this.Stloc(builder8);
					this.Ldc(this.Operand(1));
					this.Bge(label9);
					this.Ldloc(this._textposV);
					this.TopTrack();
					this.Beq(label9);
					this.PushStack(this._textposV);
					this.ReadyPushStack();
					this.Ldloc(builder8);
					this.Ldc(1);
					this.Add();
					this.DoPush();
					this.TrackUnique2(8);
					this.Goto(this.Operand(0));
					this.MarkLabel(label9);
					this.ReadyPushStack();
					this.PopTrack();
					this.DoPush();
					this.PushStack(builder8);
					this.Back();
					return;
				}
				case 0x9e:
				case 0x9f:
					this.PopDiscardStack();
					this.Back();
					return;

				case 160:
					this.ReadyPushStack();
					this.PopTrack();
					this.DoPush();
					this.Ldthis();
					this.Callvirt(_uncaptureM);
					if ((this.Operand(0) != -1) && (this.Operand(1) != -1))
					{
						this.Ldthis();
						this.Callvirt(_uncaptureM);
					}
					this.Back();
					return;

				case 0xa1:
					this.ReadyPushStack();
					this.PopTrack();
					this.DoPush();
					this.Back();
					return;

				case 0xa2:
					this.PopDiscardStack(2);
					this.Back();
					return;

				case 0xa4:
				{
					Label label12 = this.DefineLabel();
					Label label13 = this.DefineLabel();
					this.PopTrack();
					this.Dup();
					this.Ldthis();
					this.Callvirt(_crawlposM);
					this.Beq(label13);
					this.MarkLabel(label12);
					this.Ldthis();
					this.Callvirt(_uncaptureM);
					this.Dup();
					this.Ldthis();
					this.Callvirt(_crawlposM);
					this.Bne(label12);
					this.MarkLabel(label13);
					this.Pop();
					this.Back();
					return;
				}
				case 280:
					this.ReadyPushStack();
					this.PopTrack();
					this.DoPush();
					this.Back();
					return;

				case 0x119:
					this.ReadyReplaceStack(0);
					this.PopTrack();
					this.DoReplace();
					this.Back();
					return;

				case 0x11c:
					this.PopTrack();
					this.Stloc(this._tempV);
					this.ReadyPushStack();
					this.PopTrack();
					this.DoPush();
					this.PushStack(this._tempV);
					this.Back();
					return;

				case 0x11d:
					this.ReadyReplaceStack(1);
					this.PopTrack();
					this.DoReplace();
					this.ReadyReplaceStack(0);
					this.TopStack();
					this.Ldc(1);
					this.Sub();
					this.DoReplace();
					this.Back();
					return;
			}
			throw new NotImplementedException("UnimplementedState");
		}

		internal void Goto(int i)
		{
			if (i < this._codepos)
			{
				Label l = this.DefineLabel();
				this.Ldloc(this._trackposV);
				this.Ldc(this._trackcount * 4);
				this.Ble(l);
				this.Ldloc(this._stackposV);
				this.Ldc(this._trackcount * 3);
				this.BgtFar(this._labels[i]);
				this.MarkLabel(l);
				this.ReadyPushTrack();
				this.Ldc(this.AddGoto(i));
				this.DoPush();
				this.BrFar(this._backtrack);
			}
			else
			{
				this.BrFar(this._labels[i]);
			}
		}

		internal bool IsCi()
		{
			return ((this._regexopcode & 0x200) != 0);
		}

		internal bool IsRtl()
		{
			return ((this._regexopcode & 0x40) != 0);
		}

		internal void Ldc(int i)
		{
			if ((i <= 0x7f) && (i >= -128))
			{
				this._ilg.Emit(OpCodes.Ldc_I4_S, (byte) i);
			}
			else
			{
				this._ilg.Emit(OpCodes.Ldc_I4, i);
			}
		}

		internal void Ldlen()
		{
			this._ilg.Emit(OpCodes.Ldlen);
		}

		internal void Ldloc(LocalBuilder lt)
		{
			this._ilg.Emit(OpCodes.Ldloc_S, lt);
		}

		internal void Ldstr(string str)
		{
			this._ilg.Emit(OpCodes.Ldstr, str);
		}

		internal void Ldthis()
		{
			this._ilg.Emit(OpCodes.Ldarg_0);
		}

		internal void Ldthisfld(FieldInfo ft)
		{
			this.Ldthis();
			this._ilg.Emit(OpCodes.Ldfld, ft);
		}

		internal void Leftchar()
		{
			this.Ldloc(this._textV);
			this.Ldloc(this._textposV);
			this.Ldc(1);
			this.Sub();
			this.Callvirt(_getcharM);
		}

		internal void Leftcharnext()
		{
			this.Ldloc(this._textV);
			this.Ldloc(this._textposV);
			this.Ldc(1);
			this.Sub();
			this.Dup();
			this.Stloc(this._textposV);
			this.Callvirt(_getcharM);
		}

		internal void MarkLabel(Label l)
		{
			this._ilg.MarkLabel(l);
		}

		internal void Mvfldloc(FieldInfo ft, LocalBuilder lt)
		{
			this.Ldthisfld(ft);
			this.Stloc(lt);
		}

		internal void Mvlocfld(LocalBuilder lt, FieldInfo ft)
		{
			this.Ldthis();
			this.Ldloc(lt);
			this.Stfld(ft);
		}

		internal void Newobj(ConstructorInfo ct)
		{
			this._ilg.Emit(OpCodes.Newobj, ct);
		}

		internal int NextCodepos()
		{
			return (this._codepos + RegexCode.OpcodeSize(this._codes[this._codepos]));
		}

		internal int Operand(int i)
		{
			return this._codes[(this._codepos + i) + 1];
		}

		internal void Pop()
		{
			this._ilg.Emit(OpCodes.Pop);
		}

		internal void PopDiscardStack()
		{
			this.PopDiscardStack(1);
		}

		internal void PopDiscardStack(int i)
		{
			this._ilg.Emit(OpCodes.Ldloc_S, this._stackposV);
			this.Ldc(i);
			this._ilg.Emit(OpCodes.Add);
			this._ilg.Emit(OpCodes.Stloc_S, this._stackposV);
		}

		internal void PopStack()
		{
			this._ilg.Emit(OpCodes.Ldloc_S, this._stackV);
			this._ilg.Emit(OpCodes.Ldloc_S, this._stackposV);
			this._ilg.Emit(OpCodes.Dup);
			this._ilg.Emit(OpCodes.Ldc_I4_1);
			this._ilg.Emit(OpCodes.Add);
			this._ilg.Emit(OpCodes.Stloc_S, this._stackposV);
			this._ilg.Emit(OpCodes.Ldelem_I4);
		}

		internal void PopTrack()
		{
			this._ilg.Emit(OpCodes.Ldloc_S, this._trackV);
			this._ilg.Emit(OpCodes.Ldloc_S, this._trackposV);
			this._ilg.Emit(OpCodes.Dup);
			this._ilg.Emit(OpCodes.Ldc_I4_1);
			this._ilg.Emit(OpCodes.Add);
			this._ilg.Emit(OpCodes.Stloc_S, this._trackposV);
			this._ilg.Emit(OpCodes.Ldelem_I4);
		}

		internal void PushStack(LocalBuilder lt)
		{
			this.ReadyPushStack();
			this._ilg.Emit(OpCodes.Ldloc_S, lt);
			this.DoPush();
		}

		internal void PushTrack(LocalBuilder lt)
		{
			this.ReadyPushTrack();
			this.Ldloc(lt);
			this.DoPush();
		}

		internal void ReadyPushStack()
		{
			this._ilg.Emit(OpCodes.Ldloc_S, this._stackV);
			this._ilg.Emit(OpCodes.Ldloc_S, this._stackposV);
			this._ilg.Emit(OpCodes.Ldc_I4_1);
			this._ilg.Emit(OpCodes.Sub);
			this._ilg.Emit(OpCodes.Dup);
			this._ilg.Emit(OpCodes.Stloc_S, this._stackposV);
		}

		internal void ReadyPushTrack()
		{
			this._ilg.Emit(OpCodes.Ldloc_S, this._trackV);
			this._ilg.Emit(OpCodes.Ldloc_S, this._trackposV);
			this._ilg.Emit(OpCodes.Ldc_I4_1);
			this._ilg.Emit(OpCodes.Sub);
			this._ilg.Emit(OpCodes.Dup);
			this._ilg.Emit(OpCodes.Stloc_S, this._trackposV);
		}

		internal void ReadyReplaceStack(int i)
		{
			this._ilg.Emit(OpCodes.Ldloc_S, this._stackV);
			this._ilg.Emit(OpCodes.Ldloc_S, this._stackposV);
			if (i != 0)
			{
				this.Ldc(i);
				this._ilg.Emit(OpCodes.Add);
			}
		}

		private static FieldInfo RegexRunnerField(string fieldname)
		{
			return typeof(RegexRunner).GetField(fieldname, BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Static | BindingFlags.Instance);
		}

		private static MethodInfo RegexRunnerMethod(string methname)
		{
			return typeof(RegexRunner).GetMethod(methname, BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Static | BindingFlags.Instance);
		}

		internal void Ret()
		{
			this._ilg.Emit(OpCodes.Ret);
		}

		internal void Rightchar()
		{
			this.Ldloc(this._textV);
			this.Ldloc(this._textposV);
			this.Callvirt(_getcharM);
		}

		internal void Rightcharnext()
		{
			this.Ldloc(this._textV);
			this.Ldloc(this._textposV);
			this.Dup();
			this.Ldc(1);
			this.Add();
			this.Stloc(this._textposV);
			this.Callvirt(_getcharM);
		}

		internal void Stfld(FieldInfo ft)
		{
			this._ilg.Emit(OpCodes.Stfld, ft);
		}

		internal void Stloc(LocalBuilder lt)
		{
			this._ilg.Emit(OpCodes.Stloc_S, lt);
		}

		internal void Sub()
		{
			this._ilg.Emit(OpCodes.Sub);
		}

		internal void Sub(bool negate)
		{
			if (negate)
			{
				this._ilg.Emit(OpCodes.Add);
			}
			else
			{
				this._ilg.Emit(OpCodes.Sub);
			}
		}

		internal void TopStack()
		{
			this._ilg.Emit(OpCodes.Ldloc_S, this._stackV);
			this._ilg.Emit(OpCodes.Ldloc_S, this._stackposV);
			this._ilg.Emit(OpCodes.Ldelem_I4);
		}

		internal void TopTrack()
		{
			this._ilg.Emit(OpCodes.Ldloc_S, this._trackV);
			this._ilg.Emit(OpCodes.Ldloc_S, this._trackposV);
			this._ilg.Emit(OpCodes.Ldelem_I4);
		}

		internal void Track()
		{
			this.ReadyPushTrack();
			this.Ldc(this.AddTrack());
			this.DoPush();
		}

		internal void Trackagain()
		{
			this.ReadyPushTrack();
			this.Ldc(this._backpos);
			this.DoPush();
		}

		internal void TrackUnique(int i)
		{
			this.ReadyPushTrack();
			this.Ldc(this.AddUniqueTrack(i));
			this.DoPush();
		}

		internal void TrackUnique2(int i)
		{
			this.ReadyPushTrack();
			this.Ldc(this.AddUniqueTrack(i, 0x100));
			this.DoPush();
		}

		// Nested Types
		internal sealed class BacktrackNote
		{
			// Fields
			internal int _codepos;
			internal int _flags;
			internal Label _label;

			// Methods
			internal BacktrackNote(int flags, Label label, int codepos)
			{
				this._codepos = codepos;
				this._flags = flags;
				this._label = label;
			}
		}
	}

	[Serializable]
	public class RegexCompilationInfo
	{
		// Fields
		private bool isPublic;
		private string name;
		private string nspace;
		private RegexOptions options;
		private string pattern;

		// Methods
		public RegexCompilationInfo(string pattern, RegexOptions options, string name, string fullnamespace, bool ispublic)
		{
			this.Pattern = pattern;
			this.Name = name;
			this.Namespace = fullnamespace;
			this.options = options;
			this.isPublic = ispublic;
		}

		// Properties
		public bool IsPublic
		{
			[TargetedPatchingOptOut("Performance critical to inline this type of method across NGen image boundaries")]
			get
			{
				return this.isPublic;
			}
			[TargetedPatchingOptOut("Performance critical to inline this type of method across NGen image boundaries")]
			set
			{
				this.isPublic = value;
			}
		}

		public string Name
		{
			[TargetedPatchingOptOut("Performance critical to inline this type of method across NGen image boundaries")]
			get
			{
				return this.name;
			}
			set
			{
				if (value == null)
				{
					throw new ArgumentNullException("value");
				}
				if (value.Length == 0)
				{
					throw new ArgumentException("InvalidNullEmptyArgument", "value");
				}
				this.name = value;
			}
		}

		public string Namespace
		{
			[TargetedPatchingOptOut("Performance critical to inline this type of method across NGen image boundaries")]
			get
			{
				return this.nspace;
			}
			set
			{
				if (value == null)
				{
					throw new ArgumentNullException("value");
				}
				this.nspace = value;
			}
		}

		public RegexOptions Options
		{
			[TargetedPatchingOptOut("Performance critical to inline this type of method across NGen image boundaries")]
			get
			{
				return this.options;
			}
			[TargetedPatchingOptOut("Performance critical to inline this type of method across NGen image boundaries")]
			set
			{
				this.options = value;
			}
		}

		public string Pattern
		{
			[TargetedPatchingOptOut("Performance critical to inline this type of method across NGen image boundaries")]
			get
			{
				return this.pattern;
			}
			set
			{
				if (value == null)
				{
					throw new ArgumentNullException("value");
				}
				this.pattern = value;
			}
		}
	}

	[Serializable]
	public class MatchCollection : ICollection, IEnumerable
	{
		// Fields
		internal int _beginning;
		internal bool _done;
		internal string _input;
		internal int _length;
		internal ArrayList _matches;
		internal int _prevlen;
		internal Regex _regex;
		internal int _startat;
		private static int infinite = 0x7fffffff;

		// Methods
		internal MatchCollection(Regex regex, string input, int beginning, int length, int startat)
		{
			if ((startat < 0) || (startat > input.Length))
			{
				throw new ArgumentOutOfRangeException("startat", "BeginIndexNotNegative");
			}
			this._regex = regex;
			this._input = input;
			this._beginning = beginning;
			this._length = length;
			this._startat = startat;
			this._prevlen = -1;
			this._matches = new ArrayList();
			this._done = false;
		}

		public void CopyTo(Array array, int arrayIndex)
		{
			if ((array != null) && (array.Rank != 1))
			{
				throw new ArgumentException("Arg_RankMultiDimNotSupported");
			}
			int count = this.Count;
			try
			{
				this._matches.CopyTo(array, arrayIndex);
			}
			catch (ArrayTypeMismatchException)
			{
				throw new ArgumentException("Arg_InvalidArrayType");
			}
		}

		public IEnumerator GetEnumerator()
		{
			return new MatchEnumerator(this);
		}

		internal Match GetMatch(int i)
		{
			Match match;
			if (i < 0)
			{
				return null;
			}
			if (this._matches.Count > i)
			{
				return (Match) this._matches[i];
			}
			if (this._done)
			{
				return null;
			}
			do
			{
				match = this._regex.Run(false, this._prevlen, this._input, this._beginning, this._length, this._startat);
				if (!match.Success)
				{
					this._done = true;
					return null;
				}
				this._matches.Add(match);
				this._prevlen = match._length;
				this._startat = match._textpos;
			}
			while (this._matches.Count <= i);
			return match;
		}

		// Properties
		public int Count
		{
			get
			{
				if (!this._done)
				{
					this.GetMatch(infinite);
				}
				return this._matches.Count;
			}
		}

		public bool IsReadOnly
		{
			get
			{
				return true;
			}
		}

		public bool IsSynchronized
		{
			get
			{
				return false;
			}
		}

		public virtual Match this[int i]
		{
			get
			{
				Match match = this.GetMatch(i);
				if (match == null)
				{
					throw new ArgumentOutOfRangeException("i");
				}
				return match;
			}
		}

		public object SyncRoot
		{
			get
			{
				return this;
			}
		}
	}

	[Serializable]
	internal class MatchEnumerator : IEnumerator
	{
		// Fields
		internal int _curindex;
		internal bool _done;
		internal Match _match;
		internal MatchCollection _matchcoll;

		// Methods
		internal MatchEnumerator(MatchCollection matchcoll)
		{
			this._matchcoll = matchcoll;
		}

		public bool MoveNext()
		{
			if (this._done)
			{
				return false;
			}
			this._match = this._matchcoll.GetMatch(this._curindex++);
			if (this._match == null)
			{
				this._done = true;
				return false;
			}
			return true;
		}

		public void Reset()
		{
			this._curindex = 0;
			this._done = false;
			this._match = null;
		}

		// Properties
		public object Current
		{
			get
			{
				if (this._match == null)
				{
					throw new InvalidOperationException("EnumNotStarted");
				}
				return this._match;
			}
		}
	}

	[Serializable]
	public delegate string MatchEvaluator(Match match);

	internal sealed class RegexBoyerMoore
	{
		// Fields
		internal bool _caseInsensitive;
		internal CultureInfo _culture;
		internal int _highASCII;
		internal int _lowASCII;
		internal int[] _negativeASCII;
		internal int[][] _negativeUnicode;
		internal string _pattern;
		internal int[] _positive;
		internal bool _rightToLeft;
		internal const int infinite = 0x7fffffff;

		// Methods
		internal RegexBoyerMoore(string pattern, bool caseInsensitive, bool rightToLeft, CultureInfo culture)
		{
			int length;
			int num2;
			int num3;
			int num6;
			if (caseInsensitive)
			{
				StringBuilder builder = new StringBuilder(pattern.Length);
				for (int i = 0; i < pattern.Length; i++)
				{
					builder.Append(char.ToLower(pattern[i], culture));
				}
				pattern = builder.ToString();
			}
			this._pattern = pattern;
			this._rightToLeft = rightToLeft;
			this._caseInsensitive = caseInsensitive;
			this._culture = culture;
			if (!rightToLeft)
			{
				length = -1;
				num2 = pattern.Length - 1;
				num3 = 1;
			}
			else
			{
				length = pattern.Length;
				num2 = 0;
				num3 = -1;
			}
			this._positive = new int[pattern.Length];
			int index = num2;
			char ch = pattern[index];
			this._positive[index] = num3;
			index -= num3;
		Label_00AE:
			if (index == length)
			{
				for (num6 = num2 - num3; num6 != length; num6 -= num3)
				{
					if (this._positive[num6] == 0)
					{
						this._positive[num6] = num3;
					}
				}
				this._negativeASCII = new int[0x80];
				for (int j = 0; j < 0x80; j++)
				{
					this._negativeASCII[j] = num2 - length;
				}
				this._lowASCII = 0x7f;
				this._highASCII = 0;
				for (index = num2; index != length; index -= num3)
				{
					ch = pattern[index];
					if (ch < '\x0080')
					{
						if (this._lowASCII > ch)
						{
							this._lowASCII = ch;
						}
						if (this._highASCII < ch)
						{
							this._highASCII = ch;
						}
						if (this._negativeASCII[ch] == (num2 - length))
						{
							this._negativeASCII[ch] = num2 - index;
						}
					}
					else
					{
						int num9 = ch >> 8;
						int num10 = ch & '\x00ff';
						if (this._negativeUnicode == null)
						{
							this._negativeUnicode = new int[0x100][];
						}
						if (this._negativeUnicode[num9] == null)
						{
							int[] destinationArray = new int[0x100];
							for (int k = 0; k < 0x100; k++)
							{
								destinationArray[k] = num2 - length;
							}
							if (num9 == 0)
							{
								Array.Copy(this._negativeASCII, destinationArray, 0x80);
								this._negativeASCII = destinationArray;
							}
							this._negativeUnicode[num9] = destinationArray;
						}
						if (this._negativeUnicode[num9][num10] == (num2 - length))
						{
							this._negativeUnicode[num9][num10] = num2 - index;
						}
					}
				}
			}
			else
			{
				if (pattern[index] != ch)
				{
					index -= num3;
				}
				else
				{
					num6 = num2;
					int num5 = index;
					while (true)
					{
						if ((num5 == length) || (pattern[num6] != pattern[num5]))
						{
							if (this._positive[num6] == 0)
							{
								this._positive[num6] = num6 - num5;
							}
							break;
						}
						num5 -= num3;
						num6 -= num3;
					}
					index -= num3;
				}
				goto Label_00AE;
			}
		}

		internal bool IsMatch(string text, int index, int beglimit, int endlimit)
		{
			if (!this._rightToLeft)
			{
				return (((index >= beglimit) && ((endlimit - index) >= this._pattern.Length)) && this.MatchPattern(text, index));
			}
			return (((index <= endlimit) && ((index - beglimit) >= this._pattern.Length)) && this.MatchPattern(text, index - this._pattern.Length));
		}

		private bool MatchPattern(string text, int index)
		{
			if (!this._caseInsensitive)
			{
				return (0 == string.CompareOrdinal(this._pattern, 0, text, index, this._pattern.Length));
			}
			if ((text.Length - index) < this._pattern.Length)
			{
				return false;
			}
			TextInfo textInfo = this._culture.TextInfo;
			for (int i = 0; i < this._pattern.Length; i++)
			{
				if (textInfo.ToLower(text[index + i]) != this._pattern[i])
				{
					return false;
				}
			}
			return true;
		}

		internal int Scan(string text, int index, int beglimit, int endlimit)
		{
			int num;
			int num4;
			int num5;
			int num6;
			int length;
			int num8;
			int[] numArray;
			if (!this._rightToLeft)
			{
				length = this._pattern.Length;
				num4 = this._pattern.Length - 1;
				num5 = 0;
				num = (index + length) - 1;
				num8 = 1;
			}
			else
			{
				length = -this._pattern.Length;
				num4 = 0;
				num5 = -length - 1;
				num = index + length;
				num8 = -1;
			}
			char ch = this._pattern[num4];
		Label_005F:
			if ((num >= endlimit) || (num < beglimit))
			{
				return -1;
			}
			char c = text[num];
			if (this._caseInsensitive)
			{
				c = char.ToLower(c, this._culture);
			}
			if (c != ch)
			{
				if (c < '\x0080')
				{
					num6 = this._negativeASCII[c];
				}
				else if ((this._negativeUnicode != null) && ((numArray = this._negativeUnicode[c >> 8]) != null))
				{
					num6 = numArray[c & '\x00ff'];
				}
				else
				{
					num6 = length;
				}
				num += num6;
			}
			else
			{
				int num2 = num;
				int num3 = num4;
				do
				{
					if (num3 == num5)
					{
						if (!this._rightToLeft)
						{
							return num2;
						}
						return (num2 + 1);
					}
					num3 -= num8;
					num2 -= num8;
					c = text[num2];
					if (this._caseInsensitive)
					{
						c = char.ToLower(c, this._culture);
					}
				}
				while (c == this._pattern[num3]);
				num6 = this._positive[num3];
				if ((c & 0xff80) == 0)
				{
					num2 = (num3 - num4) + this._negativeASCII[c];
				}
				else if ((this._negativeUnicode != null) && ((numArray = this._negativeUnicode[c >> 8]) != null))
				{
					num2 = (num3 - num4) + numArray[c & '\x00ff'];
				}
				else
				{
					num += num6;
					goto Label_005F;
				}
				if (this._rightToLeft ? (num2 < num6) : (num2 > num6))
				{
					num6 = num2;
				}
				num += num6;
			}
			goto Label_005F;
		}

		public override string ToString()
		{
			return this._pattern;
		}
	}


	internal sealed class RegexPrefix
	{
		// Fields
		internal bool _caseInsensitive;
		internal static RegexPrefix _empty = new RegexPrefix(string.Empty, false);
		internal string _prefix;

		// Methods
		internal RegexPrefix(string prefix, bool ci)
		{
			this._prefix = prefix;
			this._caseInsensitive = ci;
		}

		// Properties
		internal bool CaseInsensitive
		{
			get
			{
				return this._caseInsensitive;
			}
		}

		internal static RegexPrefix Empty
		{
			get
			{
				return _empty;
			}
		}

		internal string Prefix
		{
			get
			{
				return this._prefix;
			}
		}
	}

	internal sealed class RegexReplacement
	{
		// Fields
		internal string _rep;
		internal List<int> _rules;
		internal List<string> _strings;
		internal const int LastGroup = -3;
		internal const int LeftPortion = -1;
		internal const int RightPortion = -2;
		internal const int Specials = 4;
		internal const int WholeString = -4;

		// Methods
		internal RegexReplacement(string rep, RegexNode concat, Hashtable _caps)
		{
			this._rep = rep;
			if (concat.Type() != 0x19)
			{
				throw new ArgumentException("ReplacementError");
			}
			StringBuilder builder = new StringBuilder();
			List<string> list = new List<string>();
			List<int> list2 = new List<int>();
			for (int i = 0; i < concat.ChildCount(); i++)
			{
				RegexNode node = concat.Child(i);
				switch (node.Type())
				{
					case 9:
					{
						builder.Append(node._ch);
						continue;
					}
					case 12:
					{
						builder.Append(node._str);
						continue;
					}
					case 13:
					{
						if (builder.Length > 0)
						{
							list2.Add(list.Count);
							list.Add(builder.ToString());
							builder.Length = 0;
						}
						int num = node._m;
						if ((_caps != null) && (num >= 0))
						{
							num = (int) _caps[num];
						}
						list2.Add(-5 - num);
						continue;
					}
				}
				throw new ArgumentException("ReplacementError");
			}
			if (builder.Length > 0)
			{
				list2.Add(list.Count);
				list.Add(builder.ToString());
			}
			this._strings = list;
			this._rules = list2;
		}

		internal string Replace(Regex regex, string input, int count, int startat)
		{
			StringBuilder builder;
			if (count < -1)
			{
				throw new ArgumentOutOfRangeException("count", "CountTooSmall");
			}
			if ((startat < 0) || (startat > input.Length))
			{
				throw new ArgumentOutOfRangeException("startat", "BeginIndexNotNegative");
			}
			if (count == 0)
			{
				return input;
			}
			Match match = regex.Match(input, startat);
			if (!match.Success)
			{
				return input;
			}
			if (regex.RightToLeft)
			{
				List<string> al = new List<string>();
				int length = input.Length;
			Label_00DD:
				if ((match.Index + match.Length) != length)
				{
					al.Add(input.Substring(match.Index + match.Length, (length - match.Index) - match.Length));
				}
				length = match.Index;
				this.ReplacementImplRTL(al, match);
				if (--count != 0)
				{
					match = match.NextMatch();
					if (match.Success)
					{
						goto Label_00DD;
					}
				}
				builder = new StringBuilder();
				if (length > 0)
				{
					builder.Append(input, 0, length);
				}
				for (int i = al.Count - 1; i >= 0; i--)
				{
					builder.Append(al[i]);
				}
				goto Label_017A;
			}
			builder = new StringBuilder();
			int startIndex = 0;
		Label_0066:
			if (match.Index != startIndex)
			{
				builder.Append(input, startIndex, match.Index - startIndex);
			}
			startIndex = match.Index + match.Length;
			this.ReplacementImpl(builder, match);
			if (--count != 0)
			{
				match = match.NextMatch();
				if (match.Success)
				{
					goto Label_0066;
				}
			}
			if (startIndex < input.Length)
			{
				builder.Append(input, startIndex, input.Length - startIndex);
			}
		Label_017A:
			return builder.ToString();
		}

		internal static string Replace(MatchEvaluator evaluator, Regex regex, string input, int count, int startat)
		{
			StringBuilder builder;
			if (evaluator == null)
			{
				throw new ArgumentNullException("evaluator");
			}
			if (count < -1)
			{
				throw new ArgumentOutOfRangeException("count", "CountTooSmall");
			}
			if ((startat < 0) || (startat > input.Length))
			{
				throw new ArgumentOutOfRangeException("startat", "BeginIndexNotNegative");
			}
			if (count == 0)
			{
				return input;
			}
			Match match = regex.Match(input, startat);
			if (!match.Success)
			{
				return input;
			}
			if (regex.RightToLeft)
			{
				List<string> list = new List<string>();
				int length = input.Length;
			Label_00F1:
				if ((match.Index + match.Length) != length)
				{
					list.Add(input.Substring(match.Index + match.Length, (length - match.Index) - match.Length));
				}
				length = match.Index;
				list.Add(evaluator(match));
				if (--count != 0)
				{
					match = match.NextMatch();
					if (match.Success)
					{
						goto Label_00F1;
					}
				}
				builder = new StringBuilder();
				if (length > 0)
				{
					builder.Append(input, 0, length);
				}
				for (int i = list.Count - 1; i >= 0; i--)
				{
					builder.Append(list[i]);
				}
				goto Label_0193;
			}
			builder = new StringBuilder();
			int startIndex = 0;
		Label_0074:
			if (match.Index != startIndex)
			{
				builder.Append(input, startIndex, match.Index - startIndex);
			}
			startIndex = match.Index + match.Length;
			builder.Append(evaluator(match));
			if (--count != 0)
			{
				match = match.NextMatch();
				if (match.Success)
				{
					goto Label_0074;
				}
			}
			if (startIndex < input.Length)
			{
				builder.Append(input, startIndex, input.Length - startIndex);
			}
		Label_0193:
			return builder.ToString();
		}

		internal string Replacement(Match match)
		{
			StringBuilder sb = new StringBuilder();
			this.ReplacementImpl(sb, match);
			return sb.ToString();
		}

		private void ReplacementImpl(StringBuilder sb, Match match)
		{
			for (int i = 0; i < this._rules.Count; i++)
			{
				int num2 = this._rules[i];
				if (num2 >= 0)
				{
					sb.Append(this._strings[num2]);
				}
				else if (num2 < -4)
				{
					sb.Append(match.GroupToStringImpl(-5 - num2));
				}
				else
				{
					switch ((-5 - num2))
					{
						case -4:
							sb.Append(match.GetOriginalString());
							break;

						case -3:
							sb.Append(match.LastGroupToStringImpl());
							break;

						case -2:
							sb.Append(match.GetRightSubstring());
							break;

						case -1:
							sb.Append(match.GetLeftSubstring());
							break;
					}
				}
			}
		}

		private void ReplacementImplRTL(List<string> al, Match match)
		{
			for (int i = this._rules.Count - 1; i >= 0; i--)
			{
				int num2 = this._rules[i];
				if (num2 >= 0)
				{
					al.Add(this._strings[num2]);
				}
				else if (num2 < -4)
				{
					al.Add(match.GroupToStringImpl(-5 - num2));
				}
				else
				{
					switch ((-5 - num2))
					{
						case -4:
							al.Add(match.GetOriginalString());
							break;

						case -3:
							al.Add(match.LastGroupToStringImpl());
							break;

						case -2:
							al.Add(match.GetRightSubstring());
							break;

						case -1:
							al.Add(match.GetLeftSubstring());
							break;
					}
				}
			}
		}

		internal static string[] Split(Regex regex, string input, int count, int startat)
		{
			if (count < 0)
			{
				throw new ArgumentOutOfRangeException("count", "CountTooSmall");
			}
			if ((startat < 0) || (startat > input.Length))
			{
				throw new ArgumentOutOfRangeException("startat", "BeginIndexNotNegative");
			}
			if (count == 1)
			{
				return new string[] { input };
			}
			count--;
			Match match = regex.Match(input, startat);
			if (!match.Success)
			{
				return new string[] { input };
			}
			List<string> list = new List<string>();
			if (regex.RightToLeft)
			{
				int length = input.Length;
			Label_011A:
				list.Add(input.Substring(match.Index + match.Length, (length - match.Index) - match.Length));
				length = match.Index;
				for (int j = 1; j < match.Groups.Count; j++)
				{
					if (match.IsMatched(j))
					{
						list.Add(match.Groups[j].ToString());
					}
				}
				if (--count != 0)
				{
					match = match.NextMatch();
					if (match.Success)
					{
						goto Label_011A;
					}
				}
				list.Add(input.Substring(0, length));
				list.Reverse(0, list.Count);
				goto Label_01BD;
			}
			int startIndex = 0;
		Label_0082:
			list.Add(input.Substring(startIndex, match.Index - startIndex));
			startIndex = match.Index + match.Length;
			for (int i = 1; i < match.Groups.Count; i++)
			{
				if (match.IsMatched(i))
				{
					list.Add(match.Groups[i].ToString());
				}
			}
			if (--count != 0)
			{
				match = match.NextMatch();
				if (match.Success)
				{
					goto Label_0082;
				}
			}
			list.Add(input.Substring(startIndex, input.Length - startIndex));
		Label_01BD:
			return list.ToArray();
		}

		// Properties
		internal string Pattern
		{
			get
			{
				return this._rep;
			}
		}
	}

	[EditorBrowsable(EditorBrowsableState.Never)]
	public abstract class RegexRunner
	{
		// Fields
		protected internal int[] runcrawl;
		protected internal int runcrawlpos;
		protected internal Match runmatch;
		protected internal Regex runregex;
		protected internal int[] runstack;
		protected internal int runstackpos;
		protected internal string runtext;
		protected internal int runtextbeg;
		protected internal int runtextend;
		protected internal int runtextpos;
		protected internal int runtextstart;
		protected internal int[] runtrack;
		protected internal int runtrackcount;
		protected internal int runtrackpos;

		// Methods
		[TargetedPatchingOptOut("Performance critical to inline this type of method across NGen image boundaries")]
		protected internal RegexRunner()
		{
		}

		protected void Capture(int capnum, int start, int end)
		{
			if (end < start)
			{
				int num = end;
				end = start;
				start = num;
			}
			this.Crawl(capnum);
			this.runmatch.AddMatch(capnum, start, end - start);
		}

		protected static bool CharInClass(char ch, string charClass)
		{
			return RegexCharClass.CharInClass(ch, charClass);
		}

		protected static bool CharInSet(char ch, string set, string category)
		{
			string str = RegexCharClass.ConvertOldStringsToClass(set, category);
			return RegexCharClass.CharInClass(ch, str);
		}

		protected void Crawl(int i)
		{
			if (this.runcrawlpos == 0)
			{
				this.DoubleCrawl();
			}
			this.runcrawl[--this.runcrawlpos] = i;
		}

		protected int Crawlpos()
		{
			return (this.runcrawl.Length - this.runcrawlpos);
		}

		protected void DoubleCrawl()
		{
			int[] destinationArray = new int[this.runcrawl.Length * 2];
			Array.Copy(this.runcrawl, 0, destinationArray, this.runcrawl.Length, this.runcrawl.Length);
			this.runcrawlpos += this.runcrawl.Length;
			this.runcrawl = destinationArray;
		}

		protected void DoubleStack()
		{
			int[] destinationArray = new int[this.runstack.Length * 2];
			Array.Copy(this.runstack, 0, destinationArray, this.runstack.Length, this.runstack.Length);
			this.runstackpos += this.runstack.Length;
			this.runstack = destinationArray;
		}

		protected void DoubleTrack()
		{
			int[] destinationArray = new int[this.runtrack.Length * 2];
			Array.Copy(this.runtrack, 0, destinationArray, this.runtrack.Length, this.runtrack.Length);
			this.runtrackpos += this.runtrack.Length;
			this.runtrack = destinationArray;
		}

		protected void EnsureStorage()
		{
			if (this.runstackpos < (this.runtrackcount * 4))
			{
				this.DoubleStack();
			}
			if (this.runtrackpos < (this.runtrackcount * 4))
			{
				this.DoubleTrack();
			}
		}

		protected abstract bool FindFirstChar();
		protected abstract void Go();
		private void InitMatch()
		{
			if (this.runmatch == null)
			{
				if (this.runregex.caps != null)
				{
					this.runmatch = new MatchSparse(this.runregex, this.runregex.caps, this.runregex.capsize, this.runtext, this.runtextbeg, this.runtextend - this.runtextbeg, this.runtextstart);
				}
				else
				{
					this.runmatch = new Match(this.runregex, this.runregex.capsize, this.runtext, this.runtextbeg, this.runtextend - this.runtextbeg, this.runtextstart);
				}
			}
			else
			{
				this.runmatch.Reset(this.runregex, this.runtext, this.runtextbeg, this.runtextend, this.runtextstart);
			}
			if (this.runcrawl != null)
			{
				this.runtrackpos = this.runtrack.Length;
				this.runstackpos = this.runstack.Length;
				this.runcrawlpos = this.runcrawl.Length;
			}
			else
			{
				this.InitTrackCount();
				int num = this.runtrackcount * 8;
				int num2 = this.runtrackcount * 8;
				if (num < 0x20)
				{
					num = 0x20;
				}
				if (num2 < 0x10)
				{
					num2 = 0x10;
				}
				this.runtrack = new int[num];
				this.runtrackpos = num;
				this.runstack = new int[num2];
				this.runstackpos = num2;
				this.runcrawl = new int[0x20];
				this.runcrawlpos = 0x20;
			}
		}

		protected abstract void InitTrackCount();
		protected bool IsBoundary(int index, int startpos, int endpos)
		{
			return (((index > startpos) && RegexCharClass.IsWordChar(this.runtext[index - 1])) != ((index < endpos) && RegexCharClass.IsWordChar(this.runtext[index])));
		}

		protected bool IsECMABoundary(int index, int startpos, int endpos)
		{
			return (((index > startpos) && RegexCharClass.IsECMAWordChar(this.runtext[index - 1])) != ((index < endpos) && RegexCharClass.IsECMAWordChar(this.runtext[index])));
		}

		protected bool IsMatched(int cap)
		{
			return this.runmatch.IsMatched(cap);
		}

		protected int MatchIndex(int cap)
		{
			return this.runmatch.MatchIndex(cap);
		}

		protected int MatchLength(int cap)
		{
			return this.runmatch.MatchLength(cap);
		}

		protected int Popcrawl()
		{
			return this.runcrawl[this.runcrawlpos++];
		}

		protected internal Match Scan(Regex regex, string text, int textbeg, int textend, int textstart, int prevlen, bool quick)
		{
			bool flag = false;
			this.runregex = regex;
			this.runtext = text;
			this.runtextbeg = textbeg;
			this.runtextend = textend;
			this.runtextstart = textstart;
			int num = this.runregex.RightToLeft ? -1 : 1;
			int num2 = this.runregex.RightToLeft ? this.runtextbeg : this.runtextend;
			this.runtextpos = textstart;
			if (prevlen == 0)
			{
				if (this.runtextpos == num2)
				{
					return Match.Empty;
				}
				this.runtextpos += num;
			}
			while (true)
			{
				if (this.FindFirstChar())
				{
					if (!flag)
					{
						this.InitMatch();
						flag = true;
					}
					this.Go();
					if (this.runmatch._matchcount[0] > 0)
					{
						return this.TidyMatch(quick);
					}
					this.runtrackpos = this.runtrack.Length;
					this.runstackpos = this.runstack.Length;
					this.runcrawlpos = this.runcrawl.Length;
				}
				if (this.runtextpos == num2)
				{
					this.TidyMatch(true);
					return Match.Empty;
				}
				this.runtextpos += num;
			}
		}

		private Match TidyMatch(bool quick)
		{
			if (!quick)
			{
				Match runmatch = this.runmatch;
				this.runmatch = null;
				runmatch.Tidy(this.runtextpos);
				return runmatch;
			}
			return null;
		}

		protected void TransferCapture(int capnum, int uncapnum, int start, int end)
		{
			if (end < start)
			{
				int num3 = end;
				end = start;
				start = num3;
			}
			int num = this.MatchIndex(uncapnum);
			int num2 = num + this.MatchLength(uncapnum);
			if (start >= num2)
			{
				end = start;
				start = num2;
			}
			else if (end <= num)
			{
				start = num;
			}
			else
			{
				if (end > num2)
				{
					end = num2;
				}
				if (num > start)
				{
					start = num;
				}
			}
			this.Crawl(uncapnum);
			this.runmatch.BalanceMatch(uncapnum);
			if (capnum != -1)
			{
				this.Crawl(capnum);
				this.runmatch.AddMatch(capnum, start, end - start);
			}
		}

		protected void Uncapture()
		{
			int cap = this.Popcrawl();
			this.runmatch.RemoveMatch(cap);
		}
	}

	[Serializable]
	public class GroupCollection : ICollection, IEnumerable
	{
		// Fields
		internal Hashtable _captureMap;
		internal Group[] _groups;
		internal Match _match;

		// Methods
		internal GroupCollection(Match match, Hashtable caps)
		{
			this._match = match;
			this._captureMap = caps;
		}

		public void CopyTo(Array array, int arrayIndex)
		{
			if (array == null)
			{
				throw new ArgumentNullException("array");
			}
			int index = arrayIndex;
			for (int i = 0; i < this.Count; i++)
			{
				array.SetValue(this[i], index);
				index++;
			}
		}

		public IEnumerator GetEnumerator()
		{
			return new GroupEnumerator(this);
		}

		internal Group GetGroup(int groupnum)
		{
			if (this._captureMap != null)
			{
				object obj2 = this._captureMap[groupnum];
				if (obj2 == null)
				{
					return Group._emptygroup;
				}
				return this.GetGroupImpl((int) obj2);
			}
			if ((groupnum < this._match._matchcount.Length) && (groupnum >= 0))
			{
				return this.GetGroupImpl(groupnum);
			}
			return Group._emptygroup;
		}

		internal Group GetGroupImpl(int groupnum)
		{
			if (groupnum == 0)
			{
				return this._match;
			}
			if (this._groups == null)
			{
				this._groups = new Group[this._match._matchcount.Length - 1];
				for (int i = 0; i < this._groups.Length; i++)
				{
					this._groups[i] = new Group(this._match._text, this._match._matches[i + 1], this._match._matchcount[i + 1]);
				}
			}
			return this._groups[groupnum - 1];
		}

		// Properties
		public int Count
		{
			get
			{
				return this._match._matchcount.Length;
			}
		}

		public bool IsReadOnly
		{
			get
			{
				return true;
			}
		}

		public bool IsSynchronized
		{
			get
			{
				return false;
			}
		}

		public Group this[int groupnum]
		{
			[TargetedPatchingOptOut("Performance critical to inline this type of method across NGen image boundaries")]
			get
			{
				return this.GetGroup(groupnum);
			}
		}

		public Group this[string groupname]
		{
			get
			{
				if (this._match._regex == null)
				{
					return Group._emptygroup;
				}
				return this.GetGroup(this._match._regex.GroupNumberFromName(groupname));
			}
		}

		public object SyncRoot
		{
			[TargetedPatchingOptOut("Performance critical to inline this type of method across NGen image boundaries")]
			get
			{
				return this._match;
			}
		}
	}

	[Serializable]
	public class CaptureCollection : ICollection, IEnumerable
	{
		// Fields
		internal int _capcount;
		internal Capture[] _captures;
		internal Group _group;

		// Methods
		internal CaptureCollection(Group group)
		{
			this._group = group;
			this._capcount = this._group._capcount;
		}

		public void CopyTo(Array array, int arrayIndex)
		{
			if (array == null)
			{
				throw new ArgumentNullException("array");
			}
			int index = arrayIndex;
			for (int i = 0; i < this.Count; i++)
			{
				array.SetValue(this[i], index);
				index++;
			}
		}

		internal Capture GetCapture(int i)
		{
			if ((i == (this._capcount - 1)) && (i >= 0))
			{
				return this._group;
			}
			if ((i >= this._capcount) || (i < 0))
			{
				throw new ArgumentOutOfRangeException("i");
			}
			if (this._captures == null)
			{
				this._captures = new Capture[this._capcount];
				for (int j = 0; j < (this._capcount - 1); j++)
				{
					this._captures[j] = new Capture(this._group._text, this._group._caps[j * 2], this._group._caps[(j * 2) + 1]);
				}
			}
			return this._captures[i];
		}

		public IEnumerator GetEnumerator()
		{
			return new CaptureEnumerator(this);
		}

		// Properties
		public int Count
		{
			[TargetedPatchingOptOut("Performance critical to inline this type of method across NGen image boundaries")]
			get
			{
				return this._capcount;
			}
		}

		public bool IsReadOnly
		{
			get
			{
				return true;
			}
		}

		public bool IsSynchronized
		{
			get
			{
				return false;
			}
		}

		public Capture this[int i]
		{
			[TargetedPatchingOptOut("Performance critical to inline this type of method across NGen image boundaries")]
			get
			{
				return this.GetCapture(i);
			}
		}

		public object SyncRoot
		{
			[TargetedPatchingOptOut("Performance critical to inline this type of method across NGen image boundaries")]
			get
			{
				return this._group;
			}
		}
	}

	internal sealed class RegexCharClass
	{
		// Fields
		private bool _canonical;
		private StringBuilder _categories;
		private static Dictionary<string, string> _definedCategories;
		private static readonly LowerCaseMapping[] _lcTable = new LowerCaseMapping[] { 
			new LowerCaseMapping('A', 'Z', 1, 0x20), new LowerCaseMapping('\x00c0', '\x00de', 1, 0x20), new LowerCaseMapping('Ā', 'Į', 2, 0), new LowerCaseMapping('İ', 'İ', 0, 0x69), new LowerCaseMapping('Ĳ', 'Ķ', 2, 0), new LowerCaseMapping('Ĺ', 'Ň', 3, 0), new LowerCaseMapping('Ŋ', 'Ŷ', 2, 0), new LowerCaseMapping('Ÿ', 'Ÿ', 0, 0xff), new LowerCaseMapping('Ź', 'Ž', 3, 0), new LowerCaseMapping('Ɓ', 'Ɓ', 0, 0x253), new LowerCaseMapping('Ƃ', 'Ƅ', 2, 0), new LowerCaseMapping('Ɔ', 'Ɔ', 0, 0x254), new LowerCaseMapping('Ƈ', 'Ƈ', 0, 0x188), new LowerCaseMapping('Ɖ', 'Ɗ', 1, 0xcd), new LowerCaseMapping('Ƌ', 'Ƌ', 0, 0x18c), new LowerCaseMapping('Ǝ', 'Ǝ', 0, 0x1dd), 
			new LowerCaseMapping('Ə', 'Ə', 0, 0x259), new LowerCaseMapping('Ɛ', 'Ɛ', 0, 0x25b), new LowerCaseMapping('Ƒ', 'Ƒ', 0, 0x192), new LowerCaseMapping('Ɠ', 'Ɠ', 0, 0x260), new LowerCaseMapping('Ɣ', 'Ɣ', 0, 0x263), new LowerCaseMapping('Ɩ', 'Ɩ', 0, 0x269), new LowerCaseMapping('Ɨ', 'Ɨ', 0, 0x268), new LowerCaseMapping('Ƙ', 'Ƙ', 0, 0x199), new LowerCaseMapping('Ɯ', 'Ɯ', 0, 0x26f), new LowerCaseMapping('Ɲ', 'Ɲ', 0, 0x272), new LowerCaseMapping('Ɵ', 'Ɵ', 0, 0x275), new LowerCaseMapping('Ơ', 'Ƥ', 2, 0), new LowerCaseMapping('Ƨ', 'Ƨ', 0, 0x1a8), new LowerCaseMapping('Ʃ', 'Ʃ', 0, 0x283), new LowerCaseMapping('Ƭ', 'Ƭ', 0, 0x1ad), new LowerCaseMapping('Ʈ', 'Ʈ', 0, 0x288), 
			new LowerCaseMapping('Ư', 'Ư', 0, 0x1b0), new LowerCaseMapping('Ʊ', 'Ʋ', 1, 0xd9), new LowerCaseMapping('Ƴ', 'Ƶ', 3, 0), new LowerCaseMapping('Ʒ', 'Ʒ', 0, 0x292), new LowerCaseMapping('Ƹ', 'Ƹ', 0, 0x1b9), new LowerCaseMapping('Ƽ', 'Ƽ', 0, 0x1bd), new LowerCaseMapping('Ǆ', 'ǅ', 0, 0x1c6), new LowerCaseMapping('Ǉ', 'ǈ', 0, 0x1c9), new LowerCaseMapping('Ǌ', 'ǋ', 0, 460), new LowerCaseMapping('Ǎ', 'Ǜ', 3, 0), new LowerCaseMapping('Ǟ', 'Ǯ', 2, 0), new LowerCaseMapping('Ǳ', 'ǲ', 0, 0x1f3), new LowerCaseMapping('Ǵ', 'Ǵ', 0, 0x1f5), new LowerCaseMapping('Ǻ', 'Ȗ', 2, 0), new LowerCaseMapping('Ά', 'Ά', 0, 940), new LowerCaseMapping('Έ', 'Ί', 1, 0x25), 
			new LowerCaseMapping('Ό', 'Ό', 0, 0x3cc), new LowerCaseMapping('Ύ', 'Ώ', 1, 0x3f), new LowerCaseMapping('Α', 'Ϋ', 1, 0x20), new LowerCaseMapping('Ϣ', 'Ϯ', 2, 0), new LowerCaseMapping('Ё', 'Џ', 1, 80), new LowerCaseMapping('А', 'Я', 1, 0x20), new LowerCaseMapping('Ѡ', 'Ҁ', 2, 0), new LowerCaseMapping('Ґ', 'Ҿ', 2, 0), new LowerCaseMapping('Ӂ', 'Ӄ', 3, 0), new LowerCaseMapping('Ӈ', 'Ӈ', 0, 0x4c8), new LowerCaseMapping('Ӌ', 'Ӌ', 0, 0x4cc), new LowerCaseMapping('Ӑ', 'Ӫ', 2, 0), new LowerCaseMapping('Ӯ', 'Ӵ', 2, 0), new LowerCaseMapping('Ӹ', 'Ӹ', 0, 0x4f9), new LowerCaseMapping('Ա', 'Ֆ', 1, 0x30), new LowerCaseMapping('Ⴀ', 'Ⴥ', 1, 0x30), 
			new LowerCaseMapping('Ḁ', 'Ỹ', 2, 0), new LowerCaseMapping('Ἀ', 'Ἇ', 1, -8), new LowerCaseMapping('Ἐ', '἟', 1, -8), new LowerCaseMapping('Ἠ', 'Ἧ', 1, -8), new LowerCaseMapping('Ἰ', 'Ἷ', 1, -8), new LowerCaseMapping('Ὀ', 'Ὅ', 1, -8), new LowerCaseMapping('Ὑ', 'Ὑ', 0, 0x1f51), new LowerCaseMapping('Ὓ', 'Ὓ', 0, 0x1f53), new LowerCaseMapping('Ὕ', 'Ὕ', 0, 0x1f55), new LowerCaseMapping('Ὗ', 'Ὗ', 0, 0x1f57), new LowerCaseMapping('Ὠ', 'Ὧ', 1, -8), new LowerCaseMapping('ᾈ', 'ᾏ', 1, -8), new LowerCaseMapping('ᾘ', 'ᾟ', 1, -8), new LowerCaseMapping('ᾨ', 'ᾯ', 1, -8), new LowerCaseMapping('Ᾰ', 'Ᾱ', 1, -8), new LowerCaseMapping('Ὰ', 'Ά', 1, -74), 
			new LowerCaseMapping('ᾼ', 'ᾼ', 0, 0x1fb3), new LowerCaseMapping('Ὲ', 'Ή', 1, -86), new LowerCaseMapping('ῌ', 'ῌ', 0, 0x1fc3), new LowerCaseMapping('Ῐ', 'Ῑ', 1, -8), new LowerCaseMapping('Ὶ', 'Ί', 1, -100), new LowerCaseMapping('Ῠ', 'Ῡ', 1, -8), new LowerCaseMapping('Ὺ', 'Ύ', 1, -112), new LowerCaseMapping('Ῥ', 'Ῥ', 0, 0x1fe5), new LowerCaseMapping('Ὸ', 'Ό', 1, -128), new LowerCaseMapping('Ὼ', 'Ώ', 1, -126), new LowerCaseMapping('ῼ', 'ῼ', 0, 0x1ff3), new LowerCaseMapping('Ⅰ', 'Ⅿ', 1, 0x10), new LowerCaseMapping('Ⓐ', 'ⓐ', 1, 0x1a), new LowerCaseMapping((char)0xff21, (char)0xff3a, 1, 0x20)
		 };
		private bool _negate;
		private static readonly string[,] _propTable = new string[,] { 
			{ "IsAlphabeticPresentationForms", "ﬀﭐ" }, { "IsArabic", "؀܀" }, { "IsArabicPresentationForms-A", "ﭐ︀" }, { "IsArabicPresentationForms-B", "ﹰ＀" }, { "IsArmenian", "԰֐" }, { "IsArrows", "←∀" }, { "IsBasicLatin", "\0\x0080" }, { "IsBengali", "ঀ਀" }, { "IsBlockElements", "▀■" }, { "IsBopomofo", "㄀㄰" }, { "IsBopomofoExtended", "ㆠ㇀" }, { "IsBoxDrawing", "─▀" }, { "IsBraillePatterns", "⠀⤀" }, { "IsBuhid", "ᝀᝠ" }, { "IsCJKCompatibility", "㌀㐀" }, { "IsCJKCompatibilityForms", "︰﹐" }, 
			{ "IsCJKCompatibilityIdeographs", "豈ﬀ" }, { "IsCJKRadicalsSupplement", "⺀⼀" }, { "IsCJKSymbolsandPunctuation", "　぀" }, { "IsCJKUnifiedIdeographs", "一ꀀ" }, { "IsCJKUnifiedIdeographsExtensionA", "㐀䷀" }, { "IsCherokee", "Ꭰ᐀" }, { "IsCombiningDiacriticalMarks", "̀Ͱ" }, { "IsCombiningDiacriticalMarksforSymbols", "⃐℀" }, { "IsCombiningHalfMarks", "︠︰" }, { "IsCombiningMarksforSymbols", "⃐℀" }, { "IsControlPictures", "␀⑀" }, { "IsCurrencySymbols", "₠⃐" }, { "IsCyrillic", "ЀԀ" }, { "IsCyrillicSupplement", "Ԁ԰" }, { "IsDevanagari", "ऀঀ" }, { "IsDingbats", "✀⟀" }, 
			{ "IsEnclosedAlphanumerics", "①─" }, { "IsEnclosedCJKLettersandMonths", "㈀㌀" }, { "IsEthiopic", "ሀᎀ" }, { "IsGeneralPunctuation", " ⁰" }, { "IsGeometricShapes", "■☀" }, { "IsGeorgian", "Ⴀᄀ" }, { "IsGreek", "ͰЀ" }, { "IsGreekExtended", "ἀ " }, { "IsGreekandCoptic", "ͰЀ" }, { "IsGujarati", "઀଀" }, { "IsGurmukhi", "਀઀" }, { "IsHalfwidthandFullwidthForms", "＀�" }, { "IsHangulCompatibilityJamo", "㄰㆐" }, { "IsHangulJamo", "ᄀሀ" }, { "IsHangulSyllables", "가ힰ" }, { "IsHanunoo", "ᜠᝀ" }, 
			{ "IsHebrew", "֐؀" }, { "IsHighPrivateUseSurrogates", "\udb80\udc00" }, { "IsHighSurrogates", "\ud800\udb80" }, { "IsHiragana", "぀゠" }, { "IsIPAExtensions", "ɐʰ" }, { "IsIdeographicDescriptionCharacters", "⿰　" }, { "IsKanbun", "㆐ㆠ" }, { "IsKangxiRadicals", "⼀⿠" }, { "IsKannada", "ಀഀ" }, { "IsKatakana", "゠㄀" }, { "IsKatakanaPhoneticExtensions", "ㇰ㈀" }, { "IsKhmer", "ក᠀" }, { "IsKhmerSymbols", "᧠ᨀ" }, { "IsLao", "຀ༀ" }, { "IsLatin-1Supplement", "\x0080Ā" }, { "IsLatinExtended-A", "Āƀ" }, 
			{ "IsLatinExtended-B", "ƀɐ" }, { "IsLatinExtendedAdditional", "Ḁἀ" }, { "IsLetterlikeSymbols", "℀⅐" }, { "IsLimbu", "ᤀᥐ" }, { "IsLowSurrogates", "\udc00\ue000" }, { "IsMalayalam", "ഀ඀" }, { "IsMathematicalOperators", "∀⌀" }, { "IsMiscellaneousMathematicalSymbols-A", "⟀⟰" }, { "IsMiscellaneousMathematicalSymbols-B", "⦀⨀" }, { "IsMiscellaneousSymbols", "☀✀" }, { "IsMiscellaneousSymbolsandArrows", "⬀Ⰰ" }, { "IsMiscellaneousTechnical", "⌀␀" }, { "IsMongolian", "᠀ᢰ" }, { "IsMyanmar", "ကႠ" }, { "IsNumberForms", "⅐←" }, { "IsOgham", " ᚠ" }, 
			{ "IsOpticalCharacterRecognition", "⑀①" }, { "IsOriya", "଀஀" }, { "IsPhoneticExtensions", "ᴀᶀ" }, { "IsPrivateUse", "豈" }, { "IsPrivateUseArea", "豈" }, { "IsRunic", "ᚠᜀ" }, { "IsSinhala", "඀฀" }, { "IsSmallFormVariants", "﹐ﹰ" }, { "IsSpacingModifierLetters", "ʰ̀" }, { "IsSpecials", "�" }, { "IsSuperscriptsandSubscripts", "⁰₠" }, { "IsSupplementalArrows-A", "⟰⠀" }, { "IsSupplementalArrows-B", "⤀⦀" }, { "IsSupplementalMathematicalOperators", "⨀⬀" }, { "IsSyriac", "܀ݐ" }, { "IsTagalog", "ᜀᜠ" }, 
			{ "IsTagbanwa", "ᝠក" }, { "IsTaiLe", "ᥐᦀ" }, { "IsTamil", "஀ఀ" }, { "IsTelugu", "ఀಀ" }, { "IsThaana", "ހ߀" }, { "IsThai", "฀຀" }, { "IsTibetan", "ༀက" }, { "IsUnifiedCanadianAboriginalSyllabics", "᐀ " }, { "IsVariationSelectors", "︀︐" }, { "IsYiRadicals", "꒐ꓐ" }, { "IsYiSyllables", "ꀀ꒐" }, { "IsYijingHexagramSymbols", "䷀一" }, { "_xmlC", "-/0;A[_`a{\x00b7\x00b8\x00c0\x00d7\x00d8\x00f7\x00f8ĲĴĿŁŉŊſƀǄǍǱǴǶǺȘɐʩʻ˂ː˒̀͆͢͠Ά΋Ό΍Ύ΢ΣϏϐϗϚϛϜϝϞϟϠϡϢϴЁЍЎѐёѝў҂҃҇ҐӅӇӉӋӍӐӬӮӶӸӺԱ՗ՙ՚աևֺֻ֑֢֣־ֿ׀ׁ׃ׅׄא׫װ׳ءػـٓ٠٪ٰڸںڿۀۏې۔ە۩۪ۮ۰ۺँऄअऺ़ॎ॑ॕक़।०॰ঁ঄অ঍এ঑ও঩প঱ল঳শ঺়ঽা৅ে৉োৎৗ৘ড়৞য়৤০৲ਂਃਅ਋ਏ਑ਓ਩ਪ਱ਲ਴ਵ਷ਸ਺਼਽ਾ੃ੇ੉ੋ੎ਖ਼੝ਫ਼੟੦ੵઁ઄અઌઍ઎એ઒ઓ઩પ઱લ઴વ઺઼૆ે૊ો૎ૠૡ૦૰ଁ଄ଅ଍ଏ଑ଓ଩ପ଱ଲ଴ଶ଺଼ୄେ୉ୋ୎ୖ୘ଡ଼୞ୟୢ୦୰ஂ஄அ஋எ஑ஒ஖ங஛ஜ஝ஞ஠ண஥ந஫மஶஷ஺ா௃ெ௉ொ௎ௗ௘௧௰ఁఄఅ఍ఎ఑ఒ఩పఴవ఺ా౅ె౉ొ౎ౕ౗ౠౢ౦౰ಂ಄ಅ಍ಎ಑ಒ಩ಪ಴ವ಺ಾ೅ೆ೉ೊ೎ೕ೗ೞ೟ೠೢ೦೰ംഄഅ഍എ഑ഒഩപഺാൄെ൉ൊൎൗ൘ൠൢ൦൰กฯะ฻เ๏๐๚ກ຃ຄ຅ງຉຊ຋ຍຎດຘນຠມ຤ລ຦ວຨສຬອຯະ຺ົ຾ເ໅ໆ໇່໎໐໚༘༚༠༪༵༶༷༸༹༺༾཈ཉཪཱ྅྆ྌྐྖྗ྘ྙྮྱྸྐྵྺႠ჆აჷᄀᄁᄂᄄᄅᄈᄉᄊᄋᄍᄎᄓᄼᄽᄾᄿᅀᅁᅌᅍᅎᅏᅐᅑᅔᅖᅙᅚᅟᅢᅣᅤᅥᅦᅧᅨᅩᅪᅭᅯᅲᅴᅵᅶᆞᆟᆨᆩᆫᆬᆮᆰᆷᆹᆺᆻᆼᇃᇫᇬᇰᇱᇹᇺḀẜẠỺἀ἖Ἐ἞ἠ὆Ὀ὎ὐ὘Ὑ὚Ὓ὜Ὕ὞Ὗ὾ᾀ᾵ᾶ᾽ι᾿ῂ῅ῆ῍ῐ῔ῖ῜ῠ῭ῲ῵ῶ´⃐⃝⃡⃢Ω℧Kℬ℮ℯↀↃ々〆〇〈〡〰〱〶ぁゕ゙゛ゝゟァ・ーヿㄅㄭ一龦가힤" }, { "_xmlD", "0:٠٪۰ۺ०॰০ৰ੦ੰ૦૰୦୰௧௰౦౰೦೰൦൰๐๚໐໚༠༪၀၊፩፲០៪᠐᠚０：" }, { "_xmlI", ":;A[_`a{\x00c0\x00d7\x00d8\x00f7\x00f8ĲĴĿŁŉŊſƀǄǍǱǴǶǺȘɐʩʻ˂Ά·Έ΋Ό΍Ύ΢ΣϏϐϗϚϛϜϝϞϟϠϡϢϴЁЍЎѐёѝў҂ҐӅӇӉӋӍӐӬӮӶӸӺԱ՗ՙ՚աևא׫װ׳ءػفًٱڸںڿۀۏې۔ەۖۥۧअऺऽाक़ॢঅ঍এ঑ও঩প঱ল঳শ঺ড়৞য়ৢৰ৲ਅ਋ਏ਑ਓ਩ਪ਱ਲ਴ਵ਷ਸ਺ਖ਼੝ਫ਼੟ੲੵઅઌઍ઎એ઒ઓ઩પ઱લ઴વ઺ઽાૠૡଅ଍ଏ଑ଓ଩ପ଱ଲ଴ଶ଺ଽାଡ଼୞ୟୢஅ஋எ஑ஒ஖ங஛ஜ஝ஞ஠ண஥ந஫மஶஷ஺అ఍ఎ఑ఒ఩పఴవ఺ౠౢಅ಍ಎ಑ಒ಩ಪ಴ವ಺ೞ೟ೠೢഅ഍എ഑ഒഩപഺൠൢกฯะัาิเๆກ຃ຄ຅ງຉຊ຋ຍຎດຘນຠມ຤ລ຦ວຨສຬອຯະັາິຽ຾ເ໅ཀ཈ཉཪႠ჆აჷᄀᄁᄂᄄᄅᄈᄉᄊᄋᄍᄎᄓᄼᄽᄾᄿᅀᅁᅌᅍᅎᅏᅐᅑᅔᅖᅙᅚᅟᅢᅣᅤᅥᅦᅧᅨᅩᅪᅭᅯᅲᅴᅵᅶᆞᆟᆨᆩᆫᆬᆮᆰᆷᆹᆺᆻᆼᇃᇫᇬᇰᇱᇹᇺḀẜẠỺἀ἖Ἐ἞ἠ὆Ὀ὎ὐ὘Ὑ὚Ὓ὜Ὕ὞Ὗ὾ᾀ᾵ᾶ᾽ι᾿ῂ῅ῆ῍ῐ῔ῖ῜ῠ῭ῲ῵ῶ´Ω℧Kℬ℮ℯↀↃ〇〈〡〪ぁゕァ・ㄅㄭ一龦가힤" }, { "_xmlW", "$%+,0:<?A[^_`{|}~\x007f\x00a2\x00ab\x00ac\x00ad\x00ae\x00b7\x00b8\x00bb\x00bc\x00bf\x00c0ȡȢȴɐʮʰ˯̀͐͠ͰʹͶͺͻ΄·Έ΋Ό΍Ύ΢ΣϏϐϷЀ҇҈ӏӐӶӸӺԀԐԱ՗ՙ՚աֈֺֻ֑֢֣־ֿ׀ׁ׃ׅׄא׫װ׳ءػـٖ٠٪ٮ۔ە۝۞ۮ۰ۿܐܭܰ݋ހ޲ँऄअऺ़ॎॐॕक़।०॰ঁ঄অ঍এ঑ও঩প঱ল঳শ঺়ঽা৅ে৉োৎৗ৘ড়৞য়৤০৻ਂਃਅ਋ਏ਑ਓ਩ਪ਱ਲ਴ਵ਷ਸ਺਼਽ਾ੃ੇ੉ੋ੎ਖ਼੝ਫ਼੟੦ੵઁ઄અઌઍ઎એ઒ઓ઩પ઱લ઴વ઺઼૆ે૊ો૎ૐ૑ૠૡ૦૰ଁ଄ଅ଍ଏ଑ଓ଩ପ଱ଲ଴ଶ଺଼ୄେ୉ୋ୎ୖ୘ଡ଼୞ୟୢ୦ୱஂ஄அ஋எ஑ஒ஖ங஛ஜ஝ஞ஠ண஥ந஫மஶஷ஺ா௃ெ௉ொ௎ௗ௘௧௳ఁఄఅ఍ఎ఑ఒ఩పఴవ఺ా౅ె౉ొ౎ౕ౗ౠౢ౦౰ಂ಄ಅ಍ಎ಑ಒ಩ಪ಴ವ಺ಾ೅ೆ೉ೊ೎ೕ೗ೞ೟ೠೢ೦೰ംഄഅ഍എ഑ഒഩപഺാൄെ൉ൊൎൗ൘ൠൢ൦൰ං඄අ඗ක඲ඳ඼ල඾ව෇්෋ා෕ූ෗ෘ෠ෲ෴ก฻฿๏๐๚ກ຃ຄ຅ງຉຊ຋ຍຎດຘນຠມ຤ລ຦ວຨສຬອ຺ົ຾ເ໅ໆ໇່໎໐໚ໜໞༀ༄༓༺༾཈ཉཫཱ྅྆ྌྐ྘ྙ྽྾࿍࿏࿐ကဢဣဨဩါာဳံ်၀၊ၐၚႠ჆აჹᄀᅚᅟᆣᆨᇺሀሇለቇቈ቉ቊ቎ቐ቗ቘ቙ቚ቞በኇኈ኉ኊ኎ነኯኰ኱ኲ኶ኸ኿ዀ዁ዂ዆ወዏዐ዗ዘዯደጏጐ጑ጒ጖ጘጟጠፇፈ፛፩፽ᎠᏵᐁ᙭ᙯᙷᚁ᚛ᚠ᛫ᛮᛱᜀᜍᜎ᜕ᜠ᜵ᝀ᝔ᝠ᝭ᝮ᝱ᝲ᝴ក។ៗ៘៛៝០៪᠋᠎᠐᠚ᠠᡸᢀᢪḀẜẠỺἀ἖Ἐ἞ἠ὆Ὀ὎ὐ὘Ὑ὚Ὓ὜Ὕ὞Ὗ὾ᾀ᾵ᾶ῅ῆ῔ῖ῜῝῰ῲ῵ῶ῿⁄⁅⁒⁓⁰⁲⁴⁽ⁿ₍₠₲⃫⃐℀℻ℽ⅌⅓ↄ←〈⌫⎴⎷⏏␀␧⑀⑋①⓿─☔☖☘☙♾⚀⚊✁✅✆✊✌✨✩❌❍❎❏❓❖❗❘❟❡❨❶➕➘➰➱➿⟐⟦⟰⦃⦙⧘⧜⧼⧾⬀⺀⺚⺛⻴⼀⿖⿰⿼〄〈〒〔〠〰〱〽〾぀ぁ゗゙゠ァ・ー㄀ㄅㄭㄱ㆏㆐ㆸㇰ㈝㈠㉄㉑㉼㉿㋌㋐㋿㌀㍷㍻㏞㏠㏿㐀䶶一龦ꀀ꒍꒐꓇가힤豈郞侮恵ﬀ﬇ﬓ﬘יִ﬷טּ﬽מּ﬿נּ﭂ףּ﭅צּ﮲ﯓ﴾ﵐ﶐ﶒ﷈ﷰ﷽︀︐︠︤﹢﹣﹤﹧﹩﹪ﹰ﹵ﹶ﻽＄％＋，０：＜？Ａ［＾＿｀｛｜｝～｟ｦ﾿ￂ￈ￊ￐ￒ￘ￚ￝￠￧￨￯￼�" }
		 };
		private List<SingleRange> _rangelist;
		private RegexCharClass _subtractor;
		internal const string AnyClass = "\0\x0001\0\0";
		private const int CATEGORYLENGTH = 2;
		internal static readonly string DigitClass;
		internal const string ECMADigitClass = "\0\x0002\00:";
		private const string ECMADigitSet = "0:";
		internal const string ECMASpaceClass = "\0\x0004\0\t\x000e !";
		private const string ECMASpaceSet = "\t\x000e !";
		internal const string ECMAWordClass = "\0\n\00:A[_`a{İı";
		private const string ECMAWordSet = "0:A[_`a{İı";
		internal const string EmptyClass = "\0\0\0";
		private const int FLAGS = 0;
		private const char GroupChar = '\0';
		private static readonly string InternalRegexIgnoreCase = "__InternalRegexIgnoreCase__";
		private const char Lastchar = '�';
		private const int LowercaseAdd = 1;
		private const int LowercaseBad = 3;
		private const int LowercaseBor = 2;
		private const int LowercaseSet = 0;
		internal static readonly string NotDigitClass;
		internal const string NotECMADigitClass = "\x0001\x0002\00:";
		private const string NotECMADigitSet = "\00:";
		internal const string NotECMASpaceClass = "\x0001\x0004\0\t\x000e !";
		private const string NotECMASpaceSet = "\0\t\x000e !";
		internal const string NotECMAWordClass = "\x0001\n\00:A[_`a{İı";
		private const string NotECMAWordSet = "\00:A[_`a{İı";
		private static readonly string NotSpace = NegateCategory(Space);
		internal static readonly string NotSpaceClass;
		private const short NotSpaceConst = -100;
		private static readonly string NotWord;
		internal static readonly string NotWordClass;
		private const char Nullchar = '\0';
		private const int SETLENGTH = 1;
		private const int SETSTART = 3;
		private static readonly string Space = "d";
		internal static readonly string SpaceClass;
		private const short SpaceConst = 100;
		private static readonly string Word;
		internal static readonly string WordClass;
		private const char ZeroWidthJoiner = '‍';
		private const char ZeroWidthNonJoiner = '‌';

		// Methods
		static RegexCharClass()
		{
			Dictionary<string, string> dictionary = new Dictionary<string, string>(0x20);
			char[] chArray = new char[9];
			StringBuilder builder = new StringBuilder(11);
			builder.Append('\0');
			chArray[0] = '\0';
			chArray[1] = '\x000f';
			dictionary["Cc"] = chArray[1].ToString();
			chArray[2] = '\x0010';
			dictionary["Cf"] = chArray[2].ToString();
			chArray[3] = '\x001e';
			dictionary["Cn"] = chArray[3].ToString();
			chArray[4] = '\x0012';
			dictionary["Co"] = chArray[4].ToString();
			chArray[5] = '\x0011';
			dictionary["Cs"] = chArray[5].ToString();
			chArray[6] = '\0';
			dictionary["C"] = new string(chArray, 0, 7);
			chArray[1] = '\x0002';
			dictionary["Ll"] = chArray[1].ToString();
			chArray[2] = '\x0004';
			dictionary["Lm"] = chArray[2].ToString();
			chArray[3] = '\x0005';
			dictionary["Lo"] = chArray[3].ToString();
			chArray[4] = '\x0003';
			dictionary["Lt"] = chArray[4].ToString();
			chArray[5] = '\x0001';
			dictionary["Lu"] = chArray[5].ToString();
			dictionary["L"] = new string(chArray, 0, 7);
			builder.Append(new string(chArray, 1, 5));
			dictionary[InternalRegexIgnoreCase] = string.Format(CultureInfo.InvariantCulture, "{0}{1}{2}{3}{4}", new object[] { '\0', chArray[1], chArray[4], chArray[5], chArray[6] });
			chArray[1] = '\a';
			dictionary["Mc"] = chArray[1].ToString();
			chArray[2] = '\b';
			dictionary["Me"] = chArray[2].ToString();
			chArray[3] = '\x0006';
			dictionary["Mn"] = chArray[3].ToString();
			chArray[4] = '\0';
			dictionary["M"] = new string(chArray, 0, 5);
			chArray[1] = '\t';
			dictionary["Nd"] = chArray[1].ToString();
			chArray[2] = '\n';
			dictionary["Nl"] = chArray[2].ToString();
			chArray[3] = '\v';
			dictionary["No"] = chArray[3].ToString();
			dictionary["N"] = new string(chArray, 0, 5);
			builder.Append(chArray[1]);
			chArray[1] = '\x0013';
			dictionary["Pc"] = chArray[1].ToString();
			chArray[2] = '\x0014';
			dictionary["Pd"] = chArray[2].ToString();
			chArray[3] = '\x0016';
			dictionary["Pe"] = chArray[3].ToString();
			chArray[4] = '\x0019';
			dictionary["Po"] = chArray[4].ToString();
			chArray[5] = '\x0015';
			dictionary["Ps"] = chArray[5].ToString();
			chArray[6] = '\x0018';
			dictionary["Pf"] = chArray[6].ToString();
			chArray[7] = '\x0017';
			dictionary["Pi"] = chArray[7].ToString();
			chArray[8] = '\0';
			dictionary["P"] = new string(chArray, 0, 9);
			builder.Append(chArray[1]);
			chArray[1] = '\x001b';
			dictionary["Sc"] = chArray[1].ToString();
			chArray[2] = '\x001c';
			dictionary["Sk"] = chArray[2].ToString();
			chArray[3] = '\x001a';
			dictionary["Sm"] = chArray[3].ToString();
			chArray[4] = '\x001d';
			dictionary["So"] = chArray[4].ToString();
			chArray[5] = '\0';
			dictionary["S"] = new string(chArray, 0, 6);
			chArray[1] = '\r';
			dictionary["Zl"] = chArray[1].ToString();
			chArray[2] = '\x000e';
			dictionary["Zp"] = chArray[2].ToString();
			chArray[3] = '\f';
			dictionary["Zs"] = chArray[3].ToString();
			chArray[4] = '\0';
			dictionary["Z"] = new string(chArray, 0, 5);
			builder.Append('\0');
			Word = builder.ToString();
			NotWord = NegateCategory(Word);
			SpaceClass = "\0\0\x0001" + Space;
			NotSpaceClass = "\x0001\0\x0001" + Space;
			WordClass = "\0\0" + ((char) Word.Length) + Word;
			NotWordClass = "\x0001\0" + ((char) Word.Length) + Word;
			DigitClass = "\0\0\x0001" + '\t';
			NotDigitClass = "\0\0\x0001" + ((char) 0xfff7);
			_definedCategories = dictionary;
		}

		internal RegexCharClass()
		{
			this._rangelist = new List<SingleRange>(6);
			this._canonical = true;
			this._categories = new StringBuilder();
		}

		private RegexCharClass(bool negate, List<SingleRange> ranges, StringBuilder categories, RegexCharClass subtraction)
		{
			this._rangelist = ranges;
			this._categories = categories;
			this._canonical = true;
			this._negate = negate;
			this._subtractor = subtraction;
		}

		private void AddCategory(string category)
		{
			this._categories.Append(category);
		}

		internal void AddCategoryFromName(string categoryName, bool invert, bool caseInsensitive, string pattern)
		{
			string str;
			_definedCategories.TryGetValue(categoryName, out str);
			if ((str != null) && !categoryName.Equals(InternalRegexIgnoreCase))
			{
				string category = str;
				if (caseInsensitive && ((categoryName.Equals("Ll") || categoryName.Equals("Lu")) || categoryName.Equals("Lt")))
				{
					category = _definedCategories[InternalRegexIgnoreCase];
				}
				if (invert)
				{
					category = NegateCategory(category);
				}
				this._categories.Append(category);
			}
			else
			{
				this.AddSet(SetFromProperty(categoryName, invert, pattern));
			}
		}

		internal void AddChar(char c)
		{
			this.AddRange(c, c);
		}

		internal void AddCharClass(RegexCharClass cc)
		{
			if (!cc._canonical)
			{
				this._canonical = false;
			}
			else if ((this._canonical && (this.RangeCount() > 0)) && ((cc.RangeCount() > 0) && (cc.GetRangeAt(0)._first <= this.GetRangeAt(this.RangeCount() - 1)._last)))
			{
				this._canonical = false;
			}
			for (int i = 0; i < cc.RangeCount(); i++)
			{
				this._rangelist.Add(cc.GetRangeAt(i));
			}
			this._categories.Append(cc._categories.ToString());
		}

		internal void AddDigit(bool ecma, bool negate, string pattern)
		{
			if (ecma)
			{
				if (negate)
				{
					this.AddSet("\00:");
				}
				else
				{
					this.AddSet("0:");
				}
			}
			else
			{
				this.AddCategoryFromName("Nd", negate, false, pattern);
			}
		}

		internal void AddLowercase(CultureInfo culture)
		{
			this._canonical = false;
			int num = 0;
			int count = this._rangelist.Count;
			while (num < count)
			{
				SingleRange range = this._rangelist[num];
				if (range._first == range._last)
				{
					range._first = range._last = char.ToLower(range._first, culture);
				}
				else
				{
					this.AddLowercaseRange(range._first, range._last, culture);
				}
				num++;
			}
		}

		private void AddLowercaseRange(char chMin, char chMax, CultureInfo culture)
		{
			char ch;
			char ch2;
			LowerCaseMapping mapping;
			int index = 0;
			int length = _lcTable.Length;
			while (index < length)
			{
				int num3 = (index + length) / 2;
				if (_lcTable[num3]._chMax < chMin)
				{
					index = num3 + 1;
				}
				else
				{
					length = num3;
				}
			}
			if (index < _lcTable.Length)
			{
				goto Label_00E7;
			}
			return;
		Label_00D1:
			if ((ch < chMin) || (ch2 > chMax))
			{
				this.AddRange(ch, ch2);
			}
			index++;
		Label_00E7:
			if ((index < _lcTable.Length) && ((mapping = _lcTable[index])._chMin <= chMax))
			{
				if ((ch = mapping._chMin) < chMin)
				{
					ch = chMin;
				}
				if ((ch2 = mapping._chMax) > chMax)
				{
					ch2 = chMax;
				}
				switch (mapping._lcOp)
				{
					case 0:
						ch = (char) mapping._data;
						ch2 = (char) mapping._data;
						goto Label_00D1;

					case 1:
						ch = (char) (ch + ((char) mapping._data));
						ch2 = (char) (ch2 + ((char) mapping._data));
						goto Label_00D1;

					case 2:
						ch = (char) (ch | '\x0001');
						ch2 = (char) (ch2 | '\x0001');
						goto Label_00D1;

					case 3:
						ch = (char) (ch + ((char) (ch & '\x0001')));
						ch2 = (char) (ch2 + ((char) (ch2 & '\x0001')));
						goto Label_00D1;
				}
				goto Label_00D1;
			}
		}

		internal void AddRange(char first, char last)
		{
			this._rangelist.Add(new SingleRange(first, last));
			if ((this._canonical && (this._rangelist.Count > 0)) && (first <= this._rangelist[this._rangelist.Count - 1]._last))
			{
				this._canonical = false;
			}
		}

		private void AddSet(string set)
		{
			if ((this._canonical && (this.RangeCount() > 0)) && ((set.Length > 0) && (set[0] <= this.GetRangeAt(this.RangeCount() - 1)._last)))
			{
				this._canonical = false;
			}
			int num = 0;
			while (num < (set.Length - 1))
			{
				this._rangelist.Add(new SingleRange(set[num], (char) (set[num + 1] - '\x0001')));
				num += 2;
			}
			if (num < set.Length)
			{
				this._rangelist.Add(new SingleRange(set[num], (char)0xffff));
			}
		}

		internal void AddSpace(bool ecma, bool negate)
		{
			if (negate)
			{
				if (ecma)
				{
					this.AddSet("\0\t\x000e !");
				}
				else
				{
					this.AddCategory(NotSpace);
				}
			}
			else if (ecma)
			{
				this.AddSet("\t\x000e !");
			}
			else
			{
				this.AddCategory(Space);
			}
		}

		internal void AddSubtraction(RegexCharClass sub)
		{
			this._subtractor = sub;
		}

		internal void AddWord(bool ecma, bool negate)
		{
			if (negate)
			{
				if (ecma)
				{
					this.AddSet("\00:A[_`a{İı");
				}
				else
				{
					this.AddCategory(NotWord);
				}
			}
			else if (ecma)
			{
				this.AddSet("0:A[_`a{İı");
			}
			else
			{
				this.AddCategory(Word);
			}
		}

		private void Canonicalize()
		{
			char ch;
			this._canonical = true;
			this._rangelist.Sort(0, this._rangelist.Count, new SingleRangeComparer());
			if (this._rangelist.Count <= 1)
			{
				return;
			}
			bool flag = false;
			int num = 1;
			int index = 0;
		Label_003B:
			ch = this._rangelist[index]._last;
		Label_004D:
			if ((num == this._rangelist.Count) || (ch == 0xffff))
			{
				flag = true;
			}
			else
			{
				SingleRange range;
				if ((range = this._rangelist[num])._first <= (ch + '\x0001'))
				{
					if (ch < range._last)
					{
						ch = range._last;
					}
					num++;
					goto Label_004D;
				}
			}
			this._rangelist[index]._last = ch;
			index++;
			if (!flag)
			{
				if (index < num)
				{
					this._rangelist[index] = this._rangelist[num];
				}
				num++;
				goto Label_003B;
			}
			this._rangelist.RemoveRange(index, this._rangelist.Count - index);
		}

		private static bool CharInCategory(char ch, string set, int start, int mySetLength, int myCategoryLength)
		{
			UnicodeCategory unicodeCategory = char.GetUnicodeCategory(ch);
			int i = (start + 3) + mySetLength;
			int num2 = i + myCategoryLength;
			while (i < num2)
			{
				int num3 = (short) set[i];
				if (num3 == 0)
				{
					if (CharInCategoryGroup(ch, unicodeCategory, set, ref i))
					{
						return true;
					}
				}
				else
				{
					if (num3 > 0)
					{
						if (num3 != 100)
						{
							num3--;
							if ((int)unicodeCategory == num3)
							{
								return true;
							}
							goto Label_0070;
						}
						if (char.IsWhiteSpace(ch))
						{
							return true;
						}
						i++;
						continue;
					}
					if (num3 == -100)
					{
						if (!char.IsWhiteSpace(ch))
						{
							return true;
						}
						i++;
						continue;
					}
					num3 = -1 - num3;
					if ((int)unicodeCategory != num3)
					{
						return true;
					}
				}
			Label_0070:
				i++;
			}
			return false;
		}

		private static bool CharInCategoryGroup(char ch, UnicodeCategory chcategory, string category, ref int i)
		{
			i++;
			int num = (short) category[i];
			if (num > 0)
			{
				bool flag = false;
				while (num != 0)
				{
					if (!flag)
					{
						num--;
						if ((int)chcategory == num)
						{
							flag = true;
						}
					}
					i++;
					num = (short) category[i];
				}
				return flag;
			}
			bool flag2 = true;
			while (num != 0)
			{
				if (flag2)
				{
					num = -1 - num;
					if ((int)chcategory == num)
					{
						flag2 = false;
					}
				}
				i++;
				num = (short) category[i];
			}
			return flag2;
		}

		internal static bool CharInClass(char ch, string set)
		{
			return CharInClassRecursive(ch, set, 0);
		}

		private static bool CharInClassInternal(char ch, string set, int start, int mySetLength, int myCategoryLength)
		{
			int num = start + 3;
			int num2 = num + mySetLength;
			while (num != num2)
			{
				int num3 = (num + num2) / 2;
				if (ch < set[num3])
				{
					num2 = num3;
				}
				else
				{
					num = num3 + 1;
				}
			}
			if ((num & 1) == (start & 1))
			{
				return true;
			}
			if (myCategoryLength == 0)
			{
				return false;
			}
			return CharInCategory(ch, set, start, mySetLength, myCategoryLength);
		}

		internal static bool CharInClassRecursive(char ch, string set, int start)
		{
			int mySetLength = set[start + 1];
			int myCategoryLength = set[start + 2];
			int num3 = ((start + 3) + mySetLength) + myCategoryLength;
			bool flag = false;
			if (set.Length > num3)
			{
				flag = CharInClassRecursive(ch, set, num3);
			}
			bool flag2 = CharInClassInternal(ch, set, start, mySetLength, myCategoryLength);
			if (set[start] == '\x0001')
			{
				flag2 = !flag2;
			}
			return (flag2 && !flag);
		}

		internal static string ConvertOldStringsToClass(string set, string category)
		{
			StringBuilder builder = new StringBuilder((set.Length + category.Length) + 3);
			if (((set.Length >= 2) && (set[0] == '\0')) && (set[1] == '\0'))
			{
				builder.Append('\x0001');
				builder.Append((char) (set.Length - 2));
				builder.Append((char) category.Length);
				builder.Append(set.Substring(2));
			}
			else
			{
				builder.Append('\0');
				builder.Append((char) set.Length);
				builder.Append((char) category.Length);
				builder.Append(set);
			}
			builder.Append(category);
			return builder.ToString();
		}

		private SingleRange GetRangeAt(int i)
		{
			return this._rangelist[i];
		}

		internal static bool IsECMAWordChar(char ch)
		{
			return CharInClass(ch, "\0\n\00:A[_`a{İı");
		}

		internal static bool IsEmpty(string charClass)
		{
			return (((charClass[2] == '\0') && (charClass[0] == '\0')) && ((charClass[1] == '\0') && !IsSubtraction(charClass)));
		}

		internal static bool IsMergeable(string charClass)
		{
			return (!IsNegated(charClass) && !IsSubtraction(charClass));
		}

		internal static bool IsNegated(string set)
		{
			return ((set != null) && (set[0] == '\x0001'));
		}

		internal static bool IsSingleton(string set)
		{
			if ((((set[0] != '\0') || (set[2] != '\0')) || ((set[1] != '\x0002') || IsSubtraction(set))) || ((set[3] != 0xffff) && ((set[3] + '\x0001') != set[4])))
			{
				return false;
			}
			return true;
		}

		internal static bool IsSingletonInverse(string set)
		{
			if ((((set[0] != '\x0001') || (set[2] != '\0')) || ((set[1] != '\x0002') || IsSubtraction(set))) || ((set[3] != 0xffff) && ((set[3] + '\x0001') != set[4])))
			{
				return false;
			}
			return true;
		}

		private static bool IsSubtraction(string charClass)
		{
			return (charClass.Length > (('\x0003' + charClass[1]) + charClass[2]));
		}

		internal static bool IsWordChar(char ch)
		{
			if (!CharInClass(ch, WordClass) && (ch != '‍'))
			{
				return (ch == '‌');
			}
			return true;
		}

		private static string NegateCategory(string category)
		{
			if (category == null)
			{
				return null;
			}
			StringBuilder builder = new StringBuilder(category.Length);
			for (int i = 0; i < category.Length; i++)
			{
				short num2 = (short) category[i];
				builder.Append((char) ((ushort) -num2));
			}
			return builder.ToString();
		}

		internal static RegexCharClass Parse(string charClass)
		{
			return ParseRecursive(charClass, 0);
		}

		private static RegexCharClass ParseRecursive(string charClass, int start)
		{
			int capacity = charClass[start + 1];
			int length = charClass[start + 2];
			int num3 = ((start + 3) + capacity) + length;
			List<SingleRange> ranges = new List<SingleRange>(capacity);
			int num4 = start + 3;
			int startIndex = num4 + capacity;
			while (num4 < startIndex)
			{
				char ch2;
				char first = charClass[num4];
				num4++;
				if (num4 < startIndex)
				{
					ch2 = (char) (charClass[num4] - '\x0001');
				}
				else
				{
					ch2 = (char)0xffff;
				}
				num4++;
				ranges.Add(new SingleRange(first, ch2));
			}
			RegexCharClass subtraction = null;
			if (charClass.Length > num3)
			{
				subtraction = ParseRecursive(charClass, num3);
			}
			return new RegexCharClass(charClass[start] == '\x0001', ranges, new StringBuilder(charClass.Substring(startIndex, length)), subtraction);
		}

		private int RangeCount()
		{
			return this._rangelist.Count;
		}

		private static string SetFromProperty(string capname, bool invert, string pattern)
		{
			int num = 0;
			int length = _propTable.GetLength(0);
			while (num != length)
			{
				int num3 = (num + length) / 2;
				int num4 = string.Compare(capname, _propTable[num3, 0], StringComparison.Ordinal);
				if (num4 < 0)
				{
					length = num3;
				}
				else
				{
					if (num4 > 0)
					{
						num = num3 + 1;
						continue;
					}
					string str = _propTable[num3, 1];
					if (!invert)
					{
						return str;
					}
					if (str[0] == '\0')
					{
						return str.Substring(1);
					}
					return ('\0' + str);
				}
			}
			throw new ArgumentException("MakeException");
		}

		internal static char SingletonChar(string set)
		{
			return set[3];
		}

		internal string ToStringClass()
		{
			int num2;
			if (!this._canonical)
			{
				this.Canonicalize();
			}
			int num = this._rangelist.Count * 2;
			StringBuilder builder = new StringBuilder((num + this._categories.Length) + 3);
			if (this._negate)
			{
				num2 = 1;
			}
			else
			{
				num2 = 0;
			}
			builder.Append((char) num2);
			builder.Append((char) num);
			builder.Append((char) this._categories.Length);
			for (int i = 0; i < this._rangelist.Count; i++)
			{
				SingleRange range = this._rangelist[i];
				builder.Append(range._first);
				if (range._last != 0xffff)
				{
					builder.Append((char) (range._last + '\x0001'));
				}
			}
			builder[1] = (char) (builder.Length - 3);
			builder.Append(this._categories);
			if (this._subtractor != null)
			{
				builder.Append(this._subtractor.ToStringClass());
			}
			return builder.ToString();
		}

		// Properties
		internal bool CanMerge
		{
			get
			{
				return (!this._negate && (this._subtractor == null));
			}
		}

		internal bool Negate
		{
			set
			{
				this._negate = value;
			}
		}

		// Nested Types
		[StructLayout(LayoutKind.Sequential)]
		private struct LowerCaseMapping
		{
			internal char _chMin;
			internal char _chMax;
			internal int _lcOp;
			internal int _data;
			internal LowerCaseMapping(char chMin, char chMax, int lcOp, int data)
			{
				this._chMin = chMin;
				this._chMax = chMax;
				this._lcOp = lcOp;
				this._data = data;
			}
		}

		private sealed class SingleRange
		{
			// Fields
			internal char _first;
			internal char _last;

			// Methods
			internal SingleRange(char first, char last)
			{
				this._first = first;
				this._last = last;
			}
		}

		private sealed class SingleRangeComparer : IComparer<RegexCharClass.SingleRange>
		{
			// Methods
			public int Compare(RegexCharClass.SingleRange x, RegexCharClass.SingleRange y)
			{
				if (x._first < y._first)
				{
					return -1;
				}
				if (x._first <= y._first)
				{
					return 0;
				}
				return 1;
			}
		}
	}

	internal sealed class RegexParser
	{
		// Fields
		internal RegexNode _alternation;
		internal int _autocap;
		internal int _capcount;
		internal List<string> _capnamelist;
		internal Hashtable _capnames;
		internal int[] _capnumlist;
		internal Hashtable _caps;
		internal int _capsize;
		internal int _captop;
		internal static readonly byte[] _category = new byte[] { 
			0, 0, 0, 0, 0, 0, 0, 0, 0, 2, 2, 0, 2, 2, 0, 0, 
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 
			2, 0, 0, 3, 4, 0, 0, 0, 4, 4, 5, 5, 0, 0, 4, 0, 
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 5, 
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 4, 4, 0, 4, 0, 
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 5, 4, 0, 0, 0
		 };
		internal RegexNode _concatenation;
		internal CultureInfo _culture;
		internal int _currentPos;
		internal RegexNode _group;
		internal bool _ignoreNextParen;
		internal RegexOptions _options;
		internal List<RegexOptions> _optionsStack;
		internal string _pattern;
		internal RegexNode _stack;
		internal RegexNode _unit;
		internal const byte E = 1;
		internal const int MaxValueDiv10 = 0xccccccc;
		internal const int MaxValueMod10 = 7;
		internal const byte Q = 5;
		internal const byte S = 4;
		internal const byte X = 2;
		internal const byte Z = 3;

		// Methods
		private RegexParser(CultureInfo culture)
		{
			this._culture = culture;
			this._optionsStack = new List<RegexOptions>();
			this._caps = new Hashtable();
		}

		internal void AddAlternate()
		{
			if ((this._group.Type() == 0x22) || (this._group.Type() == 0x21))
			{
				this._group.AddChild(this._concatenation.ReverseLeft());
			}
			else
			{
				this._alternation.AddChild(this._concatenation.ReverseLeft());
			}
			this._concatenation = new RegexNode(0x19, this._options);
		}

		internal void AddConcatenate()
		{
			this._concatenation.AddChild(this._unit);
			this._unit = null;
		}

		internal void AddConcatenate(bool lazy, int min, int max)
		{
			this._concatenation.AddChild(this._unit.MakeQuantifier(lazy, min, max));
			this._unit = null;
		}

		internal void AddConcatenate(int pos, int cch, bool isReplacement)
		{
			if (cch != 0)
			{
				RegexNode node;
				if (cch > 1)
				{
					string str = this._pattern.Substring(pos, cch);
					if (this.UseOptionI() && !isReplacement)
					{
						StringBuilder builder = new StringBuilder(str.Length);
						for (int i = 0; i < str.Length; i++)
						{
							builder.Append(char.ToLower(str[i], this._culture));
						}
						str = builder.ToString();
					}
					node = new RegexNode(12, this._options, str);
				}
				else
				{
					char c = this._pattern[pos];
					if (this.UseOptionI() && !isReplacement)
					{
						c = char.ToLower(c, this._culture);
					}
					node = new RegexNode(9, this._options, c);
				}
				this._concatenation.AddChild(node);
			}
		}

		internal void AddGroup()
		{
			if ((this._group.Type() == 0x22) || (this._group.Type() == 0x21))
			{
				this._group.AddChild(this._concatenation.ReverseLeft());
				if (((this._group.Type() == 0x21) && (this._group.ChildCount() > 2)) || (this._group.ChildCount() > 3))
				{
					throw this.MakeException("TooManyAlternates");
				}
			}
			else
			{
				this._alternation.AddChild(this._concatenation.ReverseLeft());
				this._group.AddChild(this._alternation);
			}
			this._unit = this._group;
		}

		internal void AddUnitNode(RegexNode node)
		{
			this._unit = node;
		}

		internal void AddUnitNotone(char ch)
		{
			if (this.UseOptionI())
			{
				ch = char.ToLower(ch, this._culture);
			}
			this._unit = new RegexNode(10, this._options, ch);
		}

		internal void AddUnitOne(char ch)
		{
			if (this.UseOptionI())
			{
				ch = char.ToLower(ch, this._culture);
			}
			this._unit = new RegexNode(9, this._options, ch);
		}

		internal void AddUnitSet(string cc)
		{
			this._unit = new RegexNode(11, this._options, cc);
		}

		internal void AddUnitType(int type)
		{
			this._unit = new RegexNode(type, this._options);
		}

		internal void AssignNameSlots()
		{
			if (this._capnames != null)
			{
				for (int i = 0; i < this._capnamelist.Count; i++)
				{
					while (this.IsCaptureSlot(this._autocap))
					{
						this._autocap++;
					}
					string str = this._capnamelist[i];
					int pos = (int) this._capnames[str];
					this._capnames[str] = this._autocap;
					this.NoteCaptureSlot(this._autocap, pos);
					this._autocap++;
				}
			}
			if (this._capcount < this._captop)
			{
				this._capnumlist = new int[this._capcount];
				int num3 = 0;
				IDictionaryEnumerator enumerator = this._caps.GetEnumerator();
				while (enumerator.MoveNext())
				{
					this._capnumlist[num3++] = (int) enumerator.Key;
				}
				Array.Sort<int>(this._capnumlist, Comparer<int>.Default);
			}
			if ((this._capnames != null) || (this._capnumlist != null))
			{
				List<string> list;
				int num4;
				int num5 = 0;
				if (this._capnames == null)
				{
					list = null;
					this._capnames = new Hashtable();
					this._capnamelist = new List<string>();
					num4 = -1;
				}
				else
				{
					list = this._capnamelist;
					this._capnamelist = new List<string>();
					num4 = (int) this._capnames[list[0]];
				}
				for (int j = 0; j < this._capcount; j++)
				{
					int num7 = (this._capnumlist == null) ? j : this._capnumlist[j];
					if (num4 == num7)
					{
						this._capnamelist.Add(list[num5++]);
						num4 = (num5 == list.Count) ? -1 : ((int) this._capnames[list[num5]]);
					}
					else
					{
						string item = Convert.ToString(num7, this._culture);
						this._capnamelist.Add(item);
						this._capnames[item] = num7;
					}
				}
			}
		}

		internal int CaptureSlotFromName(string capname)
		{
			return (int) this._capnames[capname];
		}

		internal char CharAt(int i)
		{
			return this._pattern[i];
		}

		internal int CharsRight()
		{
			return (this._pattern.Length - this._currentPos);
		}

		internal void CountCaptures()
		{
			this.NoteCaptureSlot(0, 0);
			this._autocap = 1;
			while (this.CharsRight() > 0)
			{
				int pos = this.Textpos();
				switch (this.MoveRightGetChar())
				{
					case '(':
						if (((this.CharsRight() < 2) || (this.RightChar(1) != '#')) || (this.RightChar() != '?'))
						{
							break;
						}
						this.MoveLeft();
						this.ScanBlank();
						goto Label_01C2;

					case ')':
					{
						if (!this.EmptyOptionsStack())
						{
							this.PopOptions();
						}
						continue;
					}
					case '#':
					{
						if (this.UseOptionX())
						{
							this.MoveLeft();
							this.ScanBlank();
						}
						continue;
					}
					case '[':
					{
						this.ScanCharClass(false, true);
						continue;
					}
					case '\\':
					{
						if (this.CharsRight() > 0)
						{
							this.MoveRight();
						}
						continue;
					}
					default:
					{
						continue;
					}
				}
				this.PushOptions();
				if ((this.CharsRight() > 0) && (this.RightChar() == '?'))
				{
					this.MoveRight();
					if ((this.CharsRight() > 1) && ((this.RightChar() == '<') || (this.RightChar() == '\'')))
					{
						this.MoveRight();
						char ch = this.RightChar();
						if ((ch != '0') && RegexCharClass.IsWordChar(ch))
						{
							if ((ch >= '1') && (ch <= '9'))
							{
								this.NoteCaptureSlot(this.ScanDecimal(), pos);
							}
							else
							{
								this.NoteCaptureName(this.ScanCapname(), pos);
							}
						}
						goto Label_01C2;
					}
					this.ScanOptions();
					if (this.CharsRight() <= 0)
					{
						goto Label_01C2;
					}
					if (this.RightChar() == ')')
					{
						this.MoveRight();
						this.PopKeepOptions();
						goto Label_01C2;
					}
					if (this.RightChar() != '(')
					{
						goto Label_01C2;
					}
					this._ignoreNextParen = true;
					continue;
				}
				if (!this.UseOptionN() && !this._ignoreNextParen)
				{
					this.NoteCaptureSlot(this._autocap++, pos);
				}
			Label_01C2:
				this._ignoreNextParen = false;
			}
			this.AssignNameSlots();
		}

		internal bool EmptyOptionsStack()
		{
			return (this._optionsStack.Count == 0);
		}

		internal bool EmptyStack()
		{
			return (this._stack == null);
		}

		internal static string Escape(string input)
		{
			for (int i = 0; i < input.Length; i++)
			{
				if (!IsMetachar(input[i]))
				{
					continue;
				}
				StringBuilder builder = new StringBuilder();
				char ch = input[i];
				builder.Append(input, 0, i);
				do
				{
					builder.Append('\\');
					switch (ch)
					{
						case '\t':
							ch = 't';
							break;

						case '\n':
							ch = 'n';
							break;

						case '\f':
							ch = 'f';
							break;

						case '\r':
							ch = 'r';
							break;
					}
					builder.Append(ch);
					i++;
					int startIndex = i;
					while (i < input.Length)
					{
						ch = input[i];
						if (IsMetachar(ch))
						{
							break;
						}
						i++;
					}
					builder.Append(input, startIndex, i - startIndex);
				}
				while (i < input.Length);
				return builder.ToString();
			}
			return input;
		}

		internal static int HexDigit(char ch)
		{
			int num = ch - '0';
			if (num <= 9)
			{
				return num;
			}
			num = ch - 'a';
			if (num <= 5)
			{
				return (num + 10);
			}
			num = ch - 'A';
			if (num <= 5)
			{
				return (num + 10);
			}
			return -1;
		}

		internal bool IsCaptureName(string capname)
		{
			if (this._capnames == null)
			{
				return false;
			}
			return this._capnames.ContainsKey(capname);
		}

		internal bool IsCaptureSlot(int i)
		{
			if (this._caps != null)
			{
				return this._caps.ContainsKey(i);
			}
			return ((i >= 0) && (i < this._capsize));
		}

		internal static bool IsMetachar(char ch)
		{
			return ((ch <= '|') && (_category[ch] >= 1));
		}

		internal bool IsOnlyTopOption(RegexOptions option)
		{
			if (((option != RegexOptions.RightToLeft) && (option != RegexOptions.Compiled)) && (option != RegexOptions.CultureInvariant))
			{
				return (option == RegexOptions.ECMAScript);
			}
			return true;
		}

		internal static bool IsQuantifier(char ch)
		{
			return ((ch <= '{') && (_category[ch] >= 5));
		}

		internal static bool IsSpace(char ch)
		{
			return ((ch <= ' ') && (_category[ch] == 2));
		}

		internal static bool IsSpecial(char ch)
		{
			return ((ch <= '|') && (_category[ch] >= 4));
		}

		internal static bool IsStopperX(char ch)
		{
			return ((ch <= '|') && (_category[ch] >= 2));
		}

		internal bool IsTrueQuantifier()
		{
			int num = this.CharsRight();
			if (num == 0)
			{
				return false;
			}
			int i = this.Textpos();
			char index = this.CharAt(i);
			if (index == '{')
			{
				int num3 = i;
				while (((--num > 0) && ((index = this.CharAt(++num3)) >= '0')) && (index <= '9'))
				{
				}
				if ((num == 0) || ((num3 - i) == 1))
				{
					return false;
				}
				if (index == '}')
				{
					return true;
				}
				if (index != ',')
				{
					return false;
				}
				while (((--num > 0) && ((index = this.CharAt(++num3)) >= '0')) && (index <= '9'))
				{
				}
				return ((num > 0) && (index == '}'));
			}
			return ((index <= '{') && (_category[index] >= 5));
		}

		internal ArgumentException MakeException(string message)
		{
			return new ArgumentException(message);
		}

		internal void MoveLeft()
		{
			this._currentPos--;
		}

		internal void MoveRight()
		{
			this.MoveRight(1);
		}

		internal void MoveRight(int i)
		{
			this._currentPos += i;
		}

		internal char MoveRightGetChar()
		{
			return this._pattern[this._currentPos++];
		}

		internal void NoteCaptureName(string name, int pos)
		{
			if (this._capnames == null)
			{
				this._capnames = new Hashtable();
				this._capnamelist = new List<string>();
			}
			if (!this._capnames.ContainsKey(name))
			{
				this._capnames.Add(name, pos);
				this._capnamelist.Add(name);
			}
		}

		internal void NoteCaptures(Hashtable caps, int capsize, Hashtable capnames)
		{
			this._caps = caps;
			this._capsize = capsize;
			this._capnames = capnames;
		}

		internal void NoteCaptureSlot(int i, int pos)
		{
			if (!this._caps.ContainsKey(i))
			{
				this._caps.Add(i, pos);
				this._capcount++;
				if (this._captop <= i)
				{
					if (i == 0x7fffffff)
					{
						this._captop = i;
					}
					else
					{
						this._captop = i + 1;
					}
				}
			}
		}

		internal static RegexOptions OptionFromCode(char ch)
		{
			if ((ch >= 'A') && (ch <= 'Z'))
			{
				ch = (char) (ch + ' ');
			}
			switch (ch)
			{
				case 'c':
					return RegexOptions.Compiled;

				case 'e':
					return RegexOptions.ECMAScript;

				case 'i':
					return RegexOptions.IgnoreCase;

				case 'm':
					return RegexOptions.Multiline;

				case 'n':
					return RegexOptions.ExplicitCapture;

				case 'r':
					return RegexOptions.RightToLeft;

				case 's':
					return RegexOptions.Singleline;

				case 'x':
					return RegexOptions.IgnorePatternWhitespace;
			}
			return RegexOptions.None;
		}

		internal static RegexTree Parse(string re, RegexOptions op)
		{
			string[] strArray;
			RegexParser parser = new RegexParser(((op & RegexOptions.CultureInvariant) != RegexOptions.None) ? CultureInfo.InvariantCulture : CultureInfo.CurrentCulture);
			parser._options = op;
			parser.SetPattern(re);
			parser.CountCaptures();
			parser.Reset(op);
			RegexNode root = parser.ScanRegex();
			if (parser._capnamelist == null)
			{
				strArray = null;
			}
			else
			{
				strArray = parser._capnamelist.ToArray();
			}
			return new RegexTree(root, parser._caps, parser._capnumlist, parser._captop, parser._capnames, strArray, op);
		}

		internal string ParseProperty()
		{
			if (this.CharsRight() < 3)
			{
				throw this.MakeException("IncompleteSlashP");
			}
			if (this.MoveRightGetChar() != '{')
			{
				throw this.MakeException("MalformedSlashP");
			}
			int startIndex = this.Textpos();
			while (this.CharsRight() > 0)
			{
				char ch = this.MoveRightGetChar();
				if (!RegexCharClass.IsWordChar(ch) && (ch != '-'))
				{
					this.MoveLeft();
					break;
				}
			}
			string str = this._pattern.Substring(startIndex, this.Textpos() - startIndex);
			if ((this.CharsRight() == 0) || (this.MoveRightGetChar() != '}'))
			{
				throw this.MakeException("IncompleteSlashP");
			}
			return str;
		}

		internal static RegexReplacement ParseReplacement(string rep, Hashtable caps, int capsize, Hashtable capnames, RegexOptions op)
		{
			RegexParser parser = new RegexParser(((op & RegexOptions.CultureInvariant) != RegexOptions.None) ? CultureInfo.InvariantCulture : CultureInfo.CurrentCulture);
			parser._options = op;
			parser.NoteCaptures(caps, capsize, capnames);
			parser.SetPattern(rep);
			return new RegexReplacement(rep, parser.ScanReplacement(), caps);
		}

		internal void PopGroup()
		{
			this._concatenation = this._stack;
			this._alternation = this._concatenation._next;
			this._group = this._alternation._next;
			this._stack = this._group._next;
			if ((this._group.Type() == 0x22) && (this._group.ChildCount() == 0))
			{
				if (this._unit == null)
				{
					throw this.MakeException("IllegalCondition");
				}
				this._group.AddChild(this._unit);
				this._unit = null;
			}
		}

		internal void PopKeepOptions()
		{
			this._optionsStack.RemoveAt(this._optionsStack.Count - 1);
		}

		internal void PopOptions()
		{
			this._options = this._optionsStack[this._optionsStack.Count - 1];
			this._optionsStack.RemoveAt(this._optionsStack.Count - 1);
		}

		internal void PushGroup()
		{
			this._group._next = this._stack;
			this._alternation._next = this._group;
			this._concatenation._next = this._alternation;
			this._stack = this._concatenation;
		}

		internal void PushOptions()
		{
			this._optionsStack.Add(this._options);
		}

		internal void Reset(RegexOptions topopts)
		{
			this._currentPos = 0;
			this._autocap = 1;
			this._ignoreNextParen = false;
			if (this._optionsStack.Count > 0)
			{
				this._optionsStack.RemoveRange(0, this._optionsStack.Count - 1);
			}
			this._options = topopts;
			this._stack = null;
		}

		internal char RightChar()
		{
			return this._pattern[this._currentPos];
		}

		internal char RightChar(int i)
		{
			return this._pattern[this._currentPos + i];
		}

		internal RegexNode ScanBackslash()
		{
			char ch;
			if (this.CharsRight() == 0)
			{
				throw this.MakeException("IllegalEndEscape");
			}
			switch ((ch = this.RightChar()))
			{
				case 'S':
					this.MoveRight();
					if (this.UseOptionE())
					{
						return new RegexNode(11, this._options, "\x0001\x0004\0\t\x000e !");
					}
					return new RegexNode(11, this._options, RegexCharClass.NotSpaceClass);

				case 'W':
					this.MoveRight();
					if (this.UseOptionE())
					{
						return new RegexNode(11, this._options, "\x0001\n\00:A[_`a{İı");
					}
					return new RegexNode(11, this._options, RegexCharClass.NotWordClass);

				case 'Z':
				case 'A':
				case 'B':
				case 'G':
				case 'b':
				case 'z':
					this.MoveRight();
					return new RegexNode(this.TypeFromCode(ch), this._options);

				case 'D':
					this.MoveRight();
					if (!this.UseOptionE())
					{
						return new RegexNode(11, this._options, RegexCharClass.NotDigitClass);
					}
					return new RegexNode(11, this._options, "\x0001\x0002\00:");

				case 'P':
				case 'p':
				{
					this.MoveRight();
					RegexCharClass class2 = new RegexCharClass();
					class2.AddCategoryFromName(this.ParseProperty(), ch != 'p', this.UseOptionI(), this._pattern);
					if (this.UseOptionI())
					{
						class2.AddLowercase(this._culture);
					}
					return new RegexNode(11, this._options, class2.ToStringClass());
				}
				case 'd':
					this.MoveRight();
					if (!this.UseOptionE())
					{
						return new RegexNode(11, this._options, RegexCharClass.DigitClass);
					}
					return new RegexNode(11, this._options, "\0\x0002\00:");

				case 's':
					this.MoveRight();
					if (this.UseOptionE())
					{
						return new RegexNode(11, this._options, "\0\x0004\0\t\x000e !");
					}
					return new RegexNode(11, this._options, RegexCharClass.SpaceClass);

				case 'w':
					this.MoveRight();
					if (this.UseOptionE())
					{
						return new RegexNode(11, this._options, "\0\n\00:A[_`a{İı");
					}
					return new RegexNode(11, this._options, RegexCharClass.WordClass);
			}
			return this.ScanBasicBackslash();
		}

		internal RegexNode ScanBasicBackslash()
		{
			if (this.CharsRight() == 0)
			{
				throw this.MakeException("IllegalEndEscape");
			}
			bool flag = false;
			char ch2 = '\0';
			int pos = this.Textpos();
			char ch = this.RightChar();
			if (ch == 'k')
			{
				if (this.CharsRight() >= 2)
				{
					this.MoveRight();
					ch = this.MoveRightGetChar();
					switch (ch)
					{
						case '<':
						case '\'':
							flag = true;
							ch2 = (ch == '\'') ? '\'' : '>';
							break;
					}
				}
				if (!flag || (this.CharsRight() <= 0))
				{
					throw this.MakeException("MalformedNameRef");
				}
				ch = this.RightChar();
			}
			else if (((ch == '<') || (ch == '\'')) && (this.CharsRight() > 1))
			{
				flag = true;
				ch2 = (ch == '\'') ? '\'' : '>';
				this.MoveRight();
				ch = this.RightChar();
			}
			if ((flag && (ch >= '0')) && (ch <= '9'))
			{
				int i = this.ScanDecimal();
				if ((this.CharsRight() > 0) && (this.MoveRightGetChar() == ch2))
				{
					if (!this.IsCaptureSlot(i))
					{
						throw this.MakeException("UndefinedBackref");
					}
					return new RegexNode(13, this._options, i);
				}
			}
			else if ((flag || (ch < '1')) || (ch > '9'))
			{
				if (flag && RegexCharClass.IsWordChar(ch))
				{
					string capname = this.ScanCapname();
					if ((this.CharsRight() > 0) && (this.MoveRightGetChar() == ch2))
					{
						if (!this.IsCaptureName(capname))
						{
							throw this.MakeException("UndefinedNameRef");
						}
						return new RegexNode(13, this._options, this.CaptureSlotFromName(capname));
					}
				}
			}
			else if (!this.UseOptionE())
			{
				int num6 = this.ScanDecimal();
				if (this.IsCaptureSlot(num6))
				{
					return new RegexNode(13, this._options, num6);
				}
				if (num6 <= 9)
				{
					throw this.MakeException("UndefinedBackref");
				}
			}
			else
			{
				int m = -1;
				int num4 = ch - '0';
				int num5 = this.Textpos() - 1;
				while (num4 <= this._captop)
				{
					if (this.IsCaptureSlot(num4) && ((this._caps == null) || (((int) this._caps[num4]) < num5)))
					{
						m = num4;
					}
					this.MoveRight();
					if (((this.CharsRight() == 0) || ((ch = this.RightChar()) < '0')) || (ch > '9'))
					{
						break;
					}
					num4 = (num4 * 10) + (ch - '0');
				}
				if (m >= 0)
				{
					return new RegexNode(13, this._options, m);
				}
			}
			this.Textto(pos);
			ch = this.ScanCharEscape();
			if (this.UseOptionI())
			{
				ch = char.ToLower(ch, this._culture);
			}
			return new RegexNode(9, this._options, ch);
		}

		internal void ScanBlank()
		{
			if (this.UseOptionX())
			{
				while (true)
				{
					while ((this.CharsRight() > 0) && IsSpace(this.RightChar()))
					{
						this.MoveRight();
					}
					if (this.CharsRight() == 0)
					{
						return;
					}
					if (this.RightChar() == '#')
					{
						while ((this.CharsRight() > 0) && (this.RightChar() != '\n'))
						{
							this.MoveRight();
						}
					}
					else
					{
						if (((this.CharsRight() < 3) || (this.RightChar(2) != '#')) || ((this.RightChar(1) != '?') || (this.RightChar() != '(')))
						{
							return;
						}
						while ((this.CharsRight() > 0) && (this.RightChar() != ')'))
						{
							this.MoveRight();
						}
						if (this.CharsRight() == 0)
						{
							throw this.MakeException("UnterminatedComment");
						}
						this.MoveRight();
					}
				}
			}
			while (((this.CharsRight() >= 3) && (this.RightChar(2) == '#')) && ((this.RightChar(1) == '?') && (this.RightChar() == '(')))
			{
				while ((this.CharsRight() > 0) && (this.RightChar() != ')'))
				{
					this.MoveRight();
				}
				if (this.CharsRight() == 0)
				{
					throw this.MakeException("UnterminatedComment");
				}
				this.MoveRight();
			}
		}

		internal string ScanCapname()
		{
			int startIndex = this.Textpos();
			while (this.CharsRight() > 0)
			{
				if (!RegexCharClass.IsWordChar(this.MoveRightGetChar()))
				{
					this.MoveLeft();
					break;
				}
			}
			return this._pattern.Substring(startIndex, this.Textpos() - startIndex);
		}

		internal RegexCharClass ScanCharClass(bool caseInsensitive)
		{
			return this.ScanCharClass(caseInsensitive, false);
		}

		internal RegexCharClass ScanCharClass(bool caseInsensitive, bool scanOnly)
		{
			char first = '\0';
			char c = '\0';
			bool flag = false;
			bool flag2 = true;
			bool flag3 = false;
			RegexCharClass class2 = scanOnly ? null : new RegexCharClass();
			if ((this.CharsRight() > 0) && (this.RightChar() == '^'))
			{
				this.MoveRight();
				if (!scanOnly)
				{
					class2.Negate = true;
				}
			}
			while (this.CharsRight() > 0)
			{
				bool flag4 = false;
				first = this.MoveRightGetChar();
				if (first == ']')
				{
					if (flag2)
					{
						goto Label_029F;
					}
					flag3 = true;
					break;
				}
				if ((first == '\\') && (this.CharsRight() > 0))
				{
					switch ((first = this.MoveRightGetChar()))
					{
						case '-':
							if (!scanOnly)
							{
								class2.AddRange(first, first);
							}
							goto Label_03BE;

						case 'D':
						case 'd':
							if (!scanOnly)
							{
								if (flag)
								{
									throw this.MakeException("BadClassInCharRange");
								}
								class2.AddDigit(this.UseOptionE(), first == 'D', this._pattern);
							}
							goto Label_03BE;

						case 'P':
						case 'p':
							if (!scanOnly)
							{
								if (flag)
								{
									throw this.MakeException("BadClassInCharRange");
								}
								class2.AddCategoryFromName(this.ParseProperty(), first != 'p', caseInsensitive, this._pattern);
							}
							else
							{
								this.ParseProperty();
							}
							goto Label_03BE;

						case 'S':
						case 's':
							if (!scanOnly)
							{
								if (flag)
								{
									throw this.MakeException(SR.GetString("BadClassInCharRange", new object[] { first.ToString() }));
								}
								class2.AddSpace(this.UseOptionE(), first == 'S');
							}
							goto Label_03BE;

						case 'W':
						case 'w':
							if (!scanOnly)
							{
								if (flag)
								{
									throw this.MakeException(SR.GetString("BadClassInCharRange", new object[] { first.ToString() }));
								}
								class2.AddWord(this.UseOptionE(), first == 'W');
							}
							goto Label_03BE;
					}
					this.MoveLeft();
					first = this.ScanCharEscape();
					flag4 = true;
				}
				else if (((first == '[') && (this.CharsRight() > 0)) && ((this.RightChar() == ':') && !flag))
				{
					int pos = this.Textpos();
					this.MoveRight();
					this.ScanCapname();
					if (((this.CharsRight() < 2) || (this.MoveRightGetChar() != ':')) || (this.MoveRightGetChar() != ']'))
					{
						this.Textto(pos);
					}
				}
			Label_029F:
				if (flag)
				{
					flag = false;
					if (!scanOnly)
					{
						if (((first == '[') && !flag4) && !flag2)
						{
							class2.AddChar(c);
							class2.AddSubtraction(this.ScanCharClass(caseInsensitive, false));
							if ((this.CharsRight() > 0) && (this.RightChar() != ']'))
							{
								throw this.MakeException(SR.GetString("SubtractionMustBeLast"));
							}
						}
						else
						{
							if (c > first)
							{
								throw this.MakeException(SR.GetString("ReversedCharRange"));
							}
							class2.AddRange(c, first);
						}
					}
				}
				else if (((this.CharsRight() >= 2) && (this.RightChar() == '-')) && (this.RightChar(1) != ']'))
				{
					c = first;
					flag = true;
					this.MoveRight();
				}
				else if ((((this.CharsRight() >= 1) && (first == '-')) && (!flag4 && (this.RightChar() == '['))) && !flag2)
				{
					if (!scanOnly)
					{
						this.MoveRight(1);
						class2.AddSubtraction(this.ScanCharClass(caseInsensitive, false));
						if ((this.CharsRight() > 0) && (this.RightChar() != ']'))
						{
							throw this.MakeException(SR.GetString("SubtractionMustBeLast"));
						}
					}
					else
					{
						this.MoveRight(1);
						this.ScanCharClass(caseInsensitive, true);
					}
				}
				else if (!scanOnly)
				{
					class2.AddRange(first, first);
				}
			Label_03BE:
				flag2 = false;
			}
			if (!flag3)
			{
				throw this.MakeException(SR.GetString("UnterminatedBracket"));
			}
			if (!scanOnly && caseInsensitive)
			{
				class2.AddLowercase(this._culture);
			}
			return class2;
		}

		internal char ScanCharEscape()
		{
			char ch = this.MoveRightGetChar();
			if ((ch >= '0') && (ch <= '7'))
			{
				this.MoveLeft();
				return this.ScanOctal();
			}
			switch (ch)
			{
				case 'a':
					return '\a';

				case 'b':
					return '\b';

				case 'c':
					return this.ScanControl();

				case 'e':
					return '\x001b';

				case 'f':
					return '\f';

				case 'n':
					return '\n';

				case 'r':
					return '\r';

				case 't':
					return '\t';

				case 'u':
					return this.ScanHex(4);

				case 'v':
					return '\v';

				case 'x':
					return this.ScanHex(2);
			}
			if (!this.UseOptionE() && RegexCharClass.IsWordChar(ch))
			{
				throw this.MakeException(SR.GetString("UnrecognizedEscape", new object[] { ch.ToString() }));
			}
			return ch;
		}

		internal char ScanControl()
		{
			if (this.CharsRight() <= 0)
			{
				throw this.MakeException(SR.GetString("MissingControl"));
			}
			char ch = this.MoveRightGetChar();
			if ((ch >= 'a') && (ch <= 'z'))
			{
				ch = (char) (ch - ' ');
			}
			ch = (char) (ch - '@');
			if (ch >= ' ')
			{
				throw this.MakeException(SR.GetString("UnrecognizedControl"));
			}
			return ch;
		}

		internal int ScanDecimal()
		{
			int num2;
			int num = 0;
			while ((this.CharsRight() > 0) && ((num2 = this.RightChar() - '0') <= 9))
			{
				this.MoveRight();
				if ((num > 0xccccccc) || ((num == 0xccccccc) && (num2 > 7)))
				{
					throw this.MakeException(SR.GetString("CaptureGroupOutOfRange"));
				}
				num *= 10;
				num += num2;
			}
			return num;
		}

		internal RegexNode ScanDollar()
		{
			if (this.CharsRight() != 0)
			{
				bool flag;
				char ch = this.RightChar();
				int pos = this.Textpos();
				int num2 = pos;
				if ((ch == '{') && (this.CharsRight() > 1))
				{
					flag = true;
					this.MoveRight();
					ch = this.RightChar();
				}
				else
				{
					flag = false;
				}
				if ((ch >= '0') && (ch <= '9'))
				{
					if (flag || !this.UseOptionE())
					{
						int i = this.ScanDecimal();
						if ((!flag || ((this.CharsRight() > 0) && (this.MoveRightGetChar() == '}'))) && this.IsCaptureSlot(i))
						{
							return new RegexNode(13, this._options, i);
						}
					}
					else
					{
						int m = -1;
						int num4 = ch - '0';
						this.MoveRight();
						if (this.IsCaptureSlot(num4))
						{
							m = num4;
							num2 = this.Textpos();
						}
						while (((this.CharsRight() > 0) && ((ch = this.RightChar()) >= '0')) && (ch <= '9'))
						{
							int num5 = ch - '0';
							if ((num4 > 0xccccccc) || ((num4 == 0xccccccc) && (num5 > 7)))
							{
								throw this.MakeException(SR.GetString("CaptureGroupOutOfRange"));
							}
							num4 = (num4 * 10) + num5;
							this.MoveRight();
							if (this.IsCaptureSlot(num4))
							{
								m = num4;
								num2 = this.Textpos();
							}
						}
						this.Textto(num2);
						if (m >= 0)
						{
							return new RegexNode(13, this._options, m);
						}
					}
				}
				else if (flag && RegexCharClass.IsWordChar(ch))
				{
					string capname = this.ScanCapname();
					if (((this.CharsRight() > 0) && (this.MoveRightGetChar() == '}')) && this.IsCaptureName(capname))
					{
						return new RegexNode(13, this._options, this.CaptureSlotFromName(capname));
					}
				}
				else if (!flag)
				{
					int num7 = 1;
					switch (ch)
					{
						case '$':
							this.MoveRight();
							return new RegexNode(9, this._options, '$');

						case '&':
							num7 = 0;
							break;

						case '\'':
							num7 = -2;
							break;

						case '+':
							num7 = -3;
							break;

						case '_':
							num7 = -4;
							break;

						case '`':
							num7 = -1;
							break;
					}
					if (num7 != 1)
					{
						this.MoveRight();
						return new RegexNode(13, this._options, num7);
					}
				}
				this.Textto(pos);
			}
			return new RegexNode(9, this._options, '$');
		}

		internal RegexNode ScanGroupOpen()
		{
			int num;
			int num4;
			char ch = '\0';
			char ch2 = '>';
			if (((this.CharsRight() == 0) || (this.RightChar() != '?')) || (((this.RightChar() == '?') && (this.CharsRight() > 1)) && (this.RightChar(1) == ')')))
			{
				if (!this.UseOptionN() && !this._ignoreNextParen)
				{
					return new RegexNode(0x1c, this._options, this._autocap++, -1);
				}
				this._ignoreNextParen = false;
				return new RegexNode(0x1d, this._options);
			}
			this.MoveRight();
			if (this.CharsRight() == 0)
			{
				goto Label_055F;
			}
			switch ((ch = this.MoveRightGetChar()))
			{
				case '\'':
					ch2 = '\'';
					break;

				case '(':
				{
					num4 = this.Textpos();
					if (this.CharsRight() <= 0)
					{
						goto Label_048D;
					}
					ch = this.RightChar();
					if ((ch < '0') || (ch > '9'))
					{
						if (RegexCharClass.IsWordChar(ch))
						{
							string capname = this.ScanCapname();
							if ((this.IsCaptureName(capname) && (this.CharsRight() > 0)) && (this.MoveRightGetChar() == ')'))
							{
								return new RegexNode(0x21, this._options, this.CaptureSlotFromName(capname));
							}
						}
						goto Label_048D;
					}
					int i = this.ScanDecimal();
					if ((this.CharsRight() <= 0) || (this.MoveRightGetChar() != ')'))
					{
						throw this.MakeException(SR.GetString("MalformedReference", new object[] { i.ToString(CultureInfo.CurrentCulture) }));
					}
					if (!this.IsCaptureSlot(i))
					{
						throw this.MakeException(SR.GetString("UndefinedReference", new object[] { i.ToString(CultureInfo.CurrentCulture) }));
					}
					return new RegexNode(0x21, this._options, i);
				}
				case '!':
					this._options &= ~RegexOptions.RightToLeft;
					num = 0x1f;
					goto Label_0552;

				case ':':
					num = 0x1d;
					goto Label_0552;

				case '<':
					break;

				case '=':
					this._options &= ~RegexOptions.RightToLeft;
					num = 30;
					goto Label_0552;

				case '>':
					num = 0x20;
					goto Label_0552;

				default:
					goto Label_0528;
			}
			if (this.CharsRight() == 0)
			{
				goto Label_055F;
			}
			char ch5 = ch = this.MoveRightGetChar();
			if (ch5 != '!')
			{
				if (ch5 != '=')
				{
					this.MoveLeft();
					int num2 = -1;
					int num3 = -1;
					bool flag = false;
					if ((ch >= '0') && (ch <= '9'))
					{
						num2 = this.ScanDecimal();
						if (!this.IsCaptureSlot(num2))
						{
							num2 = -1;
						}
						if (((this.CharsRight() > 0) && (this.RightChar() != ch2)) && (this.RightChar() != '-'))
						{
							throw this.MakeException(SR.GetString("InvalidGroupName"));
						}
						if (num2 == 0)
						{
							throw this.MakeException(SR.GetString("CapnumNotZero"));
						}
					}
					else if (RegexCharClass.IsWordChar(ch))
					{
						string str = this.ScanCapname();
						if (this.IsCaptureName(str))
						{
							num2 = this.CaptureSlotFromName(str);
						}
						if (((this.CharsRight() > 0) && (this.RightChar() != ch2)) && (this.RightChar() != '-'))
						{
							throw this.MakeException(SR.GetString("InvalidGroupName"));
						}
					}
					else
					{
						if (ch != '-')
						{
							throw this.MakeException(SR.GetString("InvalidGroupName"));
						}
						flag = true;
					}
					if (((num2 != -1) || flag) && ((this.CharsRight() > 0) && (this.RightChar() == '-')))
					{
						this.MoveRight();
						ch = this.RightChar();
						if ((ch >= '0') && (ch <= '9'))
						{
							num3 = this.ScanDecimal();
							if (!this.IsCaptureSlot(num3))
							{
								throw this.MakeException(SR.GetString("UndefinedBackref", new object[] { num3 }));
							}
							if ((this.CharsRight() > 0) && (this.RightChar() != ch2))
							{
								throw this.MakeException(SR.GetString("InvalidGroupName"));
							}
						}
						else
						{
							if (!RegexCharClass.IsWordChar(ch))
							{
								throw this.MakeException(SR.GetString("InvalidGroupName"));
							}
							string str2 = this.ScanCapname();
							if (!this.IsCaptureName(str2))
							{
								throw this.MakeException(SR.GetString("UndefinedNameRef", new object[] { str2 }));
							}
							num3 = this.CaptureSlotFromName(str2);
							if ((this.CharsRight() > 0) && (this.RightChar() != ch2))
							{
								throw this.MakeException(SR.GetString("InvalidGroupName"));
							}
						}
					}
					if (((num2 != -1) || (num3 != -1)) && ((this.CharsRight() > 0) && (this.MoveRightGetChar() == ch2)))
					{
						return new RegexNode(0x1c, this._options, num2, num3);
					}
					goto Label_055F;
				}
				if (ch2 == '\'')
				{
					goto Label_055F;
				}
				this._options |= RegexOptions.RightToLeft;
				num = 30;
			}
			else
			{
				if (ch2 == '\'')
				{
					goto Label_055F;
				}
				this._options |= RegexOptions.RightToLeft;
				num = 0x1f;
			}
			goto Label_0552;
		Label_048D:
			num = 0x22;
			this.Textto(num4 - 1);
			this._ignoreNextParen = true;
			int num6 = this.CharsRight();
			if ((num6 < 3) || (this.RightChar(1) != '?'))
			{
				goto Label_0552;
			}
			char ch3 = this.RightChar(2);
			switch (ch3)
			{
				case '#':
					throw this.MakeException(SR.GetString("AlternationCantHaveComment"));

				case '\'':
					throw this.MakeException(SR.GetString("AlternationCantCapture"));

				default:
					if (((num6 >= 4) && (ch3 == '<')) && ((this.RightChar(3) != '!') && (this.RightChar(3) != '=')))
					{
						throw this.MakeException(SR.GetString("AlternationCantCapture"));
					}
					goto Label_0552;
			}
		Label_0528:
			this.MoveLeft();
			num = 0x1d;
			this.ScanOptions();
			if (this.CharsRight() == 0)
			{
				goto Label_055F;
			}
			ch = this.MoveRightGetChar();
			if (ch == ')')
			{
				return null;
			}
			if (ch != ':')
			{
				goto Label_055F;
			}
		Label_0552:
			return new RegexNode(num, this._options);
		Label_055F:
			throw this.MakeException(SR.GetString("UnrecognizedGrouping"));
		}

		internal char ScanHex(int c)
		{
			int num = 0;
			if (this.CharsRight() >= c)
			{
				int num2;
				while ((c > 0) && ((num2 = HexDigit(this.MoveRightGetChar())) >= 0))
				{
					num *= 0x10;
					num += num2;
					c--;
				}
			}
			if (c > 0)
			{
				throw this.MakeException(SR.GetString("TooFewHex"));
			}
			return (char) num;
		}

		internal char ScanOctal()
		{
			int num;
			int num3 = 3;
			if (num3 > this.CharsRight())
			{
				num3 = this.CharsRight();
			}
			int num2 = 0;
			while ((num3 > 0) && ((num = this.RightChar() - '0') <= 7))
			{
				this.MoveRight();
				num2 *= 8;
				num2 += num;
				if (this.UseOptionE() && (num2 >= 0x20))
				{
					break;
				}
				num3--;
			}
			num2 &= 0xff;
			return (char) num2;
		}

		internal void ScanOptions()
		{
			bool flag = false;
			while (this.CharsRight() > 0)
			{
				char ch = this.RightChar();
				switch (ch)
				{
					case '-':
						flag = true;
						break;

					case '+':
						flag = false;
						break;

					default:
					{
						RegexOptions option = OptionFromCode(ch);
						if ((option == RegexOptions.None) || this.IsOnlyTopOption(option))
						{
							return;
						}
						if (flag)
						{
							this._options &= ~option;
						}
						else
						{
							this._options |= option;
						}
						break;
					}
				}
				this.MoveRight();
			}
		}

		internal RegexNode ScanRegex()
		{
			char ch = '@';
			bool flag = false;
			this.StartGroup(new RegexNode(0x1c, this._options, 0, -1));
		Label_043F:
			while (this.CharsRight() > 0)
			{
				int num2;
				RegexNode node;
				bool flag2 = flag;
				flag = false;
				this.ScanBlank();
				int pos = this.Textpos();
				if (!this.UseOptionX())
				{
					goto Label_006D;
				}
				while ((this.CharsRight() > 0) && (!IsStopperX(ch = this.RightChar()) || ((ch == '{') && !this.IsTrueQuantifier())))
				{
					this.MoveRight();
				}
				goto Label_0092;
			Label_0067:
				this.MoveRight();
			Label_006D:
				if ((this.CharsRight() > 0) && (!IsSpecial(ch = this.RightChar()) || ((ch == '{') && !this.IsTrueQuantifier())))
				{
					goto Label_0067;
				}
			Label_0092:
				num2 = this.Textpos();
				this.ScanBlank();
				if (this.CharsRight() == 0)
				{
					ch = '!';
				}
				else if (IsSpecial(ch = this.RightChar()))
				{
					flag = IsQuantifier(ch);
					this.MoveRight();
				}
				else
				{
					ch = ' ';
				}
				if (pos < num2)
				{
					int cch = (num2 - pos) - (flag ? 1 : 0);
					flag2 = false;
					if (cch > 0)
					{
						this.AddConcatenate(pos, cch, false);
					}
					if (flag)
					{
						this.AddUnitOne(this.CharAt(num2 - 1));
					}
				}
				switch (ch)
				{
					case ' ':
					{
						continue;
					}
					case '!':
						goto Label_044B;

					case '$':
						this.AddUnitType(this.UseOptionM() ? 15 : 20);
						goto Label_02D9;

					case '(':
					{
						this.PushOptions();
						node = this.ScanGroupOpen();
						if (node != null)
						{
							break;
						}
						this.PopKeepOptions();
						continue;
					}
					case ')':
						if (this.EmptyStack())
						{
							throw this.MakeException(SR.GetString("TooManyParens"));
						}
						goto Label_0202;

					case '*':
					case '+':
					case '?':
					case '{':
						if (this.Unit() == null)
						{
							throw this.MakeException(flag2 ? SR.GetString("NestedQuantify", new object[] { ch.ToString() }) : SR.GetString("QuantifyAfterNothing"));
						}
						this.MoveLeft();
						goto Label_02D9;

					case '.':
						if (!this.UseOptionS())
						{
							goto Label_0279;
						}
						this.AddUnitSet("\0\x0001\0\0");
						goto Label_02D9;

					case '[':
						this.AddUnitSet(this.ScanCharClass(this.UseOptionI()).ToStringClass());
						goto Label_02D9;

					case '\\':
						this.AddUnitNode(this.ScanBackslash());
						goto Label_02D9;

					case '^':
						this.AddUnitType(this.UseOptionM() ? 14 : 0x12);
						goto Label_02D9;

					case '|':
					{
						this.AddAlternate();
						continue;
					}
					default:
						throw this.MakeException(SR.GetString("InternalError"));
				}
				this.PushGroup();
				this.StartGroup(node);
				continue;
			Label_0202:
				this.AddGroup();
				this.PopGroup();
				this.PopOptions();
				if (this.Unit() != null)
				{
					goto Label_02D9;
				}
				continue;
			Label_0279:
				this.AddUnitNotone('\n');
			Label_02D9:
				this.ScanBlank();
				if ((this.CharsRight() == 0) || !(flag = this.IsTrueQuantifier()))
				{
					this.AddConcatenate();
				}
				else
				{
					ch = this.MoveRightGetChar();
					while (this.Unit() != null)
					{
						int num4;
						int num5;
						bool flag3;
						switch (ch)
						{
							case '*':
								num4 = 0;
								num5 = 0x7fffffff;
								goto Label_03EB;

							case '+':
								num4 = 1;
								num5 = 0x7fffffff;
								goto Label_03EB;

							case '?':
								num4 = 0;
								num5 = 1;
								goto Label_03EB;

							case '{':
								pos = this.Textpos();
								num5 = num4 = this.ScanDecimal();
								if (((pos < this.Textpos()) && (this.CharsRight() > 0)) && (this.RightChar() == ','))
								{
									this.MoveRight();
									if ((this.CharsRight() != 0) && (this.RightChar() != '}'))
									{
										break;
									}
									num5 = 0x7fffffff;
								}
								goto Label_03AE;

							default:
								throw this.MakeException(SR.GetString("InternalError"));
						}
						num5 = this.ScanDecimal();
					Label_03AE:
						if (((pos == this.Textpos()) || (this.CharsRight() == 0)) || (this.MoveRightGetChar() != '}'))
						{
							this.AddConcatenate();
							this.Textto(pos - 1);
							goto Label_043F;
						}
					Label_03EB:
						this.ScanBlank();
						if ((this.CharsRight() == 0) || (this.RightChar() != '?'))
						{
							flag3 = false;
						}
						else
						{
							this.MoveRight();
							flag3 = true;
						}
						if (num4 > num5)
						{
							throw this.MakeException(SR.GetString("IllegalRange"));
						}
						this.AddConcatenate(flag3, num4, num5);
					}
				}
			}
		Label_044B:
			if (!this.EmptyStack())
			{
				throw this.MakeException(SR.GetString("NotEnoughParens"));
			}
			this.AddGroup();
			return this.Unit();
		}

		internal RegexNode ScanReplacement()
		{
			this._concatenation = new RegexNode(0x19, this._options);
			while (true)
			{
				int num;
				do
				{
					num = this.CharsRight();
					if (num == 0)
					{
						return this._concatenation;
					}
					int pos = this.Textpos();
					while ((num > 0) && (this.RightChar() != '$'))
					{
						this.MoveRight();
						num--;
					}
					this.AddConcatenate(pos, this.Textpos() - pos, true);
				}
				while (num <= 0);
				if (this.MoveRightGetChar() == '$')
				{
					this.AddUnitNode(this.ScanDollar());
				}
				this.AddConcatenate();
			}
		}

		internal void SetPattern(string Re)
		{
			if (Re == null)
			{
				Re = string.Empty;
			}
			this._pattern = Re;
			this._currentPos = 0;
		}

		internal void StartGroup(RegexNode openGroup)
		{
			this._group = openGroup;
			this._alternation = new RegexNode(0x18, this._options);
			this._concatenation = new RegexNode(0x19, this._options);
		}

		internal int Textpos()
		{
			return this._currentPos;
		}

		internal void Textto(int pos)
		{
			this._currentPos = pos;
		}

		internal int TypeFromCode(char ch)
		{
			switch (ch)
			{
				case 'A':
					return 0x12;

				case 'B':
					if (this.UseOptionE())
					{
						return 0x2a;
					}
					return 0x11;

				case 'G':
					return 0x13;

				case 'Z':
					return 20;

				case 'b':
					if (!this.UseOptionE())
					{
						return 0x10;
					}
					return 0x29;

				case 'z':
					return 0x15;
			}
			return 0x16;
		}

		internal static string Unescape(string input)
		{
			for (int i = 0; i < input.Length; i++)
			{
				if (input[i] == '\\')
				{
					StringBuilder builder = new StringBuilder();
					RegexParser parser = new RegexParser(CultureInfo.InvariantCulture);
					parser.SetPattern(input);
					builder.Append(input, 0, i);
					do
					{
						i++;
						parser.Textto(i);
						if (i < input.Length)
						{
							builder.Append(parser.ScanCharEscape());
						}
						i = parser.Textpos();
						int startIndex = i;
						while ((i < input.Length) && (input[i] != '\\'))
						{
							i++;
						}
						builder.Append(input, startIndex, i - startIndex);
					}
					while (i < input.Length);
					return builder.ToString();
				}
			}
			return input;
		}

		internal RegexNode Unit()
		{
			return this._unit;
		}

		internal bool UseOptionE()
		{
			return ((this._options & RegexOptions.ECMAScript) != RegexOptions.None);
		}

		internal bool UseOptionI()
		{
			return ((this._options & RegexOptions.IgnoreCase) != RegexOptions.None);
		}

		internal bool UseOptionM()
		{
			return ((this._options & RegexOptions.Multiline) != RegexOptions.None);
		}

		internal bool UseOptionN()
		{
			return ((this._options & RegexOptions.ExplicitCapture) != RegexOptions.None);
		}

		internal bool UseOptionS()
		{
			return ((this._options & RegexOptions.Singleline) != RegexOptions.None);
		}

		internal bool UseOptionX()
		{
			return ((this._options & RegexOptions.IgnorePatternWhitespace) != RegexOptions.None);
		}
	}

	internal sealed class RegexFCD
	{
		// Fields
		private bool _failed;
		private int _fcDepth;
		private RegexFC[] _fcStack = new RegexFC[0x20];
		private int _intDepth;
		private int[] _intStack = new int[0x20];
		private bool _skipAllChildren;
		private bool _skipchild;
		private const int AfterChild = 0x80;
		private const int BeforeChild = 0x40;
		internal const int Beginning = 1;
		internal const int Bol = 2;
		internal const int Boundary = 0x40;
		internal const int ECMABoundary = 0x80;
		internal const int End = 0x20;
		internal const int EndZ = 0x10;
		internal const int Eol = 8;
		internal const int Start = 4;

		// Methods
		private RegexFCD()
		{
		}

		private static int AnchorFromType(int type)
		{
			switch (type)
			{
				case 14:
					return 2;

				case 15:
					return 8;

				case 0x10:
					return 0x40;

				case 0x12:
					return 1;

				case 0x13:
					return 4;

				case 20:
					return 0x10;

				case 0x15:
					return 0x20;

				case 0x29:
					return 0x80;
			}
			return 0;
		}

		internal static int Anchors(RegexTree tree)
		{
			RegexNode node2 = null;
			int num = 0;
			int num2 = 0;
			RegexNode node = tree._root;
		Label_000D:
			switch (node._type)
			{
				case 14:
				case 15:
				case 0x10:
				case 0x12:
				case 0x13:
				case 20:
				case 0x15:
				case 0x29:
					return (num2 | AnchorFromType(node._type));

				case 0x11:
				case 0x16:
				case 0x18:
				case 0x1a:
				case 0x1b:
				case 0x1d:
					return num2;

				case 0x17:
				case 30:
				case 0x1f:
					break;

				case 0x19:
					if (node.ChildCount() > 0)
					{
						node2 = node;
						num = 0;
					}
					break;

				case 0x1c:
				case 0x20:
					node = node.Child(0);
					node2 = null;
					goto Label_000D;

				default:
					return num2;
			}
			if ((node2 == null) || (num >= node2.ChildCount()))
			{
				return num2;
			}
			node = node2.Child(num++);
			goto Label_000D;
		}

		private void CalculateFC(int NodeType, RegexNode node, int CurIndex)
		{
			bool caseInsensitive = false;
			bool flag2 = false;
			if (NodeType <= 13)
			{
				if ((node._options & RegexOptions.IgnoreCase) != RegexOptions.None)
				{
					caseInsensitive = true;
				}
				if ((node._options & RegexOptions.RightToLeft) != RegexOptions.None)
				{
					flag2 = true;
				}
			}
			switch (NodeType)
			{
				case 3:
				case 6:
					this.PushFC(new RegexFC(node._ch, false, node._m == 0, caseInsensitive));
					return;

				case 4:
				case 7:
					this.PushFC(new RegexFC(node._ch, true, node._m == 0, caseInsensitive));
					return;

				case 5:
				case 8:
					this.PushFC(new RegexFC(node._str, node._m == 0, caseInsensitive));
					return;

				case 9:
				case 10:
					this.PushFC(new RegexFC(node._ch, NodeType == 10, false, caseInsensitive));
					return;

				case 11:
					this.PushFC(new RegexFC(node._str, false, caseInsensitive));
					return;

				case 12:
					if (node._str.Length != 0)
					{
						if (!flag2)
						{
							this.PushFC(new RegexFC(node._str[0], false, false, caseInsensitive));
							return;
						}
						this.PushFC(new RegexFC(node._str[node._str.Length - 1], false, false, caseInsensitive));
						return;
					}
					this.PushFC(new RegexFC(true));
					return;

				case 13:
					this.PushFC(new RegexFC("\0\x0001\0\0", true, false));
					return;

				case 14:
				case 15:
				case 0x10:
				case 0x11:
				case 0x12:
				case 0x13:
				case 20:
				case 0x15:
				case 0x16:
				case 0x29:
				case 0x2a:
					this.PushFC(new RegexFC(true));
					return;

				case 0x17:
					this.PushFC(new RegexFC(true));
					return;

				case 0x58:
				case 0x59:
				case 90:
				case 0x5b:
				case 0x5c:
				case 0x5d:
				case 0x60:
				case 0x61:
				case 0x9c:
				case 0x9d:
				case 0x9e:
				case 0x9f:
				case 160:
					return;

				case 0x5e:
				case 0x5f:
					this.SkipChild();
					this.PushFC(new RegexFC(true));
					return;

				case 0x62:
					if (CurIndex == 0)
					{
						this.SkipChild();
					}
					return;

				case 0x98:
				case 0xa1:
					if (CurIndex != 0)
					{
						RegexFC fc = this.PopFC();
						RegexFC xfc6 = this.TopFC();
						this._failed = !xfc6.AddFC(fc, false);
					}
					return;

				case 0x99:
					if (CurIndex != 0)
					{
						RegexFC xfc = this.PopFC();
						RegexFC xfc2 = this.TopFC();
						this._failed = !xfc2.AddFC(xfc, true);
					}
					if (!this.TopFC()._nullable)
					{
						this._skipAllChildren = true;
					}
					return;

				case 0x9a:
				case 0x9b:
					if (node._m == 0)
					{
						this.TopFC()._nullable = true;
					}
					return;

				case 0xa2:
					if (CurIndex > 1)
					{
						RegexFC xfc3 = this.PopFC();
						RegexFC xfc4 = this.TopFC();
						this._failed = !xfc4.AddFC(xfc3, false);
					}
					return;
			}
			throw new ArgumentException(SR.GetString("UnexpectedOpcode", new object[] { NodeType.ToString(CultureInfo.CurrentCulture) }));
		}

		private bool FCIsEmpty()
		{
			return (this._fcDepth == 0);
		}

		internal static RegexPrefix FirstChars(RegexTree t)
		{
			RegexFC xfc = new RegexFCD().RegexFCFromRegexTree(t);
			if ((xfc == null) || xfc._nullable)
			{
				return null;
			}
			CultureInfo culture = ((t._options & RegexOptions.CultureInvariant) != RegexOptions.None) ? CultureInfo.InvariantCulture : CultureInfo.CurrentCulture;
			return new RegexPrefix(xfc.GetFirstChars(culture), xfc.IsCaseInsensitive());
		}

		private bool IntIsEmpty()
		{
			return (this._intDepth == 0);
		}

		private RegexFC PopFC()
		{
			return this._fcStack[--this._fcDepth];
		}

		private int PopInt()
		{
			return this._intStack[--this._intDepth];
		}

		internal static RegexPrefix Prefix(RegexTree tree)
		{
			RegexNode node2 = null;
			int num = 0;
			RegexNode node = tree._root;
		Label_000B:
			switch (node._type)
			{
				case 3:
				case 6:
					if (node._m <= 0)
					{
						return RegexPrefix.Empty;
					}
					return new RegexPrefix(string.Empty.PadRight(node._m, node._ch), RegexOptions.None != (node._options & RegexOptions.IgnoreCase));

				case 9:
					return new RegexPrefix(node._ch.ToString(CultureInfo.InvariantCulture), RegexOptions.None != (node._options & RegexOptions.IgnoreCase));

				case 12:
					return new RegexPrefix(node._str, RegexOptions.None != (node._options & RegexOptions.IgnoreCase));

				case 14:
				case 15:
				case 0x10:
				case 0x12:
				case 0x13:
				case 20:
				case 0x15:
				case 0x17:
				case 30:
				case 0x1f:
				case 0x29:
					break;

				case 0x19:
					if (node.ChildCount() > 0)
					{
						node2 = node;
						num = 0;
					}
					break;

				case 0x1c:
				case 0x20:
					node = node.Child(0);
					node2 = null;
					goto Label_000B;

				default:
					return RegexPrefix.Empty;
			}
			if ((node2 == null) || (num >= node2.ChildCount()))
			{
				return RegexPrefix.Empty;
			}
			node = node2.Child(num++);
			goto Label_000B;
		}

		private void PushFC(RegexFC fc)
		{
			if (this._fcDepth >= this._fcStack.Length)
			{
				RegexFC[] destinationArray = new RegexFC[this._fcDepth * 2];
				Array.Copy(this._fcStack, 0, destinationArray, 0, this._fcDepth);
				this._fcStack = destinationArray;
			}
			this._fcStack[this._fcDepth++] = fc;
		}

		private void PushInt(int I)
		{
			if (this._intDepth >= this._intStack.Length)
			{
				int[] destinationArray = new int[this._intDepth * 2];
				Array.Copy(this._intStack, 0, destinationArray, 0, this._intDepth);
				this._intStack = destinationArray;
			}
			this._intStack[this._intDepth++] = I;
		}

		private RegexFC RegexFCFromRegexTree(RegexTree tree)
		{
			RegexNode node = tree._root;
			int curIndex = 0;
		Label_0009:
			if (node._children == null)
			{
				this.CalculateFC(node._type, node, 0);
			}
			else if ((curIndex < node._children.Count) && !this._skipAllChildren)
			{
				this.CalculateFC(node._type | 0x40, node, curIndex);
				if (!this._skipchild)
				{
					node = node._children[curIndex];
					this.PushInt(curIndex);
					curIndex = 0;
				}
				else
				{
					curIndex++;
					this._skipchild = false;
				}
				goto Label_0009;
			}
			this._skipAllChildren = false;
			if (!this.IntIsEmpty())
			{
				curIndex = this.PopInt();
				node = node._next;
				this.CalculateFC(node._type | 0x80, node, curIndex);
				if (this._failed)
				{
					return null;
				}
				curIndex++;
				goto Label_0009;
			}
			if (this.FCIsEmpty())
			{
				return null;
			}
			return this.PopFC();
		}

		private void SkipChild()
		{
			this._skipchild = true;
		}

		private RegexFC TopFC()
		{
			return this._fcStack[this._fcDepth - 1];
		}
	}

	internal sealed class RegexFC
	{
		// Fields
		internal bool _caseInsensitive;
		internal RegexCharClass _cc;
		internal bool _nullable;

		// Methods
		internal RegexFC(bool nullable)
		{
			this._cc = new RegexCharClass();
			this._nullable = nullable;
		}

		internal RegexFC(string charClass, bool nullable, bool caseInsensitive)
		{
			this._cc = RegexCharClass.Parse(charClass);
			this._nullable = nullable;
			this._caseInsensitive = caseInsensitive;
		}

		internal RegexFC(char ch, bool not, bool nullable, bool caseInsensitive)
		{
			this._cc = new RegexCharClass();
			if (not)
			{
				if (ch > '\0')
				{
					this._cc.AddRange('\0', (char) (ch - '\x0001'));
				}
				if (ch < 0xffff)
				{
					this._cc.AddRange((char) (ch + '\x0001'), (char)0xffff);
				}
			}
			else
			{
				this._cc.AddRange(ch, ch);
			}
			this._caseInsensitive = caseInsensitive;
			this._nullable = nullable;
		}

		internal bool AddFC(RegexFC fc, bool concatenate)
		{
			if (!this._cc.CanMerge || !fc._cc.CanMerge)
			{
				return false;
			}
			if (concatenate)
			{
				if (!this._nullable)
				{
					return true;
				}
				if (!fc._nullable)
				{
					this._nullable = false;
				}
			}
			else if (fc._nullable)
			{
				this._nullable = true;
			}
			this._caseInsensitive |= fc._caseInsensitive;
			this._cc.AddCharClass(fc._cc);
			return true;
		}

		internal string GetFirstChars(CultureInfo culture)
		{
			if (this._caseInsensitive)
			{
				this._cc.AddLowercase(culture);
			}
			return this._cc.ToStringClass();
		}

		internal bool IsCaseInsensitive()
		{
			return this._caseInsensitive;
		}
	}

	internal class RegexLWCGCompiler : RegexCompiler
	{
		// Fields
		private static Type[] _paramTypes = new Type[] { typeof(RegexRunner) };
		private static int _regexCount = 0;

		// Methods
		internal RegexLWCGCompiler()
		{
		}

		internal DynamicMethod DefineDynamicMethod(string methname, Type returntype, Type hostType)
		{
			MethodAttributes attributes = MethodAttributes.Static | MethodAttributes.Public;
			CallingConventions standard = CallingConventions.Standard;
			DynamicMethod method = new DynamicMethod(methname, attributes, standard, returntype, _paramTypes, hostType, false);
			base._ilg = method.GetILGenerator();
			return method;
		}

		internal RegexRunnerFactory FactoryInstanceFromCode(RegexCode code, RegexOptions options)
		{
			base._code = code;
			base._codes = code._codes;
			base._strings = code._strings;
			base._fcPrefix = code._fcPrefix;
			base._bmPrefix = code._bmPrefix;
			base._anchors = code._anchors;
			base._trackcount = code._trackcount;
			base._options = options;
			string str = Interlocked.Increment(ref _regexCount).ToString(CultureInfo.InvariantCulture);
			DynamicMethod go = this.DefineDynamicMethod("Go" + str, null, typeof(CompiledRegexRunner));
			base.GenerateGo();
			DynamicMethod firstChar = this.DefineDynamicMethod("FindFirstChar" + str, typeof(bool), typeof(CompiledRegexRunner));
			base.GenerateFindFirstChar();
			DynamicMethod trackCount = this.DefineDynamicMethod("InitTrackCount" + str, null, typeof(CompiledRegexRunner));
			base.GenerateInitTrackCount();
			return new CompiledRegexRunnerFactory(go, firstChar, trackCount);
		}
	}

	internal class RegexTypeCompiler : RegexCompiler
	{
		// Fields
		private AssemblyBuilder _assembly;
		private MethodBuilder _methbuilder;
		private ModuleBuilder _module;
		private static LocalDataStoreSlot _moduleSlot = Thread.AllocateDataSlot();
		private TypeBuilder _typebuilder;
		private static int _typeCount = 0;

		// Methods
		internal RegexTypeCompiler(AssemblyName an, CustomAttributeBuilder[] attribs, string resourceFile)
		{
			new ReflectionPermission(PermissionState.Unrestricted).Assert();
			try
			{
				List<CustomAttributeBuilder> assemblyAttributes = new List<CustomAttributeBuilder>();
				CustomAttributeBuilder item = new CustomAttributeBuilder(typeof(SecurityTransparentAttribute).GetConstructor(Type.EmptyTypes), new object[0]);
				assemblyAttributes.Add(item);
				CustomAttributeBuilder builder2 = new CustomAttributeBuilder(typeof(SecurityRulesAttribute).GetConstructor(new Type[] { typeof(SecurityRuleSet) }), new object[] { SecurityRuleSet.Level2 });
				assemblyAttributes.Add(builder2);
				this._assembly = AppDomain.CurrentDomain.DefineDynamicAssembly(an, AssemblyBuilderAccess.RunAndSave, assemblyAttributes);
				this._module = this._assembly.DefineDynamicModule(an.Name + ".dll");
				if (attribs != null)
				{
					for (int i = 0; i < attribs.Length; i++)
					{
						this._assembly.SetCustomAttribute(attribs[i]);
					}
				}
				if (resourceFile != null)
				{
					this._assembly.DefineUnmanagedResource(resourceFile);
				}
			}
			finally
			{
				CodeAccessPermission.RevertAssert();
			}
		}

		internal void BakeMethod()
		{
			this._methbuilder = null;
		}

		internal Type BakeType()
		{
			Type type = this._typebuilder.CreateType();
			this._typebuilder = null;
			return type;
		}

		internal void DefineMethod(string methname, Type returntype)
		{
			MethodAttributes attributes = MethodAttributes.Virtual | MethodAttributes.Public;
			this._methbuilder = this._typebuilder.DefineMethod(methname, attributes, returntype, null);
			base._ilg = this._methbuilder.GetILGenerator();
		}

		internal void DefineType(string typename, bool ispublic, Type inheritfromclass)
		{
			if (ispublic)
			{
				this._typebuilder = this._module.DefineType(typename, TypeAttributes.Public, inheritfromclass);
			}
			else
			{
				this._typebuilder = this._module.DefineType(typename, TypeAttributes.AutoLayout, inheritfromclass);
			}
		}

		internal Type FactoryTypeFromCode(RegexCode code, RegexOptions options, string typeprefix)
		{
			base._code = code;
			base._codes = code._codes;
			base._strings = code._strings;
			base._fcPrefix = code._fcPrefix;
			base._bmPrefix = code._bmPrefix;
			base._anchors = code._anchors;
			base._trackcount = code._trackcount;
			base._options = options;
			string str3 = Interlocked.Increment(ref _typeCount).ToString(CultureInfo.InvariantCulture);
			string typename = typeprefix + "Runner" + str3;
			string str2 = typeprefix + "Factory" + str3;
			this.DefineType(typename, false, typeof(RegexRunner));
			this.DefineMethod("Go", null);
			base.GenerateGo();
			this.BakeMethod();
			this.DefineMethod("FindFirstChar", typeof(bool));
			base.GenerateFindFirstChar();
			this.BakeMethod();
			this.DefineMethod("InitTrackCount", null);
			base.GenerateInitTrackCount();
			this.BakeMethod();
			Type newtype = this.BakeType();
			this.DefineType(str2, false, typeof(RegexRunnerFactory));
			this.DefineMethod("CreateInstance", typeof(RegexRunner));
			this.GenerateCreateInstance(newtype);
			this.BakeMethod();
			return this.BakeType();
		}

		internal void GenerateCreateHashtable(FieldInfo field, Hashtable ht)
		{
			MethodInfo method = typeof(Hashtable).GetMethod("Add", BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Static | BindingFlags.Instance);
			base.Ldthis();
			base.Newobj(typeof(Hashtable).GetConstructor(new Type[0]));
			base.Stfld(field);
			IDictionaryEnumerator enumerator = ht.GetEnumerator();
			while (enumerator.MoveNext())
			{
				base.Ldthisfld(field);
				if (enumerator.Key is int)
				{
					base.Ldc((int) enumerator.Key);
					base._ilg.Emit(OpCodes.Box, typeof(int));
				}
				else
				{
					base.Ldstr((string) enumerator.Key);
				}
				base.Ldc((int) enumerator.Value);
				base._ilg.Emit(OpCodes.Box, typeof(int));
				base.Callvirt(method);
			}
		}

		internal void GenerateCreateInstance(Type newtype)
		{
			base.Newobj(newtype.GetConstructor(new Type[0]));
			base.Ret();
		}

		internal void GenerateRegexType(string pattern, RegexOptions opts, string name, bool ispublic, RegexCode code, RegexTree tree, Type factory)
		{
			FieldInfo ft = this.RegexField("pattern");
			FieldInfo info2 = this.RegexField("roptions");
			FieldInfo info3 = this.RegexField("factory");
			FieldInfo field = this.RegexField("caps");
			FieldInfo info5 = this.RegexField("capnames");
			FieldInfo info6 = this.RegexField("capslist");
			FieldInfo info7 = this.RegexField("capsize");
			Type[] parameterTypes = new Type[0];
			this.DefineType(name, ispublic, typeof(Regex));
			this._methbuilder = null;
			MethodAttributes @public = MethodAttributes.Public;
			base._ilg = this._typebuilder.DefineConstructor(@public, CallingConventions.Standard, parameterTypes).GetILGenerator();
			base.Ldthis();
			base._ilg.Emit(OpCodes.Call, typeof(Regex).GetConstructor(BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance, null, new Type[0], new ParameterModifier[0]));
			base.Ldthis();
			base.Ldstr(pattern);
			base.Stfld(ft);
			base.Ldthis();
			base.Ldc((int) opts);
			base.Stfld(info2);
			base.Ldthis();
			base.Newobj(factory.GetConstructor(parameterTypes));
			base.Stfld(info3);
			if (code._caps != null)
			{
				this.GenerateCreateHashtable(field, code._caps);
			}
			if (tree._capnames != null)
			{
				this.GenerateCreateHashtable(info5, tree._capnames);
			}
			if (tree._capslist != null)
			{
				base.Ldthis();
				base.Ldc(tree._capslist.Length);
				base._ilg.Emit(OpCodes.Newarr, typeof(string));
				base.Stfld(info6);
				for (int i = 0; i < tree._capslist.Length; i++)
				{
					base.Ldthisfld(info6);
					base.Ldc(i);
					base.Ldstr(tree._capslist[i]);
					base._ilg.Emit(OpCodes.Stelem_Ref);
				}
			}
			base.Ldthis();
			base.Ldc(code._capsize);
			base.Stfld(info7);
			base.Ldthis();
			base.Call(typeof(Regex).GetMethod("InitializeReferences", BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Static | BindingFlags.Instance));
			base.Ret();
			this._typebuilder.CreateType();
			base._ilg = null;
			this._typebuilder = null;
		}

		private FieldInfo RegexField(string fieldname)
		{
			return typeof(Regex).GetField(fieldname, BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Static | BindingFlags.Instance);
		}

		internal void Save()
		{
			this._assembly.Save(this._assembly.GetName().Name + ".dll");
		}
	}


	internal class MatchSparse : Match
	{
		// Fields
		internal Hashtable _caps;

		// Methods
		internal MatchSparse(Regex regex, Hashtable caps, int capcount, string text, int begpos, int len, int startpos) : base(regex, capcount, text, begpos, len, startpos)
		{
			this._caps = caps;
		}

		// Properties
		public override GroupCollection Groups
		{
			get
			{
				if (base._groupcoll == null)
				{
					base._groupcoll = new GroupCollection(this, this._caps);
				}
				return base._groupcoll;
			}
		}
	}

	internal class GroupEnumerator : IEnumerator
	{
		// Fields
		internal int _curindex = -1;
		internal GroupCollection _rgc;

		// Methods
		internal GroupEnumerator(GroupCollection rgc)
		{
			this._rgc = rgc;
		}

		public bool MoveNext()
		{
			int count = this._rgc.Count;
			if (this._curindex >= count)
			{
				return false;
			}
			this._curindex++;
			return (this._curindex < count);
		}

		public void Reset()
		{
			this._curindex = -1;
		}

		// Properties
		public Capture Capture
		{
			get
			{
				if ((this._curindex < 0) || (this._curindex >= this._rgc.Count))
				{
					throw new InvalidOperationException(SR.GetString("EnumNotStarted"));
				}
				return this._rgc[this._curindex];
			}
		}

		public object Current
		{
			get
			{
				return this.Capture;
			}
		}
	}

	[Serializable]
	internal class CaptureEnumerator : IEnumerator
	{
		// Fields
		internal int _curindex = -1;
		internal CaptureCollection _rcc;

		// Methods
		internal CaptureEnumerator(CaptureCollection rcc)
		{
			this._rcc = rcc;
		}

		public bool MoveNext()
		{
			int count = this._rcc.Count;
			if (this._curindex >= count)
			{
				return false;
			}
			this._curindex++;
			return (this._curindex < count);
		}

		public void Reset()
		{
			this._curindex = -1;
		}

		// Properties
		public Capture Capture
		{
			get
			{
				if ((this._curindex < 0) || (this._curindex >= this._rcc.Count))
				{
					throw new InvalidOperationException(SR.GetString("EnumNotStarted"));
				}
				return this._rcc[this._curindex];
			}
		}

		public object Current
		{
			get
			{
				return this.Capture;
			}
		}
	}

	static class SR
	{
		public static string GetString(string str, params object[] p) { return str; }
	};

	internal sealed class CompiledRegexRunner : RegexRunner
	{
		// Fields
		private FindFirstCharDelegate findFirstCharMethod;
		private NoParamDelegate goMethod;
		private NoParamDelegate initTrackCountMethod;

		// Methods
		internal CompiledRegexRunner()
		{
		}

		protected override bool FindFirstChar()
		{
			return this.findFirstCharMethod(this);
		}

		protected override void Go()
		{
			this.goMethod(this);
		}

		protected override void InitTrackCount()
		{
			this.initTrackCountMethod(this);
		}

		internal void SetDelegates(NoParamDelegate go, FindFirstCharDelegate firstChar, NoParamDelegate trackCount)
		{
			this.goMethod = go;
			this.findFirstCharMethod = firstChar;
			this.initTrackCountMethod = trackCount;
		}
	}

	internal sealed class CompiledRegexRunnerFactory : RegexRunnerFactory
	{
		// Fields
		private DynamicMethod findFirstCharMethod;
		private DynamicMethod goMethod;
		private DynamicMethod initTrackCountMethod;

		// Methods
		internal CompiledRegexRunnerFactory(DynamicMethod go, DynamicMethod firstChar, DynamicMethod trackCount)
		{
			this.goMethod = go;
			this.findFirstCharMethod = firstChar;
			this.initTrackCountMethod = trackCount;
		}

		protected internal override RegexRunner CreateInstance()
		{
			CompiledRegexRunner runner = new CompiledRegexRunner();
			new ReflectionPermission(PermissionState.Unrestricted).Assert();
			runner.SetDelegates((NoParamDelegate) this.goMethod.CreateDelegate(typeof(NoParamDelegate)), (FindFirstCharDelegate) this.findFirstCharMethod.CreateDelegate(typeof(FindFirstCharDelegate)), (NoParamDelegate) this.initTrackCountMethod.CreateDelegate(typeof(NoParamDelegate)));
			return runner;
		}
	}


	internal delegate bool FindFirstCharDelegate(RegexRunner r);
	internal delegate void NoParamDelegate(RegexRunner r);


}
