﻿// This is free and unencumbered software released into the public domain.
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
using UnityEngine;
using UnityEngine.TestTools;
using NUnit.Framework;
using System.Collections;
using System.Globalization;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using AOT;

#if UNITY_EDITOR
using Native = StandaloneWindowTitleChanger.Tests.StandaloneWindowTitleTests.UnsupportedStandaloneWindowTitleTests;
#elif UNITY_STANDALONE_WIN
using Native = StandaloneWindowTitleChanger.Tests.StandaloneWindowTitleTests.WindowsStandaloneWindowTitleTests;
#elif UNITY_STANDALONE_OSX
using Native = StandaloneWindowTitleChanger.Tests.StandaloneWindowTitleTests.MacOSStandaloneWindowTitleTests;
#else
using Native = StandaloneWindowTitleChanger.Tests.StandaloneWindowTitleTests.UnsupportedStandaloneWindowTitleTests;
#endif

namespace StandaloneWindowTitleChanger.Tests
{
    public class StandaloneWindowTitleTests
    {
        internal static class WindowsStandaloneWindowTitleTests
        {
            internal static readonly bool Supported = true;

            private static class TestWindowsApi
            {
                [DllImport("user32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
                internal static extern int GetWindowText(IntPtr hWnd, StringBuilder lpString, int nMaxCount);
            }

            private class EnumWindowsParameter
            {
                internal System.Collections.Generic.List<string> Titles;
                internal uint ProcessId;
                internal bool Found;
                internal int LastWin32Error;
            }

            [MonoPInvokeCallback(typeof(WindowsApi.EnumWindowsProc))]
            private static bool EnumWindowsCallback(IntPtr hWnd, IntPtr parameterGCHandleIntPtr)
            {
                var parameterGCHandle = GCHandle.FromIntPtr(parameterGCHandleIntPtr);
                var parameterObject = parameterGCHandle.Target;
                if (!(parameterObject is EnumWindowsParameter))
                {
                    Debug.LogException(new Exception("Sanity error: parameter is not a EnumWindowsParameter: " +
                                                     parameterObject));
                    return false;
                }

                var parameter = (EnumWindowsParameter) parameterObject;
                uint processId;
                WindowsApi.GetWindowThreadProcessId(hWnd, out processId);
                if (parameter.ProcessId != processId)
                {
                    return true;
                }

                var className = new StringBuilder(5000);
                var classNameLength = WindowsApi.GetClassName(hWnd, className, className.Capacity);
                var getClassNameError = Marshal.GetLastWin32Error();
                if (classNameLength == 0)
                {
                    parameter.LastWin32Error = getClassNameError;
                    return true;
                }

                if (className.ToString() != WindowsStandaloneWindowTitle.TargetWindowClassName)
                {
                    return true;
                }

                parameter.Found = true;
                var stringBuilder = new StringBuilder(4096);
                var getWindowTextResult = TestWindowsApi.GetWindowText(hWnd, stringBuilder, stringBuilder.Capacity);
                var lastWin32Error = Marshal.GetLastWin32Error();
                if (getWindowTextResult == 0)
                {
                    parameter.LastWin32Error = lastWin32Error;
                    stringBuilder.Append("ERROR");
                }

                parameter.Titles.Add(stringBuilder.ToString());
                return true;
            }

            internal static System.Collections.Generic.List<string> ReadStandaloneWindowTitles()
            {
                var parameter = new EnumWindowsParameter
                {
                    Titles = new System.Collections.Generic.List<string>(),
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
                    throw new Exception("Unknown error: " + parameter.LastWin32Error);
                }

                if (!parameter.Found)
                {
                    throw new Exception("No window found");
                }

                return parameter.Titles;
            }
        }

        internal static class MacOSStandaloneWindowTitleTests
        {
            internal static readonly bool Supported = true;

            [DllImport("StandaloneWindowTitleChangerTests", EntryPoint =
                "StandaloneWindowTitleChanger_Tests_MacOSStandaloneWindowTitleTests_ReadNative")]
            private static extern int ReadNative(StringBuilder title, int titleCapacity);

            internal static System.Collections.Generic.List<string> ReadStandaloneWindowTitles()
            {
                var stringBuilder = new StringBuilder(4096);
                var result = ReadNative(stringBuilder, stringBuilder.Capacity);
                if (result != 0)
                {
                    throw new Exception("result=" + result);
                }

                return stringBuilder.ToString().Split('\n').ToList();
            }
        }

        internal static class UnsupportedStandaloneWindowTitleTests
        {
            internal static readonly bool Supported = false;

            internal static System.Collections.Generic.List<string> ReadStandaloneWindowTitles()
            {
                throw new Exception("Not supported");
            }
        }

        [Test]
        public void ThrowExceptionIfUnsupported()
        {
            if (Native.Supported)
            {
                return;
            }
            var e = Assert.Throws<StandaloneWindowTitleChangeException>(() =>
            {
                StandaloneWindowTitle.Change("test");
            });
            Assert.That(e.Cause, Is.EqualTo(StandaloneWindowTitleChangeException.Error.NotSupported));
        }

        [Test]
        public void MainThreadCanChangeTitle()
        {
            if (!Native.Supported)
            {
                return;
            }
            var input = DateTime.Now.ToString(CultureInfo.CurrentCulture);
            StandaloneWindowTitle.Change(input);
            var outputs = Native.ReadStandaloneWindowTitles();
            Assert.Positive(outputs.Count);
            foreach (var output in outputs)
            {
                Assert.AreEqual(input, output);
            }
        }

        [UnityTest]
        public IEnumerator SubThreadCanChangeTitle()
        {
            if (!Native.Supported)
            {
                yield break;
            }

            var input = DateTime.Now.ToString(CultureInfo.CurrentCulture);
            Exception exception = null;
            var thread = new Thread(() =>
            {
                try
                {
                    StandaloneWindowTitle.Change(input);
                }
                catch (Exception threadException)
                {
                    exception = threadException;
                }
            });
            thread.Start();
            yield return new WaitWhile(() => thread.IsAlive);
            Assert.Null(exception);
            var outputs = Native.ReadStandaloneWindowTitles();
            Assert.Positive(outputs.Count);
            foreach (var output in outputs)
            {
                Assert.AreEqual(input, output);
            }
        }
    }
}
