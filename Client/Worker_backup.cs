using SageIntegration.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace SageIntegration.Client
{
    internal class Worker_backup
    {
        //private readonly ILogger<Worker> _logger;
        //private readonly IServiceScopeFactory _serviceScopeFactory;
        //private readonly SchedulingSettings _schedulingSettings;

        //public Worker(ILogger<Worker> logger, IServiceScopeFactory serviceScopeFactory, SchedulingSettings schedulingOptions)
        //{
        //    _logger = logger;
        //    _serviceScopeFactory = serviceScopeFactory;
        //    _schedulingSettings = schedulingOptions;
        //}
        //private DateTime GetDefaultLastRunTime(string runType)
        //{
        //    return runType switch
        //    {
        //        "Daily" => DateTime.Now.AddDays(-1),
        //        "Weekly" => DateTime.Now.AddDays(-7),
        //        "Hourly" => DateTime.Now.AddHours(-1),
        //        _ => DateTime.Now
        //    };
        //}

        //protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        //{
        //    //SetLastRun();
        //    //await ADDUpdateCustomers();
        //    ////await ADDUpdateProducts();
        //    //await ADDUpdateOrders();
        //    //await AddSagetoWooMedia();



        //    bool _hasRunOnce = false;
        //    while (!stoppingToken.IsCancellationRequested)
        //    {
        //        if (_logger.IsEnabled(LogLevel.Information))
        //        {
        //            LogManager.Instance.LogMessage("Worker running at: {time}" + DateTimeOffset.Now);
        //        }

        //        // If this is the first run and RunImmediatelyOnStart is set to true, run the task immediately
        //        if (_schedulingSettings.RunImmediatelyOnStart && !_hasRunOnce)
        //        {
        //            LogManager.Instance.LogMessage("First time run initiated immediately as per configuration.");
        //            try
        //            {
        //                _logger.LogInformation("Integration Stared at: {time}", DateTimeOffset.Now);
        //                LogManager.Instance.LogMessage("Integration Stared at: " + DateTimeOffset.Now);
        //                SetLastRun();
        //                await AddWooToSageCustomers();
        //                await ADDUpdateCustomers();
        //                //await AddWooToSageProducts();
        //                //await ADDUpdateProducts();
        //                await ADDUpdateOrders();
        //                await AddWooToSageOrders();
        //                await AddSagetoWooMedia();
        //                _logger.LogInformation("Integration Stared at: {time}", DateTimeOffset.Now);
        //                LogManager.Instance.LogMessage("Integration end at: " + DateTimeOffset.Now);


        //            }
        //            catch (Exception ex)
        //            {
        //                _logger.LogError(ex, "Error occurred while running");
        //            }

        //            _hasRunOnce = true; // Flag the task as having run at least once
        //        }
        //        else if (ShouldRunNow()) // Otherwise, follow the scheduled timing
        //        {
        //            try
        //            {
        //                _logger.LogInformation("Integration Stared at: {time}", DateTimeOffset.Now);
        //                LogManager.Instance.LogMessage("Integration Stared at ShouldRunNow: " + DateTimeOffset.Now);
        //                SetLastRun();
        //                await AddWooToSageCustomers();
        //                await ADDUpdateCustomers();
        //                //await AddWooToSageProducts();
        //                //await ADDUpdateProducts();
        //                await ADDUpdateOrders();
        //                await AddWooToSageOrders();
        //                await AddSagetoWooMedia();
        //                LogManager.Instance.LogMessage("Integration end at: " + DateTimeOffset.Now);

        //            }
        //            catch (Exception ex)
        //            {
        //                _logger.LogError(ex, "Error occurred while running");
        //            }
        //        }

        //        await Task.Delay(TimeSpan.FromMinutes(5), stoppingToken); // Check every minute
        //    }


        //    //while (!stoppingToken.IsCancellationRequested)
        //    //{
        //    //    if (_logger.IsEnabled(LogLevel.Information))
        //    //    {
        //    //        _logger.LogInformation("Worker running at: {time}", DateTimeOffset.Now);
        //    //    }
        //    //    if (ShouldRunNow())
        //    //    {
        //    //        try
        //    //        {
        //    //            _logger.LogInformation("Integration Stared at: {time}", DateTimeOffset.Now);
        //    //            LogManager.Instance.LogMessage("Integration Stared at: " + DateTimeOffset.Now);
        //    //            SetLastRun();
        //    //            await AddWooToSageCustomers();
        //    //            await ADDUpdateCustomers();
        //    //            await AddWooToSageProducts();
        //    //            await ADDUpdateProducts();
        //    //            await AddWooToSageOrders();
        //    //            await ADDUpdateOrders();
        //    //        }
        //    //        catch (Exception ex)
        //    //        {
        //    //            _logger.LogError(ex, "Error occurred while running");
        //    //        }
        //    //    }
        //    //    await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken); // Check every minute
        //    //}
        //}
        //private bool ShouldRunNow()
        //{
        //    var now = DateTime.Now;

        //    _logger.LogInformation($"Checking schedule: Current time: {now}, Scheduled minute: {_schedulingSettings.Minute}, Scheduled hour: {_schedulingSettings.Hour}");

        //    if (_schedulingSettings.RunImmediatelyOnStart)
        //    {
        //        // If RunImmediatelyOnStart is true, run immediately once at startup.
        //        return true;
        //    }
        //    var lastRunTime = _schedulingSettings.LastRunTime.HasValue
        //        ? _schedulingSettings.LastRunTime.Value
        //        : DateTime.MinValue;

        //    var timeSinceLastRun = now - lastRunTime;

        //    _logger.LogInformation($"Checking schedule: Current Time: {now}, LastRunTime: {lastRunTime}, TimeSinceLastRun: {timeSinceLastRun.TotalMinutes} minutes");

        //    return _schedulingSettings.RunType switch
        //    {
        //        "Hourly" => timeSinceLastRun.TotalMinutes >= 60,
        //        "Daily" => timeSinceLastRun.TotalDays >= 1 && now.Hour == _schedulingSettings.Hour && now.Minute == _schedulingSettings.Minute,
        //        "Weekly" => timeSinceLastRun.TotalDays >= 7 && now.DayOfWeek.ToString() == _schedulingSettings.DayOfWeek &&
        //                    now.Hour == _schedulingSettings.Hour && now.Minute == _schedulingSettings.Minute,
        //        "Monthly" => timeSinceLastRun.TotalDays >= 30 && now.Day == 1 &&
        //                     now.Hour == _schedulingSettings.Hour && now.Minute == _schedulingSettings.Minute,
        //        _ => false
        //    };


        //}
        //private void SetLastRun()
        //{
        //    // Read and parse the JSON file
        //    string appSettingsPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "appsettings.json");

        //    if (!File.Exists(appSettingsPath))
        //    {
        //        _logger.LogError($"The file {appSettingsPath} does not exist.");
        //        _schedulingSettings.LastRunTime = DateTime.Now;
        //        return;
        //    }

        //    // Read and parse the JSON file
        //    var jsonString = File.ReadAllText(appSettingsPath);
        //    var config = JsonSerializer.Deserialize<Dictionary<string, object>>(jsonString);

        //    if (config.TryGetValue("Scheduling", out var schedulingObj) && schedulingObj is JsonElement schedulingElement)
        //    {
        //        var runType = schedulingElement.GetProperty("RunType").GetString();
        //        var lastRunTimeStr = schedulingElement.GetProperty("LastRunTime").GetString();

        //        // Check if LastRunTime exists, parse it, or set a default if missing
        //        var lastRunTime = !string.IsNullOrEmpty(lastRunTimeStr)
        //            ? DateTime.Parse(lastRunTimeStr)
        //            : GetDefaultLastRunTime(runType);
        //        _schedulingSettings.LastRunTime = lastRunTime;
        //        // Log and update LastRunTime with the current date and time
        //        _logger.LogInformation($"RunType: {runType}, LastRunTime: {lastRunTime}");
        //        var newRunTime = DateTime.Now;

        //        // Update LastRunTime in the configuration dictionary
        //        var schedulingDict = new Dictionary<string, object>
        //        {
        //            ["RunType"] = runType,
        //            ["Hour"] = schedulingElement.TryGetProperty("Hour", out var hour) ? hour.GetInt32() : 0,
        //            ["Minute"] = schedulingElement.TryGetProperty("Minute", out var minute) ? minute.GetInt32() : 0,
        //            ["DayOfWeek"] = schedulingElement.TryGetProperty("DayOfWeek", out var dayOfWeek) ? dayOfWeek.GetString() : "",
        //            ["LastRunTime"] = newRunTime.ToString("o"), // ISO 8601 format
        //            ["RunImmediatelyOnStart"] = false
        //        };

        //        config["Scheduling"] = JsonSerializer.SerializeToElement(schedulingDict);

        //        // Serialize and write the updated config back to JSON file
        //        var updatedJsonString = JsonSerializer.Serialize(config, new JsonSerializerOptions { WriteIndented = true });
        //        File.WriteAllText(appSettingsPath, updatedJsonString);

        //    }
        //    else
        //    {
        //        _logger.LogError("Scheduling configuration not found in appsettings.json.");
        //    }
        //}
        //private async Task ADDUpdateCustomers()
        //{
        //    using (var scope = _serviceScopeFactory.CreateScope())
        //    {
        //        var SageService = scope.ServiceProvider.GetRequiredService<SageSrc.Customer.ISageToWoo>();
        //        await SageService.addSageToWooAsync();

        //    }
        //}
        //private async Task ADDUpdateProducts()
        //{
        //    using (var scope = _serviceScopeFactory.CreateScope())
        //    {
        //        var SageService = scope.ServiceProvider.GetRequiredService<SageSrc.Product.ISageToWoo>();
        //        await SageService.addSageToWooAsync();
        //    }
        //}
        //private async Task ADDUpdateOrders()
        //{
        //    using (var scope = _serviceScopeFactory.CreateScope())
        //    {
        //        var SageService = scope.ServiceProvider.GetRequiredService<SageSrc.SalesOrder.ISageToWoo>();
        //        await SageService.addSageToWooAsync();
        //    }
        //}
        //private async Task AddWooToSageCustomers()
        //{
        //    using (var scope = _serviceScopeFactory.CreateScope())
        //    {
        //        var wooService = scope.ServiceProvider.GetRequiredService<WooSrc.IWooService>();
        //        await wooService.addCustomerWootoSage();
        //    }
        //}
        //private async Task AddWooToSageProducts()
        //{
        //    using (var scope = _serviceScopeFactory.CreateScope())
        //    {
        //        var wooService = scope.ServiceProvider.GetRequiredService<WooSrc.IWooService>();
        //        await wooService.addProdcutWootoSage();
        //    }
        //}
        //private async Task AddWooToSageOrders()
        //{
        //    using (var scope = _serviceScopeFactory.CreateScope())
        //    {
        //        var wooService = scope.ServiceProvider.GetRequiredService<WooSrc.IWooService>();
        //        await wooService.addOrderWootoSage();
        //    }
        //}
        //private async Task AddSagetoWooMedia()
        //{
        //    using (var scope = _serviceScopeFactory.CreateScope())
        //    {
        //        var sageService = scope.ServiceProvider.GetRequiredService<SageSrc.Invoice.ISagetoPdf>();
        //        await sageService.addSageToPDF();
        //    }
        //}
    }
}
