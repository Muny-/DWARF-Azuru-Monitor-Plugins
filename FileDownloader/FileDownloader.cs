using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using DWARF;
using System.Net;

namespace FileDownloader
{
    [Export(typeof(DWARF.IPlugin))]
    [ExportMetadata("Identifier", "FileDownloader")]
    public class FileDownloaderPlugin : DWARF.IPlugin
    {
        Dictionary<int, FileDownloader> downloaderInstances = new Dictionary<int, FileDownloader>();

        public object GetData(List<string> args, ApplicationForm form)
        {
            if (args.Count >= 1)
            {
                if (args[0] == "Instantiate")
                {
                    int id = 0;

                    if (downloaderInstances.Count() > 0)
                        id = downloaderInstances.Keys.Last() + 1;

                    while (downloaderInstances.Keys.Contains(id))
                        id++;

                    downloaderInstances.Add(id, new FileDownloader(id, form));

                    return id.ToString();
                }
                else if (args[0] == "Download")
                {
                    if (downloaderInstances.ContainsKey(Convert.ToInt32(args[1])))
                    {
                        downloaderInstances[Convert.ToInt32(args[1])].Download((string)args[2]);
                    }
                }
                else if (args[0] == "CancelDownload")
                {
                    if (downloaderInstances.ContainsKey(Convert.ToInt32(args[1])))
                    {
                        downloaderInstances[Convert.ToInt32(args[1])].CancelDownload();
                    }
                }
            }

            return "";
        }

        public void OnEnabled()
        {

        }

        public void OnDisabled()
        {

        }
    }

    public class FileDownloader
    {
        // error codes: 
        // 0 = Invalid or malformed URI string.
        // 1 = User cancelled choosing file.
        // 2 = Download was cancelled in-progress.
        // 3 = Other - usually a connection error (or unknown)

        public Dictionary<int, string> ErrorCodes = new Dictionary<int, string>() {
            {0, "Invalid or malformed URI string."},
            {1, "User cancelled choosing file destination."},
            {2, "Dpwmload cancelled in-progress."},
            {3, "Unknown exception"}
        };

        public int ID;
        public ApplicationForm form;

        public WebClient WC;

        public FileDownloader(int ID, ApplicationForm form)
        {
            this.ID = ID;
            this.form = form;
            WC = new WebClient();
            WC.DownloadProgressChanged += WC_DownloadProgressChanged;
            WC.DownloadFileCompleted += WC_DownloadFileCompleted;
        }

        void WC_DownloadFileCompleted(object sender, System.ComponentModel.AsyncCompletedEventArgs e)
        {
            if (e.Cancelled)
            {
                form.JS("FileDownloaderHelper.ondownloadfailed(" + this.ID.ToString().CleanForJavascript() + ", 2, '" + ErrorCodes[2] + "');");
            }
            else
            {
                if (e.Error != null)
                {
                    form.JS("FileDownloaderHelper.ondownloadfailed(" + this.ID.ToString().CleanForJavascript() + ", 3, '" + e.Error.Message.CleanForJavascript() + "');");
                }
                else
                {
                    form.JS("FileDownloaderHelper.ondownloadcomplete(" + this.ID.ToString().CleanForJavascript() + ");");
                }
            }
        }

        void WC_DownloadProgressChanged(object sender, DownloadProgressChangedEventArgs e)
        {
            form.JS("FileDownloaderHelper.onprogressupdate(" + this.ID.ToString().CleanForJavascript() + ", " + e.ProgressPercentage.ToString().CleanForJavascript() + ", " + e.BytesReceived.ToString().CleanForJavascript() + ", " + e.TotalBytesToReceive.ToString().CleanForJavascript() + ");");
        }

        public void Download(string URL)
        {
            try
            {
                SaveFileDialog sfd = new SaveFileDialog();
                sfd.Filter = "Any type | *";

                if (sfd.ShowDialog() == DialogResult.OK)
                {
                    try
                    {
                        Uri uri = new Uri(URL);
                        WC.DownloadFileAsync(uri, sfd.FileName);
                        form.JS("FileDownloaderHelper.ondownloadstarted(" + this.ID.ToString().CleanForJavascript() + ");");
                    }
                    catch (UriFormatException ex)
                    {
                        form.JS("FileDownloaderHelper.ondownloadfailed(" + this.ID.ToString().CleanForJavascript() + ", 0, '" + ErrorCodes[0] + "');");
                    }
                }
                else
                {
                    form.JS("FileDownloaderHelper.ondownloadfailed(" + this.ID.ToString().CleanForJavascript() + ", 1, '" + ErrorCodes[1] + "');");
                }

                sfd.Dispose();
            }
            catch (Exception ex)
            {
                form.JS("FileDownloaderHelper.ondownloadfailed(" + this.ID.ToString() + ", '" + ex.Message.CleanForJavascript() + "');");
            }
        }

        public void CancelDownload()
        {
            WC.CancelAsync();
        }
    }
}
