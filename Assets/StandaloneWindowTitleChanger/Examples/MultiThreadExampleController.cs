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
using System.Collections.Generic;
using System.Linq;
using System.Globalization;
using System.Threading;
using UnityEngine;
using Stopwatch = System.Diagnostics.Stopwatch;

namespace StandaloneWindowTitleChanger.Examples
{
    internal class MultiThreadExampleController : MonoBehaviour
    {
        private const int TickInterval = 50;
        private List<Thread> _threads = new List<Thread>();
        private readonly Stopwatch _stopwatch = new Stopwatch();

        private void Start()
        {
            _threads = Enumerable.Range(1, 9).Select(i =>
            {
                var thread = new Thread(() =>
                {
                    var stopwatch = Stopwatch.StartNew();
                    try
                    {
                        while (true)
                        {
                            if (stopwatch.ElapsedMilliseconds > TickInterval)
                            {
                                tick(i);
                                stopwatch.Reset();
                                stopwatch.Start();
                            }

                            Thread.Sleep(20);
                        }
                    }
                    catch (ThreadInterruptedException)
                    {
                    }
                });
                thread.Start();
                return thread;
            }).ToList();
            _stopwatch.Start();
        }

        private void OnApplicationQuit()
        {
            foreach (var thread in _threads)
            {
                thread.Interrupt();
                thread.Join();
            }

            _threads.Clear();
        }

        private void Update()
        {
            if (_stopwatch.ElapsedMilliseconds > TickInterval)
            {
                tick(0);
                _stopwatch.Reset();
                _stopwatch.Start();
            }
        }

        private static void tick(int id)
        {
            try
            {
                StandaloneWindowTitle.Change("[" + id + "] " + DateTime.Now.ToString(CultureInfo.CurrentCulture));
            }
            catch (StandaloneWindowTitleChangeException e)
            {
                switch (e.Cause)
                {
                    case StandaloneWindowTitleChangeException.Error.NoWindow:
                        return;
                    case StandaloneWindowTitleChangeException.Error.Unknown:
                        Debug.LogException(e);
                        break;
                    default:
                        Debug.LogException(e);
                        break;
                }
            }
        }
    }
}