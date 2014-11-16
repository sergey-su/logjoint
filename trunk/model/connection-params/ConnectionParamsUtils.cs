
namespace LogJoint
{
	public static class ConnectionParamsUtils
	{
		/// <summary>
		/// A mandatory IConnectionParams key.
		/// Specifies the identity of the log source. IConnectionParams having equal identities 
		/// are considered to be referencing to the same log source.
		/// </summary>
		public static readonly string IdentityConnectionParam = "id";
		/// <summary>
		/// An IConnectionParams key.
		/// Specifies a path to the file that will be open to read log content. It could be a path 
		/// to actual log file in the local file system or on a network drive. Or it could be 
		/// a temporary file written by logjoint.
		/// If connection parameters doesn't contain PathConnectionParam usually
		/// preprocessing steps are specified. <see cref="PreprocessingStepParamPrefix"/>.
		/// </summary>
		public static readonly string PathConnectionParam = "path";
		/// <summary>
		/// A prefix of a group of IConnectionParams keys.
		/// Each key in the goup specifies a preprocessing step that must be taken to obtian actual log. 
		/// Complete keys are composed this way: [prefix][zero-based-index]. 
		/// Example: prep-step0 = 'get http://example.com/log.zip'; prep-step1 = download; prep-step2 = 'unzip f/mylog.txt'
		/// </summary>
		public static readonly string PreprocessingStepParamPrefix = "prep-step";
		/// <summary>
		/// An optional IConnectionParams key.
		/// Specifies user-friendly string representing the log source.
		/// </summary>
		public static readonly string DisplayNameConnectionParam = "display-as";
		/// <summary>
		/// An IConnectionParams key.
		/// Specifies a path to folder that will be monitored for parts of rotated log.
		/// </summary>
		public static readonly string RotatedLogFolderPathConnectionParam = "rotated-log-folder-path";

		public static string GetFileOrFolderBasedUserFriendlyConnectionName(IConnectionParams cp)
		{
			string displayName = cp[DisplayNameConnectionParam];
			if (!string.IsNullOrEmpty(displayName))
				return displayName;
			string id = cp[IdentityConnectionParam];
			if (!string.IsNullOrEmpty(id))
				return id;
			string rotatedLogFolder = cp[RotatedLogFolderPathConnectionParam];
			if (!string.IsNullOrEmpty(rotatedLogFolder))
				return rotatedLogFolder;
			return cp[PathConnectionParam] ?? "";
		}
		public static string CreateFileBasedConnectionIdentityFromFileName(string fileName)
		{
			return IOUtils.NormalizePath(fileName);
		}
		public static string CreateFolderBasedConnectionIdentityFromFolderPath(string folder)
		{
			return IOUtils.NormalizePath(folder);
		}
		public static IConnectionParams CreateFileBasedConnectionParamsFromFileName(string fileName)
		{
			ConnectionParams p = new ConnectionParams();
			p[ConnectionParamsUtils.PathConnectionParam] = fileName;
			p[ConnectionParamsUtils.IdentityConnectionParam] = CreateFileBasedConnectionIdentityFromFileName(fileName);
			return p;
		}
		public static IConnectionParams CreateRotatedLogConnectionParamsFromFolderPath(string folder)
		{
			ConnectionParams p = new ConnectionParams();
			p[ConnectionParamsUtils.RotatedLogFolderPathConnectionParam] = folder;
			p[ConnectionParamsUtils.IdentityConnectionParam] = CreateFolderBasedConnectionIdentityFromFolderPath(folder);
			return p;
		}
		public static string GetConnectionIdentity(IConnectionParams cp)
		{
			var ret = cp[IdentityConnectionParam];
			if (string.IsNullOrWhiteSpace(ret))
				return null;
			return ret;
		}
		public static bool ConnectionsHaveEqualIdentities(IConnectionParams cp1, IConnectionParams cp2)
		{
			var id1 = GetConnectionIdentity(cp1);
			var id2 = GetConnectionIdentity(cp2);
			if (id1 == null || id2 == null)
				return false;
			return id1 == id2;
		}

		public static void ValidateConnectionParams(IConnectionParams cp, ILogProviderFactory againstFactory)
		{
			if (GetConnectionIdentity(cp) == null)
				throw new InvalidConnectionParamsException("no connection identity in connectino params");
		}

		public static IConnectionParams RemovePathParamIfItRefersToTemporaryFile(IConnectionParams cp, ITempFilesManager mgr)
		{
			string fileName = cp[PathConnectionParam];
			if (!string.IsNullOrEmpty(fileName))
				if (mgr.IsTemporaryFile(fileName))
					cp[PathConnectionParam] = null;
			return cp;
		}
		public static ConnectionParams CreateConnectionParamsWithIdentity(string identity)
		{
			var ret = new ConnectionParams();
			ret[IdentityConnectionParam] = identity;
			return ret;
		}

		public static string GuessFileNameFromConnectionIdentity(string identity)
		{
			string guessedFileName;
			int idx = identity.LastIndexOfAny(new char[] { '\\', '/' });
			if (idx == -1)
				guessedFileName = identity;
			else
				guessedFileName = identity.Substring(idx + 1, identity.Length - idx - 1);
			return guessedFileName;
		}
	};

}
