using System.Collections.Generic;

namespace LogJoint.Postprocessing
{
    public interface ITagged
    {
        HashSet<string>? Tags { get; set; }
    };
}
