
namespace LogJoint
{
	public static class ConnectionParamsUtils
	{
		public static string GetFileOrFolderBasedUserFriendlyConnectionName(IConnectionParams cp)
		{
			string displayName = cp[ConnectionParamsKeys.DisplayNameConnectionParam];
			if (!string.IsNullOrEmpty(displayName))
				return displayName;
			string id = cp[ConnectionParamsKeys.IdentityConnectionParam];
			if (!string.IsNullOrEmpty(id))
				return id;
			string rotatedLogFolder = cp[ConnectionParamsKeys.RotatedLogFolderPathConnectionParam];
			if (!string.IsNullOrEmpty(rotatedLogFolder))
				return rotatedLogFolder;
			return cp[ConnectionParamsKeys.PathConnectionParam] ?? "";
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
			p[ConnectionParamsKeys.PathConnectionParam] = fileName;
			p[ConnectionParamsKeys.IdentityConnectionParam] = CreateFileBasedConnectionIdentityFromFileName(fileName);
			return p;
		}
		public static IConnectionParams CreateRotatedLogConnectionParamsFromFolderPath(string folder)
		{
			ConnectionParams p = new ConnectionParams();
			p[ConnectionParamsKeys.RotatedLogFolderPathConnectionParam] = folder;
			p[ConnectionParamsKeys.IdentityConnectionParam] = CreateFolderBasedConnectionIdentityFromFolderPath(folder);
			return p;
		}
		public static string GetConnectionIdentity(IConnectionParams cp)
		{
			var ret = cp[ConnectionParamsKeys.IdentityConnectionParam];
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
				throw new InvalidConnectionParamsException("no connection identity in connection params");
		}

		public static IConnectionParams RemovePathParamIfItRefersToTemporaryFile(IConnectionParams cp, ITempFilesManager mgr)
		{
			string fileName = cp[ConnectionParamsKeys.PathConnectionParam];
			if (!string.IsNullOrEmpty(fileName))
				if (mgr.IsTemporaryFile(fileName))
					cp[ConnectionParamsKeys.PathConnectionParam] = null;
			return cp;
		}

		public static IConnectionParams RemoveNonPersistentParams(IConnectionParams cp, ITempFilesManager tempFilesManager)
		{
			RemovePathParamIfItRefersToTemporaryFile(cp, tempFilesManager);
			RemoveInitialTimeOffset(cp);
			return cp;
		}

		public static IConnectionParams RemoveInitialTimeOffset(IConnectionParams cp)
		{
			if (!string.IsNullOrEmpty(cp[ConnectionParamsKeys.TimeOffsetConnectionParam]))
				cp[ConnectionParamsKeys.TimeOffsetConnectionParam] = null;
			return cp;
		}

		public static ConnectionParams CreateConnectionParamsWithIdentity(string identity)
		{
			var ret = new ConnectionParams();
			ret[ConnectionParamsKeys.IdentityConnectionParam] = identity;
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
