using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace MPR.Connectors
{
    public abstract class Connector
    {
        protected class Pull
        {
            public string Name { get; set; }
            public Func<CancellationToken, Task> Task;
        }

        protected void StartPulls(CancellationToken token, params Pull[] pulls)
        {
            Task.WaitAll(pulls.Select(p => p.Task(token)).ToArray(), token);

            foreach (var p in pulls)
            {
                StartPull(p.Name, token, p.Task);
            }
        }

        protected void StartPull(string taskName, CancellationToken token, Func<CancellationToken, Task> pullTask, int delayInSeconds = 10)
        {
            async void Repeat(CancellationToken ct, Func<CancellationToken, Task> action, int d)
            {
                while (true)
                {
                    if (token.IsCancellationRequested)
                    {
                        return;
                    }

                    await action(ct);
                    await Task.Delay(TimeSpan.FromSeconds(d), token);
                }
            }

            new Thread(() => Repeat(token, pullTask, delayInSeconds))
            {
                Name = taskName,
                Priority = ThreadPriority.Normal,
                IsBackground = true
            }.Start();
        }
    }
}