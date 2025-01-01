using System.Collections.Generic;

namespace LogJoint.Postprocessing
{
    public static class TaggingExtensions
    {
        public static T SetTags<T>(this T tagged, HashSet<string> tags) where T : ITagged
        {
            tagged.Tags = tags;
            return tagged;
        }

        public static T SetTagsIfNotSet<T>(this T tagged, HashSet<string> tags) where T : ITagged
        {
            if (tagged.Tags == null || tagged.Tags.Count == 0)
                tagged.Tags = tags;
            return tagged;
        }
    };
}
