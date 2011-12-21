using System;
using System.Collections.Generic;
using System.Collections;
using System.Linq;
using System.IO;

namespace LogJoint
{
	public static class IOUtils
	{
		/// <summary>
		/// Does basic path normalization:
		///    ensures path is absolute,
		///    makes path lowercase
		/// </summary>
		public static string NormalizePath(string path)
		{
			return Path.GetFullPath(path).ToLower();
		}
	}
}
