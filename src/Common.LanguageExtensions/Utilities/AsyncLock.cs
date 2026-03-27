using System.Collections.Concurrent;

namespace Common.LanguageExtensions.Utilities;

public class AsyncLock
{
    private readonly LockManager _lockManager = new();

    public async Task Lock(
       object key,
       Func<Task> func)
    {
        using (await AcquireAsyncLock(key))
        {
            await func();
        }
    }

    public async Task<T> Lock<T>(
        object key,
        Func<Task<T>> func)
    {
        using (await AcquireAsyncLock(key))
        {
            return await func();
        }
    }

    public async Task<IDisposable> AcquireAsyncLock(object key)
    {
        var wrapper = await _lockManager.GetLock(key);

        return new Releaser(() =>
        {
            _lockManager.ReleaseLock(key, wrapper);
        });
    }

    /// <summary>
    /// Manages the lifecycle of semaphores associated with lock keys.
    /// </summary>
    private sealed class LockManager
    {
        private readonly ConcurrentDictionary<object, SemaphoreWrapper> _locks = [];

        /// <summary>
        /// Gets or creates a semaphore wrapper for the specified key and increments its reference count.
        /// </summary>
        public async Task<SemaphoreWrapper> GetLock(object key)
        {
            while (true)
            {
                var wrapper = _locks.GetOrAdd(key, _ => new SemaphoreWrapper());

                // Try to increment the reference count; retry if the wrapper is being disposed
                if (await wrapper.TryPushReference())
                {
                    await wrapper.Semaphore.WaitAsync();
                    return wrapper;
                }
            }
        }

        /// <summary>
        /// Releases the semaphore and decrements the reference count. 
        /// If the reference count reaches zero, removes and disposes the wrapper.
        /// </summary>
        public void ReleaseLock(object key, SemaphoreWrapper wrapper)
        {
            wrapper.Semaphore.Release();

            // Clean up the semaphore if no one is using it
            if (wrapper.PopReference() == 0
                && _locks.TryRemove(KeyValuePair.Create(key, wrapper)))
            {
                wrapper.Semaphore.Dispose();
            }
        }
    }

    /// <summary>
    /// Wraps a semaphore with lock-free reference counting to enable safe concurrent access and disposal.
    /// 
    /// Reference Count States:
    /// - Positive (> 0): Number of active references to this wrapper
    /// - Zero (0): Transitional state - no active references, attempting to dispose
    /// - Negative (-1): Disposed state - wrapper is being cleaned up, no new references allowed
    /// 
    /// This design prevents race conditions where:
    /// 1. Thread A is about to dispose the wrapper (refCount hits 0)
    /// 2. Thread B tries to acquire a reference to the same key
    /// 3. Thread B might get a disposed semaphore
    /// 
    /// The lock-free algorithm ensures that once disposal starts (refCount transitions to -1),
    /// no new references can be added, and GetLock will retry with a new wrapper instance.
    /// </summary>
    private sealed class SemaphoreWrapper
    {
        public SemaphoreSlim Semaphore { get; } = new(1, 1);

        // Reference count tracking the number of threads using this wrapper
        // Uses atomic operations to ensure thread-safety without locks
        private int _refCount;

        /// <summary>
        /// Attempts to increment the reference count atomically using Compare-And-Swap (CAS).
        /// Returns false if the wrapper is being disposed (refCount is -1).
        /// 
        /// This method implements optimistic concurrency control:
        /// 1. Read the current reference count
        /// 2. Check if it's valid (not negative/disposed)
        /// 3. Try to atomically increment it
        /// 4. If another thread modified it between steps 1-3, retry
        /// </summary>
        public async Task<bool> TryPushReference()
        {
            return await RetryUntilResult(async () =>
            {
                // Read the current reference count with volatile semantics to ensure
                // we see the most recent value written by any thread
                var current = Volatile.Read(ref _refCount);

                // If refCount is -1, the wrapper is being disposed
                // Return false to signal the caller to get a new wrapper
                if (current < 0)
                {
                    return false;
                }

                // Attempt to atomically increment the reference count from 'current' to 'current + 1'
                // CompareExchange returns the ACTUAL value that was in _refCount:
                // - If it equals 'current', our increment succeeded
                // - If it doesn't equal 'current', another thread modified it; we need to retry
                if (Interlocked.CompareExchange(ref _refCount, current + 1, current) == current)
                {
                    // Success! We incremented the reference count
                    return true;
                }

                // CAS failed - another thread modified _refCount between our read and write
                // Return null to signal retry is needed
                return null;
            });
        }

        /// <summary>
        /// Decrements the reference count atomically.
        /// When reaching zero, attempts to transition to -1 (disposed state) to prevent new references.
        /// 
        /// This two-phase approach (0 -> -1 transition) is critical:
        /// - Phase 1: Decrement to 0 (might be temporary if another thread increments)
        /// - Phase 2: Try to lock it at -1 (disposed state)
        /// 
        /// If the CAS to -1 fails, it means another thread added a reference after we decremented,
        /// so we return the actual count instead of 0.
        /// </summary>
        /// <returns>
        /// The reference count after decrement:
        /// - 0: This thread successfully marked the wrapper as disposed
        /// - Positive: Other threads still have references
        /// - If CAS failed at zero: Returns the actual count (another thread incremented)
        /// </returns>
        public int PopReference()
        {
            // Atomically decrement the reference count
            // This operation is guaranteed to succeed (no CAS loop needed)
            var count = Interlocked.Decrement(ref _refCount);

            // Special handling when count reaches zero
            // We need to transition to -1 to mark as disposed
            if (count == 0)
            {
                // Try to atomically change from 0 to -1
                // This prevents any future TryPushReference from succeeding
                var previousValue = Interlocked.CompareExchange(ref _refCount, -1, 0);

                // If CAS failed, another thread must have called TryPushReference
                // between our Decrement and this CompareExchange
                if (previousValue != 0)
                {
                    // Return the actual current value instead of 0
                    // This tells the caller that disposal should NOT happen yet
                    return previousValue;
                }

                // CAS succeeded: we transitioned from 0 to -1
                // Return 0 to signal the caller should dispose the semaphore
            }

            return count;
        }

        /// <summary>
        /// Retries a task until it returns a non-null result, using progressive backoff strategy.
        /// 
        /// Backoff Strategy:
        /// - Retries 0-9: Use Task.Yield() for quick retries (just reschedule on thread pool)
        /// - Retries 10+: Use exponential backoff with Task.Delay() to reduce CPU usage
        ///   - Retry 10: 1ms delay
        ///   - Retry 11: 2ms delay
        ///   - Retry 12: 4ms delay
        ///   - ...up to maxDelayMs (100ms)
        /// 
        /// This approach balances responsiveness (quick retries for short contention)
        /// with efficiency (longer delays for sustained contention).
        /// </summary>
        /// <param name="task">A function that returns true (success), false (definitive failure), or null (retry needed).</param>
        /// <returns>The boolean result when the task returns a non-null value.</returns>
        private static async Task<bool> RetryUntilResult(Func<Task<bool?>> task)
        {
            var retryCount = 0;
            const int yieldThreshold = 10;  // Number of fast retries before switching to delays
            const int maxDelayMs = 100;     // Cap on exponential backoff to prevent excessive waiting

            while (true)
            {
                // Execute the task and check if it returned a definitive result
                var result = await task();

                if (result.HasValue)
                {
                    // Got a definitive answer (true or false), return it
                    return result.Value;
                }

                // Task returned null, indicating we need to retry
                // Choose backoff strategy based on retry count
                if (retryCount < yieldThreshold)
                {
                    // Fast path: Just yield to allow other threads to run
                    // This is cheap and appropriate for short-lived contention
                    await Task.Yield();
                }
                else
                {
                    // Slow path: Use exponential backoff with delays
                    // Calculate delay: 2^(retryCount - yieldThreshold) milliseconds, capped at maxDelayMs
                    // This reduces CPU usage during sustained contention
                    var shift = Math.Max(retryCount - yieldThreshold, 30);
                    var delayMs = Math.Min(1 << shift, maxDelayMs);
                    await Task.Delay(delayMs);
                }

                retryCount++;
            }
        }
    }

    private sealed class Releaser(Action cleanup) : IDisposable
    {
        private int _disposed;

        public void Dispose()
        {
            if (Interlocked.Exchange(ref _disposed, 1) == 0)
            {
                cleanup();
            }
        }
    }
}
