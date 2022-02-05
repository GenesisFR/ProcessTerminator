using CommandLine;
using System.Collections.Generic;

namespace ProcessTerminator
{
    class Options
    {
        // Mandatory
        [Option('n', "name", Required = true,
            HelpText = "Name of the process to terminate.")]
        public string Name { get; set; }

        // Optional, defaults to 5000ms
        [Option('t', "timeout", Required = false, Default = 5000,
          HelpText = "Time in milliseconds before searching for the process again.")]
        public int Timeout { get; set; }

        // Optional, defaults to 0ms
        [Option('w', "wait", Required = false, Default = 0,
          HelpText = "Time to wait in milliseconds before starting to search for the process.")]
        public int Wait { get; set; }

        // Optional, if specified, must have at least the program name as argument
        [Option('p', "program", Required = false, Min = 1,
            HelpText = "Path to the program to run at startup, with optional arguments.")]
        public IEnumerable<string> Program { get; set; }
    }
}
