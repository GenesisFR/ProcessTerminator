# ProcessTerminator
Small program that waits for a given process to terminate before closing, or terminates the given process when closing. It can also run a specified program with optional arguments.  
It was made in C# using .NET 4.6.2, the CommandLineParser NuGet package and Visual Studio 2017. You may need to install the Microsoft [.NET Framework 4.6.2](https://www.microsoft.com/en-us/download/details.aspx?id=53344) to make it work.

## Arguments

Usage: ProcessTerminator.exe -n \<process name\> [-t \<milliseconds\>] [-p \<program\> [arguments]]

-n <process name>: mandatory, name of the process to terminate  
-t <milliseconds> (default: 5000) : optional, time in milliseconds before searching for the process again  
-p <program> [arguments]: optional, path to a program to run at startup, with optional arguments

Example:

ProcessTerminator.exe -n Skype -t 1000 -p java "MyTool -check -verify logfile.out"