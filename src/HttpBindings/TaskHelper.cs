using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HttpBindings
{
    internal static class TaskHelper
    {
        internal static async Task<T?> Debounce<T>(Func<Task<T>> taskFactory, int? debounceMs, CancellationToken cancellationToken)
        {
            if (debounceMs.HasValue && !cancellationToken.IsCancellationRequested)
            {
                    await Task.Delay(debounceMs.Value, cancellationToken);
            }

            if (cancellationToken.IsCancellationRequested)
            {
                return default;
            }

            return await taskFactory();
        }
    }
}
