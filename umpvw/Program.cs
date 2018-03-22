using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Pipes;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace umpvw
{
    static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main(String[] args)
        {
            //var path = @"C:\Users\i\Desktop\test\";
            //var w1 = path + @"1.webm";
            //var w2 = path + @"2.webm";
            //var w3 = path + @"3.webm";
            //var w4 = path + @"this [brace] name.mp4";
            //var w5 = path + @"this 'quote' name.mp4";
            //var w6 = path + @"this space name.mp4";
            //args = new string[] { w1, w2, w3, w4, w5, w6 };

            // passed multiple arguments. no need to deal with launcher ipc
            if (args.Length > 1)
            {
                var pipe = MpvLaunch();

                MpvLoadFile(args[0], false, pipe);
                //rest of the files get appended
                for (int i = 1; i < args.Length; i++)
                {
                    MpvLoadFile(args[i], true, pipe);
                }
            }
            else if (args.Length == 1)
            {
                doIpc(args[0]);
            }
            else if (args.Length == 0)
            {
                MpvLaunch();
            }
        }

        static private string pipePrefix = @"\\.\pipe\";
        static private string mpvPipe = "umpvw-mpv-pipe";
        static private string umpvwPipe = "umpvw-pipe";

        static private NamedPipeServerStream serverPipe;
        static private bool timeout = false;
        static private int timer = 300;

        static void serverTimeout()
        {
            Thread.Sleep(timer);
            timeout = true;
            var pipe = new NamedPipeClientStream(umpvwPipe);
            try
            {
                pipe.Connect();
            }
            catch (Exception)
            {
                Application.Exit();
            }
            pipe.Dispose();

        }

        static void doIpc(string arg)
        {
            bool createdNew;  
            var m_Mutex = new Mutex(true, "umpvwMutex", out createdNew);  

            if (createdNew) // server role
            {
                var pipe = MpvLaunch(); //start mpv first

                serverPipe = new NamedPipeServerStream(umpvwPipe);
                var pipeReader = new StreamReader(serverPipe);
                var thread = new Thread(new ThreadStart(serverTimeout));
                thread.Start();

                var list = new List<string>();
                list.Add(arg);

                while (timeout == false)
                {
                    serverPipe.WaitForConnection();
                    var s = pipeReader.ReadLine();
                    if (!String.IsNullOrEmpty(s))
                    {
                        list.Add(s);
                    }
                    serverPipe.Disconnect();
                }
                //new Thread(() => System.Windows.Forms.MessageBox.Show(String.Join(", ", list))).Start();
                list.Sort();
                MpvLoadFile(list.First(), false, pipe);
                for (int i = 1; i < list.Count; i++)
                {
                    MpvLoadFile(list.ElementAt(i), true, pipe);
                }

            }
            else {  // client role
                var clientPipe = new NamedPipeClientStream(umpvwPipe);
                try
                {
                    clientPipe.Connect(timer);
                }
                catch (Exception)
                {
                    return;
                }
                var pipeWriter = new StreamWriter(clientPipe);
                pipeWriter.Write(arg);
                pipeWriter.Flush();
            }
        }

        static string mpvPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "mpv.exe");
        //static string mpvPath = "mpv.exe";

        // launch mpv or get pipe 
        static NamedPipeClientStream MpvLaunch()
        {
            //if mpv is not running, start it
            if (!File.Exists(pipePrefix + mpvPipe))
            {
                //ensure we are launching the mpv executable from the current folder. also launch the .exe specifically as we don't need the command line.
                Process.Start(mpvPath, @"--input-ipc-server=" + pipePrefix + mpvPipe);
            }
            var pipe = new NamedPipeClientStream(mpvPipe);
            pipe.Connect();
            return pipe;
        }

        // load file into mpv
        static void MpvLoadFile(string file, bool append, NamedPipeClientStream pipe)
        {
            var command = append ? "append" : "replace";
            WriteString("loadfile \"" + file.Replace("\\", "\\\\") + "\" " + command, pipe);
        }

        // write to mpv stream in utf-8
        static public void WriteString(string outString, Stream ioStream)
        {
            byte[] outBuffer = Encoding.UTF8.GetBytes(outString + "\n");
            int len = outBuffer.Length;
            ioStream.Write(outBuffer, 0, len);
            ioStream.Flush();
        }
    }
}
