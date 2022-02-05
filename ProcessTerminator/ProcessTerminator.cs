using CommandLine;
using System;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Security;
using System.Threading;

namespace ProcessTerminator
{
    class ProcessTerminator
    {
        // Events
        static Win32.EventHandler _handler;

        // Fields
        private Options _parsedArguments;
        private Process[] _processes;
        private string _programName;
        private string _programArguments;

        public ProcessTerminator()
        {
            if (!ParseArguments())
                return;

            AddCloseEvent();
            RunProgram();
            SearchProcess();
            WaitForProcessExit();
            Cleanup();
        }

        private void AddCloseEvent()
        {
            // Some boilerplate to react to close window event, CTRL-C, kill, etc
            _handler += new Win32.EventHandler(CloseHandler);
            Win32.SetConsoleCtrlHandler(_handler, true);
        }

        private void Cleanup()
        {

            Console.WriteLine("Shutting down");

            CloseAllProcesses();

            // Wait some time for processes to close
            Thread.Sleep(2000);

            KillAllProcesses();
        }

        private void CloseAllProcesses()
        {
            // Try to gracefully close all processes
            foreach (var proc in _processes)
            {
                Win32.EnumWindows(delegate (IntPtr hWnd, IntPtr lParam)
                {
                    Win32.GetWindowThreadProcessId(hWnd, out uint processId);

                    if (processId == (uint)lParam)
                        Win32.SendMessage(hWnd, Win32.Constants.WM_QUIT, IntPtr.Zero, IntPtr.Zero);

                    return true;
                },
                (IntPtr)proc.Id);
            }
        }

        private bool CloseHandler(Win32.CtrlType sig)
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

        private void KillAllProcesses()
        {
            _processes = Process.GetProcessesByName(_parsedArguments.Name);

            // YOU'VE JUST BEEN ERASED https://www.youtube.com/watch?v=Q5AuCTdBJns
            foreach (Process process in _processes)
            {
                try
                {
                    if (!process.HasExited)
                        process.Kill();
                }
                catch
                {
                    // Ignore exceptions (usually due to trying to kill non-existant child processes)
                }
            }
        }

        private bool ParseArguments()
        {
            // Parse and validate arguments
            var parser = new Parser(with => { with.IgnoreUnknownArguments = true; with.HelpWriter = Console.Error; });
            var result = parser.ParseArguments<Options>(Environment.GetCommandLineArgs().Skip(1)).WithParsed<Options>(opts => _parsedArguments = opts);

            // Print an error message if the argument syntax is invalid
            if (result.Tag == ParserResultType.NotParsed)
            {
                Console.WriteLine("Usage: ProcessTerminator.exe -n <process name> [-t <milliseconds>] [-w <milliseconds>] [-p <program> [arguments]]");
                Console.WriteLine("Example usage: ProcessTerminator.exe - n Skype - t 1000 -w 2000 -p java \"MyTool - check - verify logfile.out\"");
                return false;
            }

            // Extract program information
            _programName = _parsedArguments.Program.FirstOrDefault();
            _programArguments = string.Join(" ", _parsedArguments.Program.Skip(1));

            return true;
        }

        private void RunProgram()
        {
            if (_programName != null)
            {
                try
                {
                    // Run program with given arguments
                    using (Process proc = new Process())
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
                }
                catch (SecurityException)
                {
                    Console.WriteLine($"The caller does not have the required permission.");
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
                    Console.WriteLine($"No file name was specified");
                }
                catch (Win32Exception)
                {
                    Console.WriteLine($"There was an error in opening the associated file: {_programArguments}");
                }
                catch (Exception e)
                {
                    Console.WriteLine($"Unhandled exception: {e.Message}");
                }
            }
        }

    private void SearchProcess()
        {
            Console.WriteLine($"Waiting for {_parsedArguments.Wait}ms before searching for the {_parsedArguments.Name} process");

            bool processFound = false;
            Thread.Sleep(_parsedArguments.Wait);

            Console.WriteLine($"Searching for the {_parsedArguments.Name} process every {_parsedArguments.Timeout}ms");

            do
            {
                // Search for all processes with the given name
                _processes = Process.GetProcessesByName(_parsedArguments.Name);
                processFound = _processes.Length != 0;

                // Wait for some time before trying again
                if (!processFound)
                    Thread.Sleep(_parsedArguments.Timeout);
            } while (!processFound);
        }

        private void WaitForProcessExit()
        {
            Console.WriteLine($"{_parsedArguments.Name} process found");
            Console.WriteLine($"Waiting for {_parsedArguments.Name} to exit");

            try
            {
                // Wait for all processes to exit
                foreach (Process process in _processes)
                {
                    Console.WriteLine($"{process.ProcessName} | PID: {process.Id}");
                    process.WaitForExit();
                }
            }
            catch
            {
                // Nothing to do here
            }

            Console.WriteLine($"{_parsedArguments.Name} has exited");
        }
    }
}
