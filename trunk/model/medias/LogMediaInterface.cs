using System;
using System.IO;
using System.Threading.Tasks;

namespace LogJoint
{
	/// <summary>
	/// Represents the media that the log may be read from.
	/// Logs may be stored in a single file, in multiple files (rotated log),
	/// in a database, or any other media. The goal of this interface
	/// is to abstract from the details of concrete media.
	/// </summary>
	public interface ILogMedia : IDisposable
	{
		/// <summary>
		/// Call this method to update the properties of ILogMedia object.
		/// </summary>
		/// <remarks>
		/// This method usually makes a request to the actual media to read
		/// the up-to-date data. This means that the values of the properties 
		/// (IsAvailable, LastModified, Size) stay the same as long as 
		/// Update is not called. Note that this method doesn't affect 
		/// the stream referenced by DataStream property. The properties of 
		/// the stream may change unpredictably regardless any calls to Update().
		/// </remarks>
		Task Update();
		/// <summary>
		/// Returns false when the media becomes unavailable. This may happen
		/// when, for example, the log file gets deleted. Property value 
		/// gets updated only after call to Update().
		/// </summary>
		bool IsAvailable { get; }
		/// <summary>
		/// Reference to Stream object that represents media's content.
		/// This property always returns the same reference. Note: call to Update() 
		/// doesn't affect the reference. Stream's content may change
		/// regardless calls to Update(). 
		/// Implementation advice: use DelegatingStream class to guarantee 
		/// reference equality when you need to recreate stream.
		/// </summary>
		Stream DataStream { get; }
		/// <summary>
		/// Returns the date/time when the media changed last time.
		/// Property's value gets updated only after call to Update().
		/// </summary>
		DateTime LastModified { get; }
		/// <summary>
		/// Returns the size of the media in bytes. Property's value 
		/// gets updated only after call to Update(). That differenciates 
		/// this property from DataStream.Length. DataStream.Length
		/// may change unpredictably.
		/// </summary>
		long Size { get; }
	};
}
