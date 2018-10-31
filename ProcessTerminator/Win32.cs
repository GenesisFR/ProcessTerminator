using System;
using System.Runtime.InteropServices;

namespace ProcessTerminator
{
    class Win32
    {
        [DllImport("Kernel32.dll")]
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
            internal const int WM_QUIT = 0x12;
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
}
