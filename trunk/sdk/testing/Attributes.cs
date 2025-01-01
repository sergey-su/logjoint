using System;

namespace LogJoint.Tests.Integration
{
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true, Inherited = true)]
    public class IntegrationTestFixtureAttribute : Attribute
    {
        public IntegrationTestFixtureAttribute() { }
        /// <summary>
        /// User-friendly message that describes the test
        /// </summary>
        public string Description { get; set; }
        /// <summary>
        /// The reason why test fixture is ignored.
        /// </summary>
        public string Ignore { get; set; }
    };

    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false, Inherited = true)]
    public class BeforeEachAttribute : Attribute
    {
        public BeforeEachAttribute() { }
    }

    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false, Inherited = true)]
    public class AfterEachAttribute : Attribute
    {
        public AfterEachAttribute() { }
    }

    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false, Inherited = true)]
    public class BeforeAllAttribute : Attribute
    {
        public BeforeAllAttribute() { }
    }

    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false, Inherited = true)]
    public class AfterAllAttribute : Attribute
    {
        public AfterAllAttribute() { }
    }

    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false, Inherited = true)]
    public class IntegrationTestAttribute : Attribute
    {
        public IntegrationTestAttribute() { }
        /// <summary>
        /// User-friendly message that describes the test
        /// </summary>
        public string Description { get; set; }
        /// <summary>
        /// The reason why test fixture is ignored.
        /// </summary>
        public string Ignore { get; set; }
    }
}
