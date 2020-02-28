namespace Cloudtoid
{
    using System;
    using System.Diagnostics.CodeAnalysis;
    using System.Runtime.ExceptionServices;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.Extensions.Logging;
    using static Contract;

    [SuppressMessage("Microsoft.VisualStudio.Threading.Analyzers", "VSTHRD200", Justification = "Reviewed.")]
    [SuppressMessage("Microsoft.VisualStudio.Threading.Analyzers", "VSTHRD105", Justification = "Reviewed.")]
    public static class ConcurrencyExtensions
    {
        public static async Task<TResult> WithTimeout<TResult>(
            this Func<CancellationToken, Task<TResult>> funcAsync,
            TimeSpan timeout)
        {
            CheckValue(funcAsync, nameof(funcAsync));

            using (var cancellationSource = new CancellationTokenSource())
            {
                cancellationSource.CancelAfter(timeout);
                var task = await funcAsync(cancellationSource.Token);
                cancellationSource.Token.ThrowIfCancellationRequested();
                return task;
            }
        }

        public static async Task<TResult> WithTimeout<TInput, TResult>(
            this Func<TInput, CancellationToken, Task<TResult>> funcAsync,
            TInput input,
            TimeSpan timeout,
            CancellationToken externalToken)
        {
            CheckValue(funcAsync, nameof(funcAsync));

            using (var cts = new CancellationTokenSource(timeout))
            using (var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(cts.Token, externalToken))
            {
                var task = await funcAsync(input, linkedCts.Token);
                linkedCts.Token.ThrowIfCancellationRequested();
                return task;
            }
        }

        public static Task TraceOnFaulted<TLoggerCategoryName>(
            this Task task,
            ILogger<TLoggerCategoryName> logger,
            string message,
            CancellationToken cancellationToken)
        {
            CheckValue(task, nameof(task));
            CheckValue(logger, nameof(logger));

            return task.ContinueWith(
                t =>
                {
                    if (task.IsCanceled)
                    {
                        var tce = new TaskCanceledException(message, t.Exception);
                        logger.LogError(tce, message);
                        throw tce;
                    }

                    if (cancellationToken.IsCancellationRequested)
                    {
                        var oce = new OperationCanceledException(message, t.Exception, cancellationToken);
                        logger.LogError(oce, message);
                        throw oce;
                    }

                    var ex = t.Exception;
                    if (ex is null)
                        return;

                    logger.LogError(ex, message);
                    ExceptionDispatchInfo.Capture(ex.GetBaseException()).Throw();
                    throw new NotSupportedException("Should never get to this line!");
                });
        }

        public static Task<TResult> TraceOnFaulted<TLoggerCategoryName, TResult>(
            this Task<TResult> task,
            ILogger<TLoggerCategoryName> logger,
            string message,
            CancellationToken cancellationToken)
        {
            CheckValue(task, nameof(task));
            CheckValue(logger, nameof(logger));

            return task.ContinueWith(
                t =>
                {
                    if (task.IsCanceled)
                    {
                        var tce = new TaskCanceledException(message, t.Exception);
                        logger.LogError(tce, message);
                        throw tce;
                    }

                    if (cancellationToken.IsCancellationRequested)
                    {
                        var oce = new OperationCanceledException(message, t.Exception, cancellationToken);
                        logger.LogError(oce, message);
                        throw oce;
                    }

                    var ex = t.Exception;
                    if (ex is null)
                        return t.Result;

                    logger.LogError(ex, message);
                    ExceptionDispatchInfo.Capture(ex.GetBaseException()).Throw();
                    throw new NotSupportedException("Should never get to this line!");
                });
        }

        public static void FireAndForget<TLoggerCategoryName>(
            this Task task,
            ILogger<TLoggerCategoryName> logger,
            string faultedMessage,
            CancellationToken cancellationToken)
        {
            CheckValue(task, nameof(task));
            CheckValue(logger, nameof(logger));
            _ = task.TraceOnFaulted(logger, faultedMessage, cancellationToken);
        }

        // It creates a task that is completed if the cancellationToken is cancelled.
        public static Task WhenCancelled(this CancellationToken cancellationToken)
        {
            var tcs = new TaskCompletionSource<bool>();
            cancellationToken.Register(s => ((TaskCompletionSource<bool>)s!).SetResult(true), tcs);
            return tcs.Task;
        }
    }
}
