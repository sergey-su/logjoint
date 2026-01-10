using System.Collections.Generic;
using System.Collections.Immutable;
using System.Threading.Tasks;
using System.Xml.Linq;
using System.Linq;

namespace LogJoint
{
    internal class AnnotationsRegistry : IAnnotationsRegistry
    {
        readonly IChangeNotification changeNotification;
        TrieNode annotations = new();
        readonly static TrieNode emptyAnnotations = new();
        readonly TaskChain saveChain = new();
        readonly LJTraceSource tracer;

        public AnnotationsRegistry(IChangeNotification changeNotification, ITraceSourceFactory sourceFactory)
        {
            this.changeNotification = changeNotification;
            this.tracer = sourceFactory.CreateTraceSource("annotations");
        }

        IAnnotationsSnapshot IAnnotationsRegistry.Annotations => annotations;

        IAnnotationsSnapshot IAnnotationsRegistry.EmptyAnnotations => emptyAnnotations;

        void IAnnotationsRegistry.Add(string key, string value, ILogSource associatedLogSource)
        {
            annotations = annotations.Set(key, new LeafValue(key, value, associatedLogSource));
            changeNotification.Post();
            if (associatedLogSource != null && !associatedLogSource.IsDisposed)
                saveChain.AddTask(() => SaveAnnotations(associatedLogSource));
        }

        bool IAnnotationsRegistry.Change(string key, string value)
        {
            LeafValue leafValue = annotations.Find(key);
            if (leafValue == null || leafValue.Annotation == value)
                return false;
            annotations = annotations.Erase(key);
            annotations = annotations.Set(key, new LeafValue(key, value, leafValue.AssociatedLogSource));
            changeNotification.Post();
            if (leafValue.AssociatedLogSource != null && !leafValue.AssociatedLogSource.IsDisposed)
                saveChain.AddTask(() => SaveAnnotations(leafValue.AssociatedLogSource));
            return true;
        }

        bool IAnnotationsRegistry.Delete(string key)
        {
            if (annotations.Find(key) == null)
                return false;
            annotations = annotations.Erase(key);
            return true;
        }

        async Task IAnnotationsRegistry.LoadAnnotations(ILogSource forLogSource)
        {
            await using var section = await forLogSource.LogSourceSpecificStorageEntry.OpenXMLSection(
                "annotations", Persistence.StorageSectionOpenFlag.ReadOnly);
            var root = section.Data.Element("annotations");
            if (root == null)
                return;
            foreach (var elt in root.Elements("annotation"))
            {
                var key = elt.Attribute("key");
                var value = elt.Attribute("value");
                if (key != null && value != null)
                {
                    annotations = annotations.Set(key.Value,
                        new LeafValue(key.Value, value.Value, forLogSource));
                }
            }
        }

        async Task SaveAnnotations(ILogSource logSource)
        {
            try
            {
                await using var section = await logSource.LogSourceSpecificStorageEntry.OpenXMLSection(
                    "annotations", Persistence.StorageSectionOpenFlag.ReadWrite | Persistence.StorageSectionOpenFlag.ClearOnOpen);
                section.Data.Add(
                    new XElement("annotations",
                    [.. annotations.EnumAnnotations(logSource, key: "").Select(
                        a => new XElement("annotation", new XAttribute[] { new("key", a.Key), new("value", a.Value) })
                    )]
                ));
            }
            catch (Persistence.StorageException storageException)
            {
                tracer.Error(storageException, "Failed to store bookmarks for log {0}",
                    logSource.GetSafeConnectionId());
            }
        }

        record class LeafValue(string Key, string Annotation, ILogSource AssociatedLogSource);

        class TrieNode : IAnnotationsSnapshot
        {
            ImmutableDictionary<char, TrieNode> children = ImmutableDictionary.Create<char, TrieNode>();
            // Non-null only when this trie node is a leaf
            LeafValue leafValue = null;

            public TrieNode Clone()
            {
                return new TrieNode() { children = children, leafValue = leafValue };
            }

            bool IAnnotationsSnapshot.IsEmpty => children.IsEmpty;

            string? IAnnotationsSnapshot.Find(string key) => Find(key)?.Annotation;

            IEnumerable<StringAnnotationEntry> IAnnotationsSnapshot.FindAnnotations(string input)
            {
                TrieNode current = this;
                int? matchBegin = null;
                for (int i = 0; i < input.Length; ++i)
                {
                    char c = input[i];
                    TrieNode? n = current.children.GetValueOrDefault(c);
                    if (n != null)
                    {
                        if (matchBegin == null)
                        {
                            matchBegin = i;
                            current = n;
                        }
                        else if (n.leafValue != null)
                        {
                            yield return new StringAnnotationEntry()
                            {
                                BeginIndex = matchBegin.Value,
                                EndIndex = i + 1,
                                Key = n.leafValue.Key,
                                Annotation = n.leafValue.Annotation,
                            };
                            current = this;
                            matchBegin = null;
                        }
                        else
                        {
                            current = n;
                        }
                    }
                    else
                    {
                        current = this;
                        matchBegin = null;
                    }
                }
            }

            public LeafValue? Find(string key)
            {
                TrieNode current = this;
                foreach (char c in key)
                {
                    TrieNode? n = current.children.GetValueOrDefault(c);
                    if (n == null)
                        return null;
                    current = n;
                }
                return current?.leafValue;
            }

            public TrieNode Erase(string key) => Erase(key, 0);

            public TrieNode Set(string key, LeafValue value)
            {
                TrieNode result = this.Clone();
                TrieNode current = result;
                foreach (char c in key)
                {
                    TrieNode n = current.children.GetValueOrDefault(c);
                    current.children = current.children.SetItem(c, n = n != null ? n.Clone() : new TrieNode());
                    current = n;
                }
                current.leafValue = value;
                return result;
            }

            public IEnumerable<KeyValuePair<string, string>> EnumAnnotations(ILogSource forLogSource, string key)
            {
                if (leafValue != null && leafValue.AssociatedLogSource == forLogSource)
                    yield return new (key, leafValue.Annotation);
                foreach (var (childChar, child) in children)
                    foreach (var childAnnotation in child.EnumAnnotations(forLogSource, $"{key}{childChar}"))
                        yield return childAnnotation;
            }

            private TrieNode Erase(string key, int keyIndex)
            {
                TrieNode result = this.Clone();
                if (keyIndex == key.Length)
                {
                    result.leafValue = null;
                }
                else
                {
                    char c = key[keyIndex];
                    TrieNode child = result.children[c].Erase(key, keyIndex + 1);
                    result.children = child.IsEmpty ?
                        result.children.Remove(c) : result.children.SetItem(c, child);
                }
                return result;
            }

            private bool IsEmpty => children.IsEmpty && leafValue == null;
        }
    }
}