using System;

namespace LogJoint
{
    public interface IPluginFormatsManager
    {
        void RegisterPluginFormats(Extensibility.IPluginManifest manifest);
    };

    public interface IUserDefinedFormatsManagerInternal : IUserDefinedFormatsManager
    {
        void RegisterFormatConfigType(string configNodeName, Func<UserDefinedFactoryParams, IUserDefinedFactory> factory);
        IUserDefinedFactory CreateFactory(string configNodeName, UserDefinedFactoryParams @params);
    };
}
