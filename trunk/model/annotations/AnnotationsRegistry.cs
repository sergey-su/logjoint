using System.Collections.Generic;
using System.Collections.Immutable;

namespace LogJoint
{
    internal class AnnotationsRegistry : IAnnotationsRegistry
    {
        readonly IChangeNotification changeNotification;
        TrieNode annotations = new TrieNode();

        public AnnotationsRegistry(IChangeNotification changeNotification)
        {
            this.changeNotification = changeNotification;
        }

        IAnnotationsSnapshot IAnnotationsRegistry.Annotations => annotations;

        void IAnnotationsRegistry.Add(string key, string value, ILogSource associatedLogSource)
        {
            annotations = annotations.Set(key, value);
            changeNotification.Post();
        }

        class TrieNode : IAnnotationsSnapshot
        {
            ImmutableDictionary<char, TrieNode> children = ImmutableDictionary.Create<char, TrieNode>();
            // Non-null only when this trie node is a leaf
            string leafValue = null;

            public TrieNode Clone()
            {
                return new TrieNode() { children = children, leafValue = leafValue };
            }

            bool IAnnotationsSnapshot.IsEmpty => children.IsEmpty;

            IEnumerable<StringAnnotationEntry> IAnnotationsSnapshot.FindAnnotations(string input)
            {
                TrieNode current = this;
                int? matchBegin = null;
                for (int i = 0; i < input.Length; ++i)
                {
                    char c = input[i];
                    TrieNode n = current.children.GetValueOrDefault(c);
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
                                Annotation = n.leafValue
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

            public TrieNode Set(string key, string value)
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
        }
    }
}