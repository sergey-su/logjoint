using System;
using System.Linq;
using System.Text;
using NUnit.Framework;
using System.Collections.Generic;
using Newtonsoft.Json.Linq;

namespace LogJoint.UI.Presenters.Reactive.Tests
{
    [TestFixture]
    public class TreeTests
    {
        void DoTest(string tree1, string tree2, string expected)
        {
            var edits = TreeEdit.GetTreeEdits(new TestNode(tree1), new TestNode(tree2));
            var actual = string.Join("; ", edits);
            Assert.That(expected, Is.EqualTo(actual));
        }

        [Test]
        public void BuildTreeFromNothing()
        {
            DoTest(
                "{k: 'root'}",
                "{k: 'root', c: [ {k: 1, s: true}, {k: 2} ]}",
                "(root).Insert ()->(1 s) at 0; (1 s).Select; (root).Insert ()->(2) at 1"
            );
        }

        [Test]
        public void Expand()
        {
            DoTest(
                "{k: 'root', c: [ {k: 1, c: [ {k: 11}, {k: 12} ] } ]}",
                "{k: 'root', c: [ {k: 1, e: true, c: [ {k: 11}, {k: 12} ] } ]}",
                "(root).Reuse (1)->(1 e) at 0; (1 e).Reuse (11)->(11) at 0; (1 e).Reuse (12)->(12) at 1; (1 e).Expand");
        }

        [Test]
        public void RepurposeWithFullCleanup()
        {
            DoTest(
                "{k: 'root', c: [ {k: 1, c: [ {k: 11}, {k: 12} ] } ]}",
                "{k: 'root', c: [ {k: 2} ]}",
                "(root).Reuse (1)->(2) at 0; (2).Delete (11)->() at 0; (2).Delete (12)->() at 0");
        }

        class TestNode : ITreeNode
        {
            public TestNode(string str) : this(JObject.Parse(str))
            {
            }

            public TestNode(JObject @object)
            {
                Key = @object.Value<string>("k");
                IsExpanded = @object.TryGetValue("e", out var e) && (bool)e;
                IsSelected = @object.TryGetValue("s", out var s) && (bool)s;
                if (@object.TryGetValue("c", out var c))
                    Children = new List<TestNode>(c.Children().OfType<JObject>().Select(i => new TestNode(i)));
                else
                    Children = new List<TestNode>();
            }

            public override string ToString()
            {
                var flags = $"{(IsExpanded ? "e" : "")}{(IsSelected ? "s" : "")}";
                return flags == "" ? $"{Key}" : $"{Key} {flags}";
            }

            public string Key { get; private set; }
            public IReadOnlyList<ITreeNode> Children { get; private set; }
            public bool IsExpanded { get; private set; }
            public bool IsSelected { get; private set; }
            public bool IsExpandable { get; private set; }
        };
    }
}