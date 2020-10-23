using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;

namespace Atoll.UtilsBundle.Helpers
{
    public static class DisposeExtensions
    {
        public static void SafeCancel(this CancellationTokenSource source)
        {
            if (ReferenceEquals(source, null)) return;

            try
            {
                source.Cancel();
            }
            catch
            {
            }
        }

        public static void SafeDispose(this IDisposable disposable)
        {
            if (ReferenceEquals(disposable, null)) return;

            try
            {
                disposable.Dispose();
            }
            catch
            {
                // ignored
            }
        }
    }
}
