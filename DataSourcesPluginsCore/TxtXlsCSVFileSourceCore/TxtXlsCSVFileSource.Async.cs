using System;
using System.Threading.Tasks;

namespace TheTechIdea.Beep.FileManager
{
    /// <summary>
    /// Provides static helper methods for wrapping synchronous operations in async Tasks.
    /// Consolidated consolidation pattern to eliminate repeated Task.Run boilerplate.
    /// </summary>
    internal static class AsyncWrapper
    {
        /// <summary>
        /// Wraps a synchronous action that returns a value in an async Task
        /// </summary>
        public static Task<T> WrapAsync<T>(Func<T> operation)
        {
            return Task.Run(() => operation());
        }

        /// <summary>
        /// Wraps a synchronous void action in an async Task
        /// </summary>
        public static Task WrapAsync(Action operation)
        {
            return Task.Run(() => operation());
        }

        /// <summary>
        /// Wraps a synchronous operation with a parameter in an async Task
        /// </summary>
        public static Task<T> WrapAsync<TParam, T>(Func<TParam, T> operation, TParam parameter)
        {
            return Task.Run(() => operation(parameter));
        }
    }
}
