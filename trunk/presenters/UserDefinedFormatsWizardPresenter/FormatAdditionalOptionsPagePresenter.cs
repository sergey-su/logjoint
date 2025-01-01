using System;
using System.Linq;
using System.Collections.Generic;
using System.Text;
using System.Xml;

namespace LogJoint.UI.Presenters.FormatsWizard.FormatAdditionalOptionsPage
{
    internal class Presenter : IPresenter, IViewEvents
    {
        readonly IView view;
        readonly IWizardScenarioHost host;
        readonly Help.IPresenter help;
        XmlNode formatRoot;
        List<string> patterns = new List<string>();
        List<EncodingEntry> encodings = new List<EncodingEntry>();
        LabeledStepperPresenter.IPresenter dejitterBufferStepper;

        public Presenter(
            IView view,
            IWizardScenarioHost host,
            Help.IPresenter help
        )
        {
            this.view = view;
            this.view.SetEventsHandler(this);
            this.host = host;
            this.help = help;

            this.dejitterBufferStepper = new LabeledStepperPresenter.Presenter(view.BufferStepperView);

            UpdateView();
            InitEncodings();
            InitDejitterGauge();
        }

        bool IWizardPagePresenter.ExitPage(bool movingForward)
        {
            XmlNode patternsRoot = formatRoot.SelectSingleNode("patterns");
            if (patternsRoot == null)
                patternsRoot = formatRoot.AppendChild(formatRoot.OwnerDocument.CreateElement("patterns"));
            patternsRoot.RemoveAll();

            foreach (string e in patterns)
                patternsRoot.AppendChild(patternsRoot.OwnerDocument.CreateElement("pattern")).InnerText = e;

            EncodingEntry ee = encodings.ElementAtOrDefault(view.EncodingComboBoxSelection);
            string encodingXMLCode = (ee != null) ? ee.ToXMLString() : "ACP";
            XmlNode encodingNode = formatRoot.SelectSingleNode("encoding");
            if (encodingNode == null)
                encodingNode = formatRoot.AppendChild(formatRoot.OwnerDocument.CreateElement("encoding"));
            encodingNode.InnerText = encodingXMLCode;

            XmlNode dejitterNode = formatRoot.SelectSingleNode("dejitter");
            if (view.EnableDejitterCheckBoxChecked)
            {
                if (dejitterNode == null)
                    dejitterNode = formatRoot.AppendChild(formatRoot.OwnerDocument.CreateElement("dejitter"));
                ((XmlElement)dejitterNode).SetAttribute("jitter-buffer-size", dejitterBufferStepper.Value.ToString());
            }
            else
            {
                if (dejitterNode != null)
                    dejitterNode.ParentNode.RemoveChild(dejitterNode);
            }

            return true;
        }

        object IWizardPagePresenter.ViewObject => view;

        void IPresenter.SetFormatRoot(XmlNode formatRoot)
        {
            this.formatRoot = formatRoot;

            foreach (XmlNode e in formatRoot.SelectNodes("patterns/pattern[text()!='']"))
                patterns.Add(e.InnerText);
            view.SetPatternsListBoxItems(patterns.ToArray());

            XmlNode encodingNode = formatRoot.SelectSingleNode("encoding");
            string encoding = "";
            if (encodingNode != null)
                encoding = encodingNode.InnerText;
            view.EncodingComboBoxSelection = encodings.IndexOf(ee => ee.ToXMLString() == encoding).GetValueOrDefault(-1);

            int? dejitterBufferSize = null;
            var dejitterBufferSizeNode = formatRoot.SelectSingleNode("dejitter/@jitter-buffer-size");
            if (dejitterBufferSizeNode != null)
            {
                int parseResult;
                if (int.TryParse(dejitterBufferSizeNode.Value, out parseResult))
                    dejitterBufferSize = parseResult;
            }
            view.EnableDejitterCheckBoxChecked = dejitterBufferSize.HasValue;
            dejitterBufferStepper.Value = dejitterBufferSize.GetValueOrDefault(10);
            UpdateView();
        }

        void UpdateView()
        {
            view.EnableControls(
                addExtensionButton: GetValidExtension() != null,
                removeExtensionButton: view.GetPatternsListBoxSelection().Any()
            );
            dejitterBufferStepper.Enabled = view.EnableDejitterCheckBoxChecked;
        }

        public enum EntryType
        {
            UseBOM,
            UseACP,
            Encoding
        }

        class EncodingEntry : IComparable<EncodingEntry>
        {
            public readonly EntryType Type;
            public readonly EncodingInfo Info;

            public EncodingEntry(EntryType t, EncodingInfo ei)
            {
                Type = t;
                Info = ei;
            }

            public string ToXMLString()
            {
                switch (Type)
                {
                    case EntryType.UseACP:
                        return "ACP";
                    case EntryType.UseBOM:
                        return "BOM";
                    case EntryType.Encoding:
                        return Info.Name;
                }
                return "";
            }

            public override string ToString()
            {
                switch (Type)
                {
                    case EntryType.UseBOM:
                        return "(Determine the encoding with BOM)";
                    case EntryType.UseACP:
                        return "(Use system's current ANSII codepage)";
                    case EntryType.Encoding:
                        return Info.Name;
                }
                return "";
            }
            int GetSortOrder()
            {
                if (Type == EntryType.UseACP)
                    return 0;
                if (Type == EntryType.UseBOM)
                    return 1;
                if (Info.Name == "utf-8")
                    return 10;
                if (Info.Name == "utf-16")
                    return 11;
                return 0xff;
            }

            int IComparable<EncodingEntry>.CompareTo(EncodingEntry other)
            {
                int order1 = this.GetSortOrder();
                int order2 = other.GetSortOrder();
                if (order1 != order2)
                    return order1 - order2;
                if (order1 == 0xff)
                    return this.Info.Name.CompareTo(other.Info.Name);
                return 0;
            }
        };

        void InitEncodings()
        {
            var entries = encodings;
            entries.Clear();
            entries.Add(new EncodingEntry(EntryType.UseBOM, null));
            entries.Add(new EncodingEntry(EntryType.UseACP, null));
            foreach (EncodingInfo e in Encoding.GetEncodings())
                entries.Add(new EncodingEntry(EntryType.Encoding, e));
            entries.Sort();
            view.SetEncodingComboBoxItems(entries.Select(e => e.ToString()).ToArray());
        }


        string GetValidExtension()
        {
            string ext = view.ExtensionTextBoxValue.Trim();
            if (ext == "")
                return null;
            if (ext.IndexOfAny(new char[] { '\\', '/', ':', '"', '<', '>', '|' }) >= 0)
                return null;
            return ext;
        }

        void IViewEvents.OnExtensionTextBoxChanged()
        {
            UpdateView();
        }

        void IViewEvents.OnExtensionsListBoxSelectionChanged()
        {
            UpdateView();
        }

        void IViewEvents.OnAddExtensionClicked()
        {
            string ext = GetValidExtension();
            if (ext == null)
                return;
            if (patterns.IndexOf(i => string.Compare(i, ext, ignoreCase: true) == 0) >= 0)
                return;
            patterns.Add(ext);
            view.SetPatternsListBoxItems(patterns.ToArray());
            view.ExtensionTextBoxValue = "";
            UpdateView();
        }

        void IViewEvents.OnDelExtensionClicked()
        {
            foreach (var i in view.GetPatternsListBoxSelection().OrderByDescending(i => i))
                patterns.RemoveAt(i);
            view.SetPatternsListBoxItems(patterns.ToArray());
            UpdateView();
        }

        void IViewEvents.OnEnableDejitterCheckBoxClicked()
        {
            UpdateView();
        }

        void IViewEvents.OnDejitterHelpLinkClicked()
        {
            help.ShowHelp("Dejitter.htm");
        }

        private void InitDejitterGauge()
        {
            dejitterBufferStepper.AllowedValues = new int[] {
                5,
                10,
                20,
                40,
                60,
                80
            };
            dejitterBufferStepper.Value = 10;
        }
    };
};