using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net;
using System.IO;
using System.Net.Sockets;
using System.Threading;
using Awesomium.Windows.Controls;
using DWARF;

namespace TcpClient
{
    [Export(typeof(DWARF.IPlugin))]
    [ExportMetadata("Identifier", "TcpClient")]
    public class TcpClientPluginManager : DWARF.IPlugin
    {
        Dictionary<int, TcpClientPlugin> tcpClientInstances = new Dictionary<int, TcpClientPlugin>();

        public object GetData(List<string> args, ApplicationForm form)
        {
            if (args.Count >= 1)
            {
                if (args[0] == "Instantiate")
                {
                    int id = 0;

                    if (tcpClientInstances.Count() > 0)
                        id = tcpClientInstances.Keys.Last()+1;

                    while (tcpClientInstances.Keys.Contains(id))
                        id++;

                    tcpClientInstances.Add(id, new TcpClientPlugin(id, form));

                    return id.ToString();
                }
                else if (args[0] == "Connect")
                {
                    if (tcpClientInstances.ContainsKey(Convert.ToInt32(args[1])))
                    {
                        tcpClientInstances[Convert.ToInt32(args[1])].Connect(IPAddress.Parse(args[2]), Convert.ToInt32(args[3]));
                    }
                }
                else if (args[0] == "Send")
                {
                    tcpClientInstances[Convert.ToInt32(args[1])].Send(args[2]);
                }
                else if (args[0] == "Disconnect")
                {
                    tcpClientInstances[Convert.ToInt32(args[1])].client.Close();
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

    public class TcpClientPlugin
    {
        public int ID;
        public ApplicationForm form;

        public BinaryReader br;
        public BinaryWriter bw;
        public System.Net.Sockets.TcpClient client;

        public TcpClientPlugin(int ID, ApplicationForm form)
        {
            this.ID = ID;
            this.form = form;
            client = new System.Net.Sockets.TcpClient();
        }

        public void Connect(IPAddress addr, int port)
        {
            try
            {
                if (client == null)
                    client = new System.Net.Sockets.TcpClient();

                client.Connect(addr, port);
                br = new BinaryReader(client.GetStream());
                bw = new BinaryWriter(client.GetStream());
                new Thread(WaitForMessages).Start();
                form.JS("TcpClientHelper.onconnected('" + this.ID.ToString() + "');");
            }
            catch (Exception ex)
            {
                form.JS("TcpClientHelper.onconnectionfailed('" + this.ID.ToString() + "', '" + ex.Message.Replace("\\", "\\\\").Replace("'", "\\'") + "');");
            }
        }

        public void Send(string data)
        {
            try
            {
                bw.Write(data);
                form.JS("TcpClientHelper.ondatasent('" + this.ID.ToString() + "', '" + data.Replace("\\", "\\\\").Replace("'", "\\'") + "');");
            }
            catch (Exception ex)
            {
                form.JS("TcpClientHelper.ondatasendfailed('" + this.ID.ToString() + "', '" + ex.Message.Replace("\\", "\\\\").Replace("'", "\\'") + "');");
            }
        }

        public static string ReadNetStream(NetworkStream stream, int maxbytes)
        {
            byte[] readBuffer = new byte[maxbytes];
            StringBuilder str = new StringBuilder();
            int bytesRead = 0;

            do
            {
                bytesRead = stream.Read(readBuffer, 0, readBuffer.Length);
                str.AppendFormat("{0}", Encoding.ASCII.GetString(readBuffer, 0, bytesRead));
            }
            while (stream.DataAvailable);

            return str.ToString();
        }

        void WaitForMessages()
        {
            while (client.Connected)
            {
                string msg = ReadNetStream(client.GetStream(), 2048);

                if (String.IsNullOrEmpty(msg))
                    client.Close();
                else if (msg != "\n")
                {
                    if (msg.EndsWith("\n"))
                        msg = msg.Remove(msg.Length - 1, 1);

                    form.JS("TcpClientHelper.ondatareceived('" + this.ID.ToString() + "', '" + msg.Replace("\\", "\\\\").Replace("'", "\\'") + "');");
                }
            }
            Console.WriteLine("disconnected");
            form.JS("TcpClientHelper.ondisconnected('" + this.ID.ToString() + "');");
            client = null;
        }
    }
}
