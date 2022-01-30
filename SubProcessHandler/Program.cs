using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;

namespace SubProcessHandler
{
    class Program
    {
        static Process RunningProcess;
        static void Main(string[] args)
        {
            if (args.Length >= 1)
            {
                string para = "";
                for (int i = 1; i < args.Length; i++)
                {
                    para += args[i];
                    if (i < args.Length - 1)
                        para += " ";
                }

                Console.CancelKeyPress += new ConsoleCancelEventHandler(Console_CancelKeyPress);
                RunningProcess = new Process();
                try
                {
                    RunningProcess.StartInfo.WorkingDirectory ="";
                    RunningProcess.StartInfo.FileName = args[0] ;
                    RunningProcess.StartInfo.Arguments = para;
                    RunningProcess.StartInfo.CreateNoWindow = false;
                    RunningProcess.StartInfo.UseShellExecute = false;

                    RunningProcess.StartInfo.RedirectStandardError = true;
                    RunningProcess.StartInfo.RedirectStandardOutput = true;
                    RunningProcess.StartInfo.RedirectStandardInput = true;
                    RunningProcess.StartInfo.WindowStyle = ProcessWindowStyle.Normal;

                    RunningProcess.OutputDataReceived += (origin, arg) => WriteTextAsync(arg.Data, false);
                    RunningProcess.ErrorDataReceived += (origin, arg) => WriteTextAsync(arg.Data, true);

                    RunningProcess.Start();

                    RunningProcess.BeginOutputReadLine();
                    RunningProcess.BeginErrorReadLine();
                    Thread threadReceive = new Thread(new ThreadStart(Receive));
                    threadReceive.IsBackground = true;
                    threadReceive.Start();
                }
                catch (Exception)
                {
                }
                RunningProcess.StandardInput.WriteLine(para);
                RunningProcess.StandardInput.Flush();
                Boolean isQuiting = false;
                while (true)
                {
                    String a = Console.ReadLine();
                    if (a.Equals("slowquit"))
                    {
                        if (!isQuiting)
                        {
                            isQuiting = true;
                            Thread threadReceive = new Thread(new ThreadStart(SlowQuit));
                            threadReceive.IsBackground = true;
                            threadReceive.Start();
                        }
                    }
                    else
                        if (a.Equals("fastquit"))
                    {
                        try {
                            RunningProcess.Kill();
                        }
                        catch (Exception)
                        {
                        }
                    }
                    else
                    {
                        RunningProcess.StandardInput.WriteLine(a);
                        RunningProcess.StandardInput.Flush();
                    }
                }
            }
        }
        //import in the declaration for GenerateConsoleCtrlEvent
        [DllImport("kernel32.dll", SetLastError = true)]
        static extern bool GenerateConsoleCtrlEvent(ConsoleCtrlEvent sigevent, int dwProcessGroupId);
        public enum ConsoleCtrlEvent
        {
            CTRL_C = 0,
            CTRL_BREAK = 1,
            CTRL_CLOSE = 2,
            CTRL_LOGOFF = 5,
            CTRL_SHUTDOWN = 6
        }

        private static void SlowQuit()
        {
            SENDING_CTRL_C_TO_CHILD = true;
            GenerateConsoleCtrlEvent(ConsoleCtrlEvent.CTRL_C, RunningProcess.Id);
            RunningProcess.WaitForExit();
            SENDING_CTRL_C_TO_CHILD = false;
        }
        private static void Receive()
        {
            RunningProcess.WaitForExit();
            Console.Error.WriteLine("exited");
        }

        //set up the parents CtrlC event handler, so we can ignore the event while sending to the child
        public static volatile bool SENDING_CTRL_C_TO_CHILD = false;
        static void Console_CancelKeyPress(object sender, ConsoleCancelEventArgs e)
        {
            e.Cancel = SENDING_CTRL_C_TO_CHILD;
        }

        private static void WriteTextAsync(string text, Boolean isError)
        {
            if (text != null)
            {
                if (isError)
                    Console.Error.WriteLine(text);
                else
                    Console.Out.WriteLine(text);
            }
        }
    }
}
