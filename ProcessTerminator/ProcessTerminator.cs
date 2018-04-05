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
        private string[] _arguments;
        private Process[] _processes;
        private string _processName;
        private string _programName;
        private string _programArguments;
        private int _timeout;

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

            // Validate syntax of arguments
            if (!ValidateArguments())
            {
                Console.WriteLine("Usage: ProcessTerminator.exe -t <milliseconds> -n <process name> -p <program> [arguments]");
                return;
            }

            Initialize();
            AddCloseEvent();
            RunProgram();
            SearchProcess();
            Cleanup();
        }

        private void AddCloseEvent()
        {
            // Some boilerplate to react to close window event, CTRL-C, kill, etc
            _handler += new EventHandler(Handler);
            SetConsoleCtrlHandler(_handler, true);
        }

        private void Cleanup()
        {
            Console.WriteLine("Shutting down");

            foreach (Process process in _processes)
            {
                if (!process.HasExited)
                    process.Kill();
            }
        }

        [DllImport("Kernel32")]
        private static extern bool SetConsoleCtrlHandler(EventHandler handler, bool add);
        private bool Handler(CtrlType sig)
        {
            try
            {
                // Kill the program
                Cleanup();

                // Shutdown right away so there are no lingering threads
                Environment.Exit(-1);
            }
            catch (Exception)
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
            _programName = _arguments[5];
            _programArguments = _arguments.Length > 6 ? _arguments[6] : "";
        }

        private void RunProgram()
        {
            Process proc = new Process();

            try
            {
                if (_programArguments == "")
                    Console.WriteLine("Running {0}", _programName);
                else
                    Console.WriteLine("Running {0} with the following arguments: {1}", _programName, _programArguments);

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
                Console.WriteLine("The file name is empty, contains only white spaces, or contains invalid characters: {0}", _programArguments);
            }
            catch (UnauthorizedAccessException)
            {
                Console.WriteLine("Access to fileName is denied: {0}", _programArguments);
            }
            catch (PathTooLongException)
            {
                Console.WriteLine("The specified path, file name, or both exceed the system-defined maximum length: {0}", _programArguments);
            }
            catch (NotSupportedException)
            {
                Console.WriteLine("File name contains a colon(:) in the middle of the string: {0}", _programArguments);
            }
            catch (InvalidOperationException)
            {
                Console.WriteLine("No file name was specified");
            }
            catch (Win32Exception)
            {
                Console.WriteLine("There was an error in opening the associated file: {0}", _programArguments);
            }
            catch (Exception e)
            {
                Console.WriteLine("Unhandled exception: {0}", e.Message);
            }
            finally
            {
                proc.Close();
            }
        }

        private void SearchProcess()
        {
            Console.WriteLine("Searching for the {0} process", _processName);
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
                Console.WriteLine("{0} process found", _processName);
                Console.WriteLine("Waiting for {0} to exit", _processName);

                foreach (Process process in _processes)
                {
                    //Console.WriteLine(@"{0} | ID: {1}", process.ProcessName, process.Id);
                    process.WaitForExit();
                }

                Console.WriteLine("{0} has exited", _processName);
            }
            catch (Exception)
            {
                // Nothing to do here
            }
        }

        private bool ValidateArguments()
        {
            if (_arguments.Length < 6)
                return false;

            if (_arguments[0] != "-t" && _arguments[2] != "-n" && _arguments[4] != "-p")
                return false;

            return true;
        }
    }
}
