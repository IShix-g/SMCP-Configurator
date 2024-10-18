
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Packages.SMCPConfigurator.Editor
{
    public static class TaskExtensions
    {
        public static void SafeContinueWith<T>(
            this Task<T> @this,
            Action<Task<T>> continuationAction,
            CancellationToken cancellationToken = default)
        {
            var context = SynchronizationContext.Current;
            @this.ContinueWith(t =>
            {
                if (cancellationToken.IsCancellationRequested)
                {
                    return;
                }

                if (SynchronizationContext.Current == context)
                {
                    continuationAction(t);
                }
                else
                {
                    context.Post(_ => continuationAction(t), null);
                }
            },
            cancellationToken,
            TaskContinuationOptions.ExecuteSynchronously,
            TaskScheduler.Current);
        }
        
        public static void SafeContinueWith(
            this Task @this,
            Action<Task> continuationAction,
            CancellationToken cancellationToken = default)
        {
            var context = SynchronizationContext.Current;
            @this.ContinueWith(t =>
            {
                if (cancellationToken.IsCancellationRequested)
                {
                    return;
                }

                if (SynchronizationContext.Current == context)
                {
                    continuationAction(t);
                }
                else
                {
                    context.Post(_ => continuationAction(t), null);
                }
            },
            cancellationToken,
            TaskContinuationOptions.ExecuteSynchronously,
            TaskScheduler.Current);
        }
        
        public static void SafeCancelAndDispose(this CancellationTokenSource @this)
        {
            if (@this == default)
            {
                return;
            }
            
            try
            {
                if (!@this.IsCancellationRequested)
                {
                    @this.Cancel();
                }
                @this.Dispose();
            }
            catch
            {
                // Ignore
            }
        }
    }
}