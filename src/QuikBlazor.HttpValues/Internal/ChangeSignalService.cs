using Microsoft.Extensions.Primitives;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QuikBlazor.HttpValues.Internal;

internal interface ISignalReceiver
{
    Task Signal();
}

internal class ChangeSignalService
{
    private List<WeakReference<ISignalReceiver>> _receivers = new();

    internal Task Signal()
    {
        var tasks = new List<Task>();

        lock (_receivers)
        {
            var newReceivers = _receivers.ToList();

            foreach (var receiver in _receivers)
            {
                if (!receiver.TryGetTarget(out var signalReceiver))
                {
                    newReceivers.Remove(receiver);
                    continue;
                }

                tasks.Add(Task.Run(() =>
                {
                    signalReceiver?.Signal();
                }));
            }

            _receivers = newReceivers;
        }

        return Task.WhenAll(tasks.ToArray());
    }

    internal void RegisterForSignal<T>(T receiver) where T : ISignalReceiver
    {
        _receivers.Add(new WeakReference<ISignalReceiver>(receiver));
    }
}
