using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
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
            MpvLaunch(args);
        }

        static private string pipePrefix = @"\\.\pipe\";
        static private string umpvwPipe = "umpvw-pipe";

        static public void WriteString(string outString, Stream ioStream)
        {
            byte[] outBuffer = Encoding.UTF8.GetBytes(outString + "\n");
            int len = outBuffer.Length;
            ioStream.Write(outBuffer, 0, len);
            ioStream.Flush();
        }

        static void MpvLaunch(String[] files)
        {
            //if mpv is not running, start it
            if (!File.Exists(pipePrefix + umpvwPipe))
            {
                //ensure we are launching the mpv executable from the current folder. also launch the .exe specifically as we don't need the command line.
                Process.Start(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "mpv.exe"), @"--input-ipc-server=" + pipePrefix + umpvwPipe);
            }

            // don't bother connecting to the type is there are no files to open. we are done.
            if (files.Length == 0)
            {
                return;
            }

            // establish connection to mpv pipe
            var pipe = new System.IO.Pipes.NamedPipeClientStream(umpvwPipe);
            pipe.Connect();

            //first file replaces the entire queue
            WriteString("loadfile \"" + files[0].Replace("\\", "\\\\") + "\" replace", pipe);

            //rest of the files get appended
            for (int i = 1; i < files.Length; i++)
            {
                WriteString("loadfile \"" + files[i].Replace("\\", "\\\\") + "\" append", pipe);
            }
        }
    }
}
