﻿using Microsoft.ServiceFabric.Data;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace SoCreate.ServiceFabric.DistributedCache
{
    static class RetryHelper
    {
        private const int DefaultMaxAttempts = 10;
        private static readonly TimeSpan InitialDelay = TimeSpan.FromMilliseconds(200);
        private static readonly TimeSpan MinimumDelay = TimeSpan.FromMilliseconds(200);

        public static async Task<TResult> ExecuteWithRetry<TResult>(
            IReliableStateManager stateManager,
            Func<ITransaction, CancellationToken, object, Task<TResult>> operation,
            object state = null,
            CancellationToken cancellationToken = default(CancellationToken),
            int maxAttempts = DefaultMaxAttempts,
            TimeSpan? initialDelay = null)
        {
            if (stateManager == null) throw new ArgumentNullException(nameof(stateManager));
            if (operation == null) throw new ArgumentNullException(nameof(operation));
            if (maxAttempts <= 0) maxAttempts = DefaultMaxAttempts;
            if (initialDelay == null || initialDelay.Value < MinimumDelay)
                initialDelay = InitialDelay;

            Func<CancellationToken, object, Task<TResult>> wrapped = async (token, st) =>
            {
                TResult result;
                using (var tran = stateManager.CreateTransaction())
                {
                    try
                    {
                        result = await operation(tran, cancellationToken, state);
                        await tran.CommitAsync();
                    }
                    catch (TimeoutException)
                    {
                        tran.Abort();
                        throw;
                    }
                }
                return result;
            };

            var outerResult = await ExecuteWithRetry(wrapped, state, cancellationToken, maxAttempts, initialDelay);
            return outerResult;
        }

        public static async Task ExecuteWithRetry(
            IReliableStateManager stateManager, 
            Func<ITransaction, CancellationToken, object, Task> operation,
            object state = null,
            CancellationToken cancellationToken = default(CancellationToken),
            int maxAttempts = DefaultMaxAttempts,
            TimeSpan? initialDelay = null)
        {
            if (stateManager == null) throw new ArgumentNullException(nameof(stateManager));
            if (operation == null) throw new ArgumentNullException(nameof(operation));
            if (maxAttempts <= 0) maxAttempts = DefaultMaxAttempts;
            if (initialDelay == null || initialDelay.Value < MinimumDelay)
                initialDelay = InitialDelay;

            Func<CancellationToken, object, Task<object>> wrapped = async (token, st) =>
            {
                using (var tran = stateManager.CreateTransaction())
                {
                    try
                    {
                        await operation(tran, cancellationToken, state);
                        await tran.CommitAsync();
                    }
                    catch (TimeoutException)
                    {
                        tran.Abort();
                        throw;
                    }
                }
                return null;
            };

            await ExecuteWithRetry(wrapped, state, cancellationToken, maxAttempts, initialDelay);
        }

        public static async Task<TResult> ExecuteWithRetry<TResult>(
            Func<CancellationToken, object, Task<TResult>> operation,
            object state = null,
            CancellationToken cancellationToken = default(CancellationToken),
            int maxAttempts = DefaultMaxAttempts,
            TimeSpan? initialDelay = null)
        {
            if (operation == null) throw new ArgumentNullException(nameof(operation));
            if (maxAttempts <= 0) maxAttempts = DefaultMaxAttempts;
            if (initialDelay == null || initialDelay.Value < MinimumDelay)
                initialDelay = InitialDelay;

            var result = default(TResult);
            for (int attempts = 0; attempts < maxAttempts; attempts++)
            {
                try
                {
                    result = await operation(cancellationToken, state);
                    break;
                }
                catch (TimeoutException)
                {
                    if (attempts == DefaultMaxAttempts)
                    {
                        throw;
                    }
                }

                //exponential back-off
                int factor = (int)Math.Pow(2, attempts) + 1;
                int delay = new Random(Guid.NewGuid().GetHashCode()).Next((int)(initialDelay.Value.TotalMilliseconds * 0.5D), (int)(initialDelay.Value.TotalMilliseconds * 1.5D));
                await Task.Delay(factor * delay, cancellationToken);
            }
            return result;
        }
    }
}
