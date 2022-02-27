using Microsoft.Extensions.Hosting;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace CloudManufacturingAPI.Services
{
    public abstract class BackgroundService : IHostedService, IDisposable
    {
        #region ATTRIBUTES
        private Task _executingTask;
        private readonly CancellationTokenSource _stoppingCts = new CancellationTokenSource();
        #endregion ATTRIBUTES

        #region ABSTRACT
        protected abstract Task ExecuteAsync(CancellationToken stoppingToken);
        #endregion ABSTRACT

        #region VIRTUAL
        public virtual Task StartAsync(CancellationToken cancellationToken)
        {
            _executingTask = ExecuteAsync(_stoppingCts.Token);
            if (_executingTask.IsCompleted)
                return _executingTask;

            return Task.CompletedTask;
        }

        public virtual async Task StopAsync(CancellationToken cancellationToken)
        {
            if (_executingTask == null)
                return;

            try
            {
                _stoppingCts.Cancel();
            }
            finally
            {
                await Task.WhenAny(_executingTask, Task.Delay(Timeout.Infinite, cancellationToken));
            }
        }

        public virtual void Dispose()
        {
            _stoppingCts.Cancel();
        }
        #endregion VIRTUAL
    }
}
