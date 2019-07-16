
namespace LogJoint.Tests.Integration
{
	// Base class for classes that you don't want NUnit to touch while tests discovery.
	// Make your class a generic no-TestFixture class and inherit from this one.
	// Inheritance is not required, but it's good make the hack visible.
	// The NUnit undiscoverability hack is required to prevent NUnit from failing
	// on classes that use dynamically loaded types, for examples, types loaded from plugins.
	public class UndiscoverableByNUnit<T>
	{
	};
}
