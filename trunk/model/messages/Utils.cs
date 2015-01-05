
namespace LogJoint
{
	public static class MessagesUtils
	{
		public static int RehashMessageWithNewTimestamp(int originalMessageHash, MessageTimestamp originalMessageTime, MessageTimestamp newMessageTimestamp)
		{
			var originalMessageHashWoTime = XORTimestampHash(originalMessageHash, originalMessageTime);
			return XORTimestampHash(originalMessageHashWoTime, newMessageTimestamp);
		}

		internal static int XORTimestampHash(int hash, MessageTimestamp timestamp)
		{
			return hash ^ timestamp.GetStableHashCode();
		}
	};
}
