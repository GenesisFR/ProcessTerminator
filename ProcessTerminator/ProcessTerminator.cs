using System;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Security;
using System.Threading;

namespace ProcessTerminator
{
    class ProcessTerminator
    {
        int DEFAULT_TIMEOUT = 5000;

        // Events
        private delegate bool EventHandler(CtrlType sig);
        static EventHandler _handler;

        // Fields
        string[] _arguments;
        string _processName;
        Process[] _processes;
        int _timeout;

        enum CtrlType
        {
            CTRL_C_EVENT = 0,
            CTRL_BREAK_EVENT = 1,
            CTRL_CLOSE_EVENT = 2,
            CTRL_LOGOFF_EVENT = 5,
            CTRL_SHUTDOWN_EVENT = 6
        }

        public ProcessTerminator(string[] args)
        {
            _arguments = args;

            // Check syntax of arguments
            if (!CheckArguments())
            {
                Console.WriteLine("Usage: ProcessTerminator.exe -t <milliseconds> -n <process name> -p <program> \"[arguments]\"");
                return;
            }

            Initialize();
            RunProgram();

            bool processFound = false;

            do
            {
                // Search for all the processes with the given name
                _processes = Process.GetProcessesByName(_processName);
                processFound = _processes.Length != 0;

                // Wait for some time before trying again
                if (!processFound)
                    Thread.Sleep(_timeout);
            } while (!processFound);

            try
            {
                Console.WriteLine("Waiting for {0} to exit", _processName);

                foreach (Process process in _processes)
                {
                    //Console.WriteLine(@"{0} | ID: {1}", process.ProcessName, process.Id);
                    process.WaitForExit();
                }

                Console.WriteLine("{0} has exited, putting the minigun back in the van and going back to HQ", _processName);
            }
            catch (Exception e)
            {
                // Nothing to do here
            }

            Cleanup();
        }

        private void AddCloseEvent()
        {
            // Some boilerplate to react to close window event, CTRL-C, kill, etc
            _handler += new EventHandler(Handler);
            SetConsoleCtrlHandler(_handler, true);
        }

        private bool CheckArguments()
        {
            if (_arguments.Length < 6)
                return false;

            if (_arguments[0] != "-t" && _arguments[2] != "-n" && _arguments[4] != "-p")
                return false;

            return true;
        }

        private void Cleanup()
        {
            foreach (Process process in _processes)
                process.Close();
        }

        [DllImport("Kernel32")]
        private static extern bool SetConsoleCtrlHandler(EventHandler handler, bool add);
        private bool Handler(CtrlType sig)
        {
            Console.WriteLine("{0}, you've just been erased :)", _processName);

            try
            {
                // Kill the program
                foreach (Process process in _processes)
                {
                    if (!process.HasExited)
                        process.Kill();
                }

                // Shutdown right away so there are no lingering threads
                Environment.Exit(-1);
            }
            catch (Exception e)
            {
                // Nothing to do here
            }

            return true;
        }

        private void Initialize()
        {
            int.TryParse(_arguments[1], out _timeout);

            if (_timeout <= 0)
                _timeout = DEFAULT_TIMEOUT;

            _processName = _arguments[3];
            AddCloseEvent();
        }

        private void RunProgram()
        {
            Process proc = new Process();

            try
            {
                proc.StartInfo.Arguments = _arguments.Length > 6 ? _arguments[6] : "";
                proc.StartInfo.FileName = _arguments[5];
                proc.StartInfo.WorkingDirectory = new FileInfo(_arguments[5]).DirectoryName;
                proc.Start();
            }
            catch (SecurityException e)
            {
                Console.WriteLine("The caller does not have the required permission.");
            }
            catch (ArgumentException e)
            {
                Console.WriteLine("The file name is empty, contains only white spaces, or contains invalid characters: {0}", _arguments[5]);
            }
            catch (UnauthorizedAccessException e)
            {
                Console.WriteLine("Access to fileName is denied: {0}", _arguments[5]);
            }
            catch (PathTooLongException e)
            {
                Console.WriteLine("The specified path, file name, or both exceed the system-defined maximum length: {0}", _arguments[5]);
            }
            catch (NotSupportedException e)
            {
                Console.WriteLine("File name contains a colon(:) in the middle of the string: {0}", _arguments[5]);
            }
            catch (InvalidOperationException e)
            {
                Console.WriteLine("No file name was specified");
            }
            catch (Win32Exception e)
            {
                Console.WriteLine("There was an error in opening the associated file: {0}", _arguments[5]);
            }
            catch (Exception e)
            {
                Console.WriteLine("Unhandled exception");
            }
            finally
            {
                proc.Close();
            }
        }
    }
}
