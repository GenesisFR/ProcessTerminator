using CommandLine;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Security;
using System.Threading;

namespace ProcessTerminator
{
    internal class Win32
    {
        [DllImport("Kernel32")]
        internal static extern bool SetConsoleCtrlHandler(EventHandler handler, bool add);
        internal delegate bool EventHandler(CtrlType sig);

        [DllImport("user32.dll")]
        internal static extern bool EnumWindows(EnumWindowsProc enumProc, IntPtr lParam);
        internal delegate bool EnumWindowsProc(IntPtr hWnd, IntPtr lParam);

        [DllImport("user32.dll")]
        internal static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint lpdwProcessId);

        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        internal static extern IntPtr SendMessage(IntPtr hWnd, int msg, IntPtr wParam, IntPtr lParam);

        internal class Constants
        {
            internal const int WM_CLOSE = 0x10;
            internal const int WM_QUIT  = 0x12;
        }

        internal enum CtrlType
        {
            CTRL_C_EVENT = 0,
            CTRL_BREAK_EVENT = 1,
            CTRL_CLOSE_EVENT = 2,
            CTRL_LOGOFF_EVENT = 5,
            CTRL_SHUTDOWN_EVENT = 6
        }
    }

    class Options
    {
        [Option('n', "name", Required = true,
            HelpText = "Name of the process to terminate.")]
        public string Name { get; set; }

        [Option('t', "timeout", Required = false, Default = 5000,
          HelpText = "Time in milliseconds before searching for the process again.")]
        public int Timeout { get; set; }

        [Option('p', "program", Required = false, Min = 1, 
            HelpText = "Path to the program to run at startup, with optional arguments.")]
        public IEnumerable<string> Program { get; set; }
    }

    class ProcessTerminator
    {
        // Events
        static Win32.EventHandler _handler;

        // Fields
        private Options _parsedArguments;
        private Process[] _processes;
        private string _programName;
        private string _programArguments;


        public ProcessTerminator(string[] args)
        {
            // Parse and validate arguments
            var parser = new Parser(with => { with.IgnoreUnknownArguments = true; with.HelpWriter = Console.Error; });
            var result = parser.ParseArguments<Options>(args).WithParsed<Options>(opts => _parsedArguments = opts);

            // Print an error message if the argument syntax is invalid
            if (result.Tag == ParserResultType.NotParsed)
            {
                Console.WriteLine("Usage: ProcessTerminator.exe -n <process name> [-t <milliseconds>] [-p <program> [arguments]]");
                Console.WriteLine("Example usage: ProcessTerminator.exe - n Skype - t 1000 - p java \"MyTool - check - verify logfile.out\"");
                return;
            }

            Initialize();
            AddCloseEvent();

            if (_programName != null)
                RunProgram();

            SearchProcess();
            Cleanup();
        }

        private void AddCloseEvent()
        {
            // Some boilerplate to react to close window event, CTRL-C, kill, etc
            _handler += new Win32.EventHandler(Handler);
            Win32.SetConsoleCtrlHandler(_handler, true);
        }

        private void Cleanup()
        {
            Console.WriteLine("Shutting down");

            // Try to gracefully close all processes
            foreach (var proc in _processes)
            {
                Win32.EnumWindows(delegate (IntPtr hWnd, IntPtr lParam)
                {
                    Win32.GetWindowThreadProcessId(hWnd, out uint processId);

                    if (processId == (uint)lParam)
                    {
                        Win32.SendMessage(hWnd, Win32.Constants.WM_CLOSE, IntPtr.Zero, IntPtr.Zero);
                        Win32.SendMessage(hWnd, Win32.Constants.WM_QUIT, IntPtr.Zero, IntPtr.Zero);
                    }

                    return true;
                },
                (IntPtr)proc.Id);
            }

            // Give some time to the processes to close
            Thread.Sleep(2000);

            _processes = Process.GetProcessesByName(_parsedArguments.Name);

            // They had one chance, now kill them all
            foreach (Process process in _processes)
            {
                try
                {
                    if (!process.HasExited)
                        process.Kill();
                }
                catch
                {
                    // ignore exceptions (usually due to trying to kill non-existant child processes)
                }
            }
        }

        
        private bool Handler(Win32.CtrlType sig)
        {
            try
            {
                // Kill the program
                Cleanup();

                // Shutdown right away so there are no lingering threads
                Environment.Exit(-1);
            }
            catch
            {
                // Nothing to do here
            }

            return true;
        }

        private void Initialize()
        {
            _programName = _parsedArguments.Program.FirstOrDefault();
            _programArguments = string.Join(" ", _parsedArguments.Program.Skip(1));
        }

        private void RunProgram()
        {
            Process proc = new Process();

            try
            {
                if (_programArguments == "")
                    Console.WriteLine($"Running {_programName}");
                else
                    Console.WriteLine($"Running {_programName} with the following arguments: {_programArguments}");

                proc.StartInfo.Arguments = _programArguments;
                proc.StartInfo.FileName = _programName;
                proc.StartInfo.WorkingDirectory = new FileInfo(_programName).DirectoryName;
                proc.Start();
            }
            catch (SecurityException)
            {
                Console.WriteLine("The caller does not have the required permission.");
            }
            catch (ArgumentException)
            {
                Console.WriteLine($"The file name is empty, contains only white spaces, or contains invalid characters: {_programArguments}");
            }
            catch (UnauthorizedAccessException)
            {
                Console.WriteLine($"Access to fileName is denied: {_programArguments}");
            }
            catch (PathTooLongException)
            {
                Console.WriteLine($"The specified path, file name, or both exceed the system-defined maximum length: {_programArguments}");
            }
            catch (NotSupportedException)
            {
                Console.WriteLine($"File name contains a colon(:) in the middle of the string: {_programArguments}");
            }
            catch (InvalidOperationException)
            {
                Console.WriteLine("No file name was specified");
            }
            catch (Win32Exception)
            {
                Console.WriteLine($"There was an error in opening the associated file: {_programArguments}");
            }
            catch (Exception e)
            {
                Console.WriteLine($"Unhandled exception: {e.Message}");
            }
            finally
            {
                proc.Close();
            }
        }

        private void SearchProcess()
        {
            Console.WriteLine($"Searching for the {_parsedArguments.Name} process every {_parsedArguments.Timeout}ms");
            bool processFound = false;

            do
            {
                // Search for all the processes with the given name
                _processes = Process.GetProcessesByName(_parsedArguments.Name);
                processFound = _processes.Length != 0;

                // Wait for some time before trying again
                if (!processFound)
                    Thread.Sleep(_parsedArguments.Timeout);
            } while (!processFound);

            try
            {
                Console.WriteLine($"{_parsedArguments.Name} process found");
                Console.WriteLine($"Waiting for {_parsedArguments.Name} to exit");

                foreach (Process process in _processes)
                {
                    //Console.WriteLine(@"{0} | ID: {1}", process.ProcessName, process.Id);
                    process.WaitForExit();
                }

                Console.WriteLine($"{_parsedArguments.Name} has exited");
            }
            catch
            {
                // Nothing to do here
            }
        }
    }
}
