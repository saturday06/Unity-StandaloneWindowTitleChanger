// This is free and unencumbered software released into the public domain.
//
// Anyone is free to copy, modify, publish, use, compile, sell, or
// distribute this software, either in source code form or as a compiled
// binary, for any purpose, commercial or non-commercial, and by any
// means.
//
// In jurisdictions that recognize copyright laws, the author or authors
// of this software dedicate any and all copyright interest in the
// software to the public domain. We make this dedication for the benefit
// of the public at large and to the detriment of our heirs and
// successors. We intend this dedication to be an overt act of
// relinquishment in perpetuity of all present and future rights to this
// software under copyright law.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
// MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT.
// IN NO EVENT SHALL THE AUTHORS BE LIABLE FOR ANY CLAIM, DAMAGES OR
// OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE,
// ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR
// OTHER DEALINGS IN THE SOFTWARE.
//
// For more information, please refer to <http://unlicense.org>

using System;
using System.Runtime.InteropServices;
using UnityEngine;

namespace StandaloneWindowTitleChanger
{
    public class StandaloneWindowTitleChangeException : Exception
    {
        public enum Error
        {
            Unknown = 0,
            NoWindow = 1,
        }

        public readonly Error Cause;

        internal StandaloneWindowTitleChangeException(Error cause, string message) : base(message)
        {
            Cause = cause;
        }
    }

#if UNITY_STANDALONE_WIN
    public static class WindowsApi // visible for testing
    {
        [DllImport("kernel32.dll")]
        public static extern uint GetCurrentProcessId();

        [DllImport("user32.dll")]
        public static extern bool EnumWindows(EnumWindowsProc enumWindowsProc, IntPtr lParam);

        [DllImportAttribute("user32.dll", SetLastError = true)]
        public static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint processId);

        [DllImportAttribute("user32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        public static extern bool SetWindowText(IntPtr hWnd, string text);

        public delegate bool EnumWindowsProc(IntPtr hWnd, IntPtr lParam);
    }

    public static class StandaloneWindowTitle
    {
        public static readonly bool IsSupported = true;

        private static bool EnumWindowsCallback(IntPtr hWnd, IntPtr parameterGCHandleIntPtr)
        {
            var parameterGCHandle = GCHandle.FromIntPtr(parameterGCHandleIntPtr);
            var parameterObject = parameterGCHandle.Target;
            if (!(parameterObject is EnumWindowsParameter))
            {
                Debug.LogException(new Exception("Sanity error: parameter is not a EnumWindowsParameter: " + parameterObject));
                return false;
            }

            var parameter = (EnumWindowsParameter) parameterObject;
            uint processId;
            WindowsApi.GetWindowThreadProcessId(hWnd, out processId);
            if (parameter.ProcessId != processId)
            {
                return true;
            }

            parameter.Found = true;

            var setWindowTextSuccess = WindowsApi.SetWindowText(hWnd, parameter.Title);
            var lastWin32Error = Marshal.GetLastWin32Error();
            if (!setWindowTextSuccess)
            {
                parameter.LastWin32Error = lastWin32Error;
            }

            return true;
        }

        private class EnumWindowsParameter
        {
            internal string Title;
            internal uint ProcessId;
            internal bool Found;
            internal int LastWin32Error;
        }

        public static void Change(string title)
        {
            var parameter = new EnumWindowsParameter
            {
                Title = title,
                ProcessId = WindowsApi.GetCurrentProcessId(),
            };

            var parameterGCHandle = GCHandle.Alloc(parameter);
            try
            {
                WindowsApi.EnumWindows(EnumWindowsCallback, GCHandle.ToIntPtr(parameterGCHandle));
            }
            finally
            {
                parameterGCHandle.Free();
            }

            if (parameter.LastWin32Error != 0)
            {
                throw new StandaloneWindowTitleChangeException(StandaloneWindowTitleChangeException.Error.Unknown,
                    "Unknown error: " + parameter.LastWin32Error);
            }

            if (!parameter.Found)
            {
                throw new StandaloneWindowTitleChangeException(StandaloneWindowTitleChangeException.Error.NoWindow,
                    "No window found");
            }
        }
    }
#elif UNITY_STANDALONE_OSX
    public static class StandaloneWindowTitle
    {
        public static readonly bool IsSupported = true;

        [DllImport ("StandaloneWindowTitleChanger", EntryPoint =
 "StandaloneWindowTitleChanger_StandaloneWindowTitle_ChangeNative")]
        private static extern int ChangeNative(string title);

        public static void Change(string title)
        {
            var result = ChangeNative(title);
            switch (result)
            {
                case 0:
                    // success
                    break;
                case 1:
                    throw new StandaloneWindowTitleChangeException(StandaloneWindowTitleChangeException.Error.NoWindow, "No window found");
                default:
                    throw new StandaloneWindowTitleChangeException(StandaloneWindowTitleChangeException.Error.Unknown, "Unknown error: " + result);
            }
        }
    }
#else
    public static class StandaloneWindowTitle
    {
        public static readonly bool IsSupported = true;

        public static void Change(string newTitle)
        {
        }
    }
#endif
}