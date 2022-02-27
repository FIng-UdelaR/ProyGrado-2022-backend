using CloudManufacturingAPI.Repositories.SystemManagement;
using Microsoft.Extensions.Logging;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace CloudManufacturingAPI.Services
{
    public class MonitoringService : BackgroundService
    {
        private readonly ISystemRepository _systemRepository;
        private readonly ILogger<MonitoringService> _logger;

        public MonitoringService(ILogger<MonitoringService> logger, ISystemRepository systemRepository)
        {
            _logger = logger;
            _systemRepository = systemRepository;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            try
            {
                while (!stoppingToken.IsCancellationRequested)
                {
                    if (_systemRepository.GetRunMonitoringAsService())
                    {
                        try
                        {
                            _systemRepository.RunMonitoring();
                        
                        }
                        catch (Exception ex) 
                        {
                            _logger.LogError($"Exception in MonitoringService.ExecuteAsync loop: {ex}");
                        }
                    }
                    await Task.Delay(TimeSpan.FromSeconds(15), stoppingToken);
                }
            }
            catch (Exception ex) 
            {
                _logger.LogError($"Exception in MonitoringService.ExecuteAsync: {ex}");
            }
        }
    }
}
