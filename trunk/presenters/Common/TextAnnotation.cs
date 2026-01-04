using LogJoint.Drawing;
using System.Collections.Generic;
using System.Collections.Immutable;

namespace LogJoint.UI.Presenters
{
    public struct AnnotatedTextFragment
    {
        public StringSlice Value;
        public bool IsAnnotationFragment;
        public Color? HighlightColor;
    };

    public class AnnotatedTextFragmentsBuilder
    {
        private List<AnnotatedTextFragment> fragments = new();

        public IReadOnlyList<AnnotatedTextFragment> Build()
        {
            return fragments;
        }

        public AnnotatedTextFragmentsBuilder AddAnnotatedTextFragments(
            StringSlice text, IAnnotationsSnapshot annotationsSnapshot,
            IReadOnlyList<IFilter> highligingFilters, ImmutableArray<Color> highlightColors)
        {
            using var annotations = annotationsSnapshot.FindAnnotations(text).GetEnumerator();
            using var highlights = highligingFilters.GetHighlightRanges(text).GetEnumerator();

            int lastTextIndex = 0;
            void AddTextFragment(int tillIndex, FilterAction? highlightAction)
            {
                if (tillIndex > lastTextIndex)
                {
                    fragments.Add(new AnnotatedTextFragment()
                    {
                        Value = text.SubString(lastTextIndex, tillIndex - lastTextIndex),
                        HighlightColor = highlightAction?.ToColor(highlightColors),
                    });
                    lastTextIndex = tillIndex;
                }
            }
            ;
            void AddAnnotationFragment(string value)
            {
                fragments.Add(new AnnotatedTextFragment()
                {
                    Value = new StringSlice(value),
                    IsAnnotationFragment = true
                });
            }
            ;

            bool annotationExists = annotations.MoveNext();
            bool highlightExists = highlights.MoveNext();
            for (; ; )
            {
                if (annotationExists && highlightExists)
                {
                    if (annotations.Current.BeginIndex <= highlights.Current.beginIdx)
                    {
                        AddTextFragment(annotations.Current.BeginIndex, null);
                        AddAnnotationFragment(annotations.Current.Annotation);
                        annotationExists = annotations.MoveNext();
                    }
                    else if (annotations.Current.BeginIndex <= highlights.Current.endIdx)
                    {
                        AddTextFragment(highlights.Current.beginIdx, null);
                        AddTextFragment(annotations.Current.BeginIndex, highlights.Current.action);
                        AddAnnotationFragment(annotations.Current.Annotation);
                        AddTextFragment(highlights.Current.endIdx, highlights.Current.action);
                        annotationExists = annotations.MoveNext();
                        highlightExists = highlights.MoveNext();
                    }
                    else
                    {
                        AddTextFragment(highlights.Current.beginIdx, null);
                        AddTextFragment(highlights.Current.endIdx, highlights.Current.action);
                        highlightExists = highlights.MoveNext();
                    }
                }
                else if (highlightExists)
                {
                    AddTextFragment(highlights.Current.beginIdx, null);
                    AddTextFragment(highlights.Current.endIdx, highlights.Current.action);
                    highlightExists = highlights.MoveNext();
                }
                else if (annotationExists)
                {
                    AddTextFragment(annotations.Current.BeginIndex, null);
                    AddAnnotationFragment(annotations.Current.Annotation);
                    annotationExists = annotations.MoveNext();
                }
                else
                {
                    break;
                }
            }
            AddTextFragment(text.Length, null);
            return this;
        }

        public AnnotatedTextFragmentsBuilder AddAnnotatedTextFragments(
            StringSlice text, IAnnotationsSnapshot annotationsSnapshot)
        {
            return AddAnnotatedTextFragments(text, annotationsSnapshot,
                ImmutableList<IFilter>.Empty, ImmutableArray<Color>.Empty);
        }

        public AnnotatedTextFragmentsBuilder AddText(StringSlice str)
        {
            fragments.Add(new AnnotatedTextFragment() { Value = str });
            return this;
        }
    };
}
