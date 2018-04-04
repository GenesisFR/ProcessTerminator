# ProcessTerminator
Small program that runs a specified program and waits for a given process to terminate before closing, or terminates the given process when closing.  
It was made in C# using .NET 4.6.2 and Visual Studio 2017. You may need to install the Microsoft [.NET Framework 4.6.2](https://www.microsoft.com/en-us/download/details.aspx?id=53344) to make it work.

## Arguments

-t <milliseconds>: time in milliseconds before searching for the process again  
-n <process name>: name of the process to terminate  
-p <program> [arguments]: path to the program to run at startup, with optional arguments