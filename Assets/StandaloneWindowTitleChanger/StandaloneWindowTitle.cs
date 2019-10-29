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
using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Text;
using AOT;
using UnityEngine;

#if UNITY_EDITOR
using NativeStandaloneWindowTitle = StandaloneWindowTitleChanger.UnsupportedStandaloneWindowTitle;
#elif UNITY_STANDALONE_WIN
using NativeStandaloneWindowTitle = StandaloneWindowTitleChanger.WindowsStandaloneWindowTitle;
#elif UNITY_STANDALONE_OSX
using NativeStandaloneWindowTitle = StandaloneWindowTitleChanger.MacOSStandaloneWindowTitle;
#else
using NativeStandaloneWindowTitle = StandaloneWindowTitleChanger.UnsupportedStandaloneWindowTitle;
#endif

namespace StandaloneWindowTitleChanger
{
    /// <summary>
    /// Exception.
    /// </summary>
    public class StandaloneWindowTitleChangeException : Exception
    {
        public enum Error
        {
            Unknown = 0,
            NoWindow = 1,
            NotSupported = 2,
        }

        public readonly Error Cause;

        internal StandaloneWindowTitleChangeException(Error cause, string message) : base(message)
        {
            Cause = cause;
        }

        internal StandaloneWindowTitleChangeException(Error cause, string message, Exception innerException) : base(message, innerException)
        {
            Cause = cause;
        }
    }

    /// <summary>
    /// Window title.
    /// </summary>
    public static class StandaloneWindowTitle
    {
        /// <summary>
        /// true if running platform is supported.
        /// </summary>
        public static readonly bool IsSupported = NativeStandaloneWindowTitle.IsSupported;

        /// <summary>
        /// Change window title.
        /// </summary>
        /// <param name="newTitle"></param>
        /// <exception cref="StandaloneWindowTitleChanger.StandaloneWindowTitleChangeException">On error</exception>
        public static void Change(string newTitle)
        {
            NativeStandaloneWindowTitle.Change(newTitle);
        }
    }

    /// <summary>
    /// Windows API wrappers. Visible for testing.
    /// </summary>
    public static class WindowsApi
    {
        [DllImport("kernel32.dll")]
        public static extern uint GetCurrentProcessId();

        [DllImportAttribute("user32.dll", SetLastError = true)]
        public static extern bool EnumWindows(EnumWindowsProc enumWindowsProc, IntPtr lParam);

        [DllImportAttribute("user32.dll", SetLastError = true)]
        public static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint processId);

        [DllImportAttribute("user32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        public static extern bool SetWindowText(IntPtr hWnd, string text);

        [DllImportAttribute("user32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        public static extern int GetClassName(IntPtr hWnd, StringBuilder lpClassName, int nMaxCount);

        public delegate bool EnumWindowsProc(IntPtr hWnd, IntPtr lParam);
    }

    /// <summary>
    /// Native implementation. Visible for testing.
    /// </summary>
    public static class WindowsStandaloneWindowTitle
    {
        internal static readonly bool IsSupported = true;

#if UNITY_2017 || UNITY_2018 || UNITY_2019_1 || UNITY_2019_2
        /// <summary>
        /// Windows class name for main window handle. Visible for testing.
        /// </summary>
        public const string TargetWindowClassName = "UnityWndClass";
#else
#error Please check your unity player's class name
#endif

        [MonoPInvokeCallback(typeof(WindowsApi.EnumWindowsProc))]
        private static bool EnumWindowsCallback(IntPtr hWnd, IntPtr parameterGCHandleIntPtr)
        {
            var parameterGCHandle = GCHandle.FromIntPtr(parameterGCHandleIntPtr);
            var parameterObject = parameterGCHandle.Target;
            if (!(parameterObject is EnumWindowsParameter))
            {
                var parameterTypeName =
                    parameterObject == null ? "null" : parameterObject.GetType().AssemblyQualifiedName;
                Debug.LogException(new Exception("Fatal sanity error: parameter is not a EnumWindowsParameter but " + parameterTypeName + ": " + parameterObject));
                return false;
            }

            var parameter = (EnumWindowsParameter) parameterObject;
            uint processId;
            WindowsApi.GetWindowThreadProcessId(hWnd, out processId);
            if (parameter.ProcessId != processId)
            {
                return true;
            }

            var className = new StringBuilder(4096);
            var classNameLength = WindowsApi.GetClassName(hWnd, className, className.Capacity);
            var getClassNameError = Marshal.GetLastWin32Error();
            if (classNameLength == 0)
            {
                Debug.LogException(new Exception("Failed to get window class name for HWND:" + hWnd, new Win32Exception(getClassNameError)));
                return true;
            }

            if (className.ToString() != TargetWindowClassName)
            {
                return true;
            }

            parameter.Found = true;

            var setWindowTextSuccess = WindowsApi.SetWindowText(hWnd, parameter.Title);
            var setWindowTextError = Marshal.GetLastWin32Error();
            if (!setWindowTextSuccess)
            {
                parameter.LastWin32Error = setWindowTextError;
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

        internal static void Change(string title)
        {
            var parameter = new EnumWindowsParameter
            {
                Title = title,
                ProcessId = WindowsApi.GetCurrentProcessId(),
            };

            var parameterGCHandle = GCHandle.Alloc(parameter);
            bool enumWindowsResult;
            int enumWindowsError;
            try
            {
                enumWindowsResult = WindowsApi.EnumWindows(EnumWindowsCallback, GCHandle.ToIntPtr(parameterGCHandle));
                enumWindowsError = Marshal.GetLastWin32Error();
            }
            finally
            {
                parameterGCHandle.Free();
            }

            if (!enumWindowsResult)
            {
                throw new StandaloneWindowTitleChangeException(StandaloneWindowTitleChangeException.Error.Unknown,
                    "Failed to enumerate windows", new Win32Exception(enumWindowsError));
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

    internal static class MacOSStandaloneWindowTitle
    {
        internal static readonly bool IsSupported = true;

        [DllImport ("StandaloneWindowTitleChanger", EntryPoint =
 "StandaloneWindowTitleChanger_MacOSStandaloneWindowTitle_ChangeNative")]
        private static extern int ChangeNative(string title);

        internal static void Change(string title)
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

    internal static class UnsupportedStandaloneWindowTitle
    {
        internal static readonly bool IsSupported = false;

        internal static void Change(string newTitle)
        {
            throw new StandaloneWindowTitleChangeException(StandaloneWindowTitleChangeException.Error.NotSupported, "Not supported");
        }
    }
}
