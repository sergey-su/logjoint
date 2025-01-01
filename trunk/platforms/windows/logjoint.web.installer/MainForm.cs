using System;
using System.Drawing;
using System.IO;
using System.IO.Compression;
using System.Net;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Xml.Linq;
using WSH = IWshRuntimeLibrary;
using System.Diagnostics;
using System.ComponentModel;

namespace LogJoint.Installer
{
    public partial class MainForm : Form
    {
        StringBuilder warnings = new StringBuilder();
        bool progress;

        public MainForm()
        {
            InitializeComponent();
        }

        private void MainForm_Load(object sender, EventArgs e)
        {
            UpdateLayout();
        }

        void UpdateLayout()
        {
            int contentBottom;
            if (advancedOptionsPanel.Visible)
                contentBottom = advancedOptionsPanel.Bottom;
            else
                contentBottom = advancedOptionsLinkLabel.Bottom;
            var csz = this.ClientSize;
            this.advancedOptionsLinkLabel.Text = "Advanced options " + (advancedOptionsPanel.Visible ? "<<" : ">>");
            this.ClientSize = new Size(csz.Width, contentBottom + (csz.Height - startButton.Top) + 5);
        }

        private void advancedOptionsLinkLabel_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            advancedOptionsPanel.Visible = !advancedOptionsPanel.Visible;
            UpdateLayout();
        }

        private void cencelButton_Click(object sender, EventArgs e)
        {
            Close();
        }

        private async void startButton_Click(object sender, EventArgs e)
        {
            Action<bool> enableView = f =>
            {
                createDesktopShortcutCheckBox.Enabled = f;
                openInstallationFolderCheckBox.Enabled = f;
                startLJCheckBox.Enabled = f;
                advancedOptionsPanel.Enabled = f;
                startButton.Enabled = f;
                cencelButton.Enabled = f;
            };
            enableView(false);
            try
            {
                if (!await DoTheJob())
                {
                    enableView(true);
                }
            }
            catch (Exception ex)
            {
                progress = false;
                statusLabel.Text = "Error!";
                MessageBox.Show(ex.Message, "Installation failed");
                statusLabel.Text = "";
                enableView(true);
                return;
            }
            if (warnings.Length > 0)
            {
                cencelButton.Enabled = true;
                cencelButton.Text = "Close";
                MessageBox.Show(warnings.ToString(), "Installer warnings", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
            else
            {
                Close();
            }
        }

        async Task<bool> DoTheJob()
        {
            var installationDir = Environment.ExpandEnvironmentVariables(targetFolderTextBox.Text);
            if (Directory.Exists(installationDir) && Directory.EnumerateFileSystemEntries(installationDir).Any())
            {
                var rsp = MessageBox.Show("Target directory already exists. Delete current content?", "Installer", MessageBoxButtons.YesNoCancel, MessageBoxIcon.Warning);
                if (rsp == DialogResult.Cancel || rsp == DialogResult.No)
                    return false;
                Directory.Delete(installationDir, true);
            }
            progress = true;
            var request = HttpWebRequest.CreateHttp(@"https://publogjoint.blob.core.windows.net/updates/logjoint.zip");
            request.Method = "GET";
            statusLabel.Text = "Downloading package...";
            using (var response = (HttpWebResponse)await request.GetResponseAsync())
            {
                var zipFileName = Path.GetTempFileName();
                try
                {
                    using (var zipStream = new FileStream(zipFileName, FileMode.Create, FileAccess.ReadWrite))
                    {
                        await response.GetResponseStream().CopyToAsync(zipStream);
                    }
                    using (var zip = ZipFile.OpenRead(zipFileName))
                    {
                        statusLabel.Text = "Extracting...";
                        await Task.Delay(50);
                        zip.ExtractToDirectory(installationDir);
                    }
                    WriteUpdateInfoFile(Path.Combine(installationDir, "update-info.xml"), response.Headers[HttpResponseHeader.ETag]);
                }
                finally
                {
                    File.Delete(zipFileName);
                }
            }
            statusLabel.Text = "Finalizing...";
            await Task.Delay(50);
            var appExePath = Path.Combine(installationDir, "logjoint.exe");
            CreateShortcut(appExePath, Environment.GetFolderPath(Environment.SpecialFolder.Programs));
            if (createDesktopShortcutCheckBox.Checked)
                CreateShortcut(appExePath, Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory));
            if (openInstallationFolderCheckBox.Checked)
                Process.Start("explorer.exe", "/select,\"" + appExePath + "\"");
            if (startLJCheckBox.Checked)
                Process.Start(appExePath);
            statusLabel.Text = "";
            progress = false;
            return true;
        }


        static void WriteUpdateInfoFile(string fileName, string etag)
        {
            var doc = new XDocument(new XElement(
                "root",
                new XAttribute("binaries-etag", etag),
                new XAttribute("last-check-timestamp", DateTime.UtcNow.ToString("o"))
            ));
            doc.Save(fileName);
        }

        string CreateShortcut(string exe, string shortcutFileLocation)
        {
            try
            {
                var shell = new WSH.WshShell();
                string shortcutAddress = Path.Combine(shortcutFileLocation, "LogJoint.lnk");
                var shortcut = (WSH.IWshShortcut)shell.CreateShortcut(shortcutAddress);
                shortcut.Description = "LogJoint Log Viewer";
                shortcut.TargetPath = exe;
                shortcut.Save();
                return shortcutAddress;
            }
            catch (Exception e)
            {
                warnings.AppendLine("Failed to create shortcut in '" + shortcutFileLocation + "': " + e.Message);
                return null;
            }
        }

        private void MainForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (progress)
                if (MessageBox.Show("Intallation did not complete. Quit anyway?", "Confirmation", MessageBoxButtons.YesNoCancel) != DialogResult.Yes)
                    e.Cancel = true;
        }

        private void button1_Click(object sender, EventArgs e)
        {
            folderBrowserDialog1.SelectedPath = Environment.ExpandEnvironmentVariables(targetFolderTextBox.Text);
            if (folderBrowserDialog1.ShowDialog() == DialogResult.OK)
                targetFolderTextBox.Text = folderBrowserDialog1.SelectedPath;
        }
    }
}
