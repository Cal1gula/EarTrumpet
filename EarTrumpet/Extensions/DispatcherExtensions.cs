﻿using System;
using System.Threading;
using System.Windows.Threading;

namespace EarTrumpet.Extensions
{
    public static class DispatcherExtensions
    {
        public static void SafeInvoke(this Dispatcher dispatcher, Action method)
        {
            if (dispatcher.Thread.ManagedThreadId == Thread.CurrentThread.ManagedThreadId)
            {
                method();
            }
            else
            {
                dispatcher.BeginInvoke(method);
            }
        }
    }
}