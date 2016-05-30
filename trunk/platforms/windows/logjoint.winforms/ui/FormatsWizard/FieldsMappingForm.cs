using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using System.Xml;
using System.Runtime.InteropServices;
using System.IO;
using System.Xml.Linq;

namespace LogJoint.UI
{
	public partial class FieldsMappingForm : Form
	{
		static readonly string[] predefindOutputFields = new string[]
			{ "Time", "Thread", "Body", "Severity", "EntryType"	};
		int fieldIndex = 0;
		readonly XmlNode grammarRoot;
		bool updateLock;
		readonly string[] availableInputFields;
		readonly Presenters.Help.IPresenter help;
		readonly ITempFilesManager tempFileManager;


		public FieldsMappingForm(XmlNode root, string[] availableInputFields, Presenters.Help.IPresenter help, ITempFilesManager tempFileManager)
		{
			this.grammarRoot = root;
			this.availableInputFields = availableInputFields;
			this.help = help;
			this.tempFileManager = tempFileManager;

			InitializeComponent();
			InitAvailableFieldsList(availableInputFields);

			ReadMapping();
			UpdateView();
			InitTabStops();
		}

		[DllImport("user32.dll", EntryPoint = "SendMessage", CharSet = CharSet.Auto)]
		public static extern IntPtr SendTabStopsMessage(HandleRef hWnd, int msg,
			int wParam, [In, MarshalAs(UnmanagedType.LPArray)] uint[] stops);

		void InitTabStops()
		{
			int EM_SETTABSTOPS = 0x00CB;
			SendTabStopsMessage(new HandleRef(codeTextBox, codeTextBox.Handle), EM_SETTABSTOPS, 1, new uint[] { 16 });
		}

		void InitAvailableFieldsList(string[] availableInputFields)
		{
			foreach (string f in availableInputFields)
			{
				LinkLabel l = new LinkLabel();
				l.Text = f;
				l.AutoSize = true;
				l.Click += AvailableLinkClick;
				availableInputFieldsPanel.Controls.Add(l);
			}
		}

		void AvailableLinkClick(object sender, EventArgs e)
		{
			string txt = ((LinkLabel)sender).Text;
			int selIdx = codeTextBox.SelectionStart;
			codeTextBox.Text = codeTextBox.Text.Insert(selIdx, txt);
			codeTextBox.SelectionStart = selIdx + txt.Length;
			codeTextBox.Focus();
		}

		void ReadMapping()
		{
			foreach (XmlElement e in grammarRoot.SelectNodes("fields-config/field[@name]"))
			{
				Field f = new Field(e.GetAttribute("name"));
				if (e.GetAttribute("code-type") == "function")
					f.CodeType = FieldCodeType.Function;
				f.Code = e.InnerText;
				fieldsListBox.Items.Add(f);
			}
		}

		XmlNode WriteMappingInternal(XmlNode grammarRoot)
		{
			XmlNode cfgNode = grammarRoot.SelectSingleNode("fields-config");
			if (cfgNode != null)
				cfgNode.RemoveAll();
			else
				cfgNode = grammarRoot.AppendChild(grammarRoot.OwnerDocument.CreateElement("fields-config"));
			foreach (Field f in fieldsListBox.Items)
			{
				XmlElement e = grammarRoot.OwnerDocument.CreateElement("field");
				e.SetAttribute("name", f.Name);
				if (f.CodeType == FieldCodeType.Function)
					e.SetAttribute("code-type", "function");
				e.AppendChild(grammarRoot.OwnerDocument.CreateCDataSection(f.Code));
				cfgNode.AppendChild(e);
			}
			return cfgNode;
		}

		void WriteMapping()
		{
			WriteMappingInternal(this.grammarRoot);
		}

		public enum FieldCodeType
		{
			Expression,
			Function
		};

		public class Field
		{
			public string Name;
			public FieldCodeType CodeType = FieldCodeType.Expression;
			public string Code = "";

			public Field(string name)
			{
				Name = name;
			}

			public override string ToString()
			{
				return Name;
			}
		};

		private void addFieldButton_Click(object sender, EventArgs e)
		{
			Field f = new Field(string.Format("Field {0}", ++fieldIndex));
			fieldsListBox.SelectedIndex = fieldsListBox.Items.Add(f);
		}

		Field Get()
		{
			if (fieldsListBox.SelectedIndex >= 0)
				return fieldsListBox.Items[fieldsListBox.SelectedIndex] as Field;
			return null;
		}

		Field Get(string name)
		{
			foreach (Field f in fieldsListBox.Items)
			{
				if (f.Name == name)
					return f;
			}
			return null;
		}

		void UpdateView()
		{
			if (updateLock)
				return;
			Field f = Get();
			removeFieldButton.Enabled = f != null;
			nameComboBox.Enabled = f != null;
			codeTypeComboBox.Enabled = f != null;
			codeTextBox.Enabled = f != null;
			availableInputFieldsPanel.Enabled = f != null;
			if (f != null)
			{
				nameComboBox.Text = f.Name;
				codeTypeComboBox.SelectedIndex = (int)f.CodeType;
				codeTextBox.Text = f.Code;

				nameComboBox.Items.Clear();
				foreach (string s in predefindOutputFields)
				{
					if (Get(s) == null)
						nameComboBox.Items.Add(s);
				}
			}
			else
			{
				nameComboBox.Text = "";
				codeTypeComboBox.SelectedIndex = -1;
				codeTextBox.Text = "";
			}
		}

		private void fieldsListBox_SelectedIndexChanged(object sender, EventArgs e)
		{
			UpdateView();
		}

		bool TrySelect(int idx)
		{
			if (idx >= 0 && idx < fieldsListBox.Items.Count)
			{
				fieldsListBox.SelectedIndex = idx;
				return true;
			}
			return false;
		}

		private void removeFieldButton_Click(object sender, EventArgs e)
		{
			int idx = fieldsListBox.SelectedIndex;
			if (idx >= 0)
				fieldsListBox.Items.RemoveAt(idx);
			if (!TrySelect(idx) && !TrySelect(idx - 1))
				UpdateView();
		}

		private void nameComboBox_TextUpdate(object sender, EventArgs e)
		{
			Field f = Get();
			if (f == null)
				return;
			if (f.Name == nameComboBox.Text)
				return;
			f.Name = nameComboBox.Text;
			updateLock = true;
			try
			{
				fieldsListBox.Items[fieldsListBox.SelectedIndex] = "";
				fieldsListBox.Items[fieldsListBox.SelectedIndex] = f;
			}
			finally
			{
				updateLock = false;
			}
		}

		private void codeTypeComboBox_SelectedIndexChanged(object sender, EventArgs e)
		{
			if (Get() != null)
				Get().CodeType = (FieldCodeType)codeTypeComboBox.SelectedIndex;
		}

		private void codeTextBox_TextChanged(object sender, EventArgs e)
		{
			if (Get() != null)
				Get().Code = codeTextBox.Text;
		}

		string ValidateInput()
		{
			Dictionary<string, bool> fldMap = new Dictionary<string, bool>();
			foreach (Field f in fieldsListBox.Items)
			{
				if (f.Name == "")
					return "One of the fields has empty name. The name can not be empty.";
				if (fldMap.ContainsKey(f.Name))
					return "Field name duplicate: " + f.Name;
				fldMap.Add(f.Name, true);
			}
			if (!fldMap.ContainsKey("Time"))
				return "There must create a field with name 'Time'. This field is required.";
			return null;
		}

		private void okButton_Click(object sender, EventArgs e)
		{
			string msg = ValidateInput();
			if (msg != null)
			{
				MessageBox.Show(msg, "Validation", MessageBoxButtons.OK, MessageBoxIcon.Warning);
				return;
			}
			WriteMapping();
			this.DialogResult = DialogResult.OK;
		}
		
		void HandleProcessorError(BadUserCodeException exception, bool alwaysFallToAdvancedMode)
		{
			if (!alwaysFallToAdvancedMode && exception.BadField != null)
			{
				Field field = Get(exception.BadField.FieldName);
				if (field != null)
				{
					fieldsListBox.SelectedItem = field;
					codeTextBox.SelectionStart = exception.BadField.ErrorPosition;
					codeTextBox.SelectionLength = 1;
					codeTextBox.Focus();

					MessageBox.Show(exception.ErrorMessage, "Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
					return;
				}
			}

			if (MessageBox.Show(
				"LogJoint tried to combine your code into a class that would create LogJoin messages out of the regex captures. " +
				"The combined code can not be compiled and the errors are outside your code. " +
				"Although most likely the errors are caused by mistakes in the code you provided. " +
				"It is recommended to doublecheck fields code.\n\n" +
				"Error message: " + exception.ErrorMessage + "\n\n" +
				"LogJoint can save combined code and detailed error messages into a file so you could analize them. " +
				"Do you want to save this file?",
				"Error",
				MessageBoxButtons.YesNo,
				MessageBoxIcon.Warning,
				MessageBoxDefaultButton.Button2) == DialogResult.Yes)
			{
				if (saveFileDialog1.ShowDialog() == DialogResult.OK)
				{
					try
					{
						using (TextWriter fs = new StreamWriter(saveFileDialog1.FileName, false, Encoding.UTF8))
						{
							fs.WriteLine(exception.FullCode);
							fs.WriteLine();
							fs.WriteLine("Compilation errors:");
							fs.Write(exception.AllErrors);
						}
					}
					catch (Exception e)
					{
						MessageBox.Show("Failed to save file. " + e.Message, "Error", MessageBoxButtons.OK,
							MessageBoxIcon.Warning);
					}
				}
			}
		}

		private void testButton_Click(object sender, EventArgs evt)
		{
			XmlDocument tmp = new XmlDocument();
			XmlNode root = tmp.AppendChild(tmp.CreateElement("root"));
			XmlNode mapping = WriteMappingInternal(root);

			XDocument tmpXDoc = XDocument.Parse(tmp.OuterXml);
			
			FieldsProcessor.InitializationParams tmpProcessorParams = new FieldsProcessor.InitializationParams(
				tmpXDoc.Element("root").Element("fields-config"), false, null);
			try
			{
				FieldsProcessor tmpProcessor = new FieldsProcessor(tmpProcessorParams, availableInputFields, null, tempFileManager);
				tmpProcessor.Reset();
				MessageBox.Show("Code compiled OK", "", MessageBoxButtons.OK, MessageBoxIcon.Information);
			}
			catch (BadUserCodeException e)
			{
				HandleProcessorError(e, (Control.ModifierKeys & Keys.Control) != 0);
			}
		}

		private void linkLabel1_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
		{
			help.ShowHelp("FieldsMapping.htm");
		}
	}
}