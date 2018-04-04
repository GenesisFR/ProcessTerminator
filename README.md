# ProcessTerminator
Small program that runs a specified program and waits for a given process to terminate before closing, or terminates the given process when closing.

##Arguments

-t <milliseconds>: time in milliseconds before searching for the process again
-n <process name>: name of the process to terminate
-p <program> [arguments]: path to the program to run at startup, with optional arguments