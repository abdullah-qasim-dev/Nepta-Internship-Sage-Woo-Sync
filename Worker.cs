namespace SageIntegration;

using Microsoft.Extensions.Options;
using System.Text.Json;
using SageDataObject310;
using SageIntegration.Configuration;
using SageRepo = SageIntegration.SageRepository;
using SageSrc = SageIntegration.SageService;
using WooSrc = SageIntegration.WooServices;
using WooRepo = SageIntegration.WooRepository;
using SageIntegration.SageService.Customer;

public class Worker : BackgroundService
{
    private readonly ILogger<Worker> _logger;
    private readonly IServiceScopeFactory _serviceScopeFactory;
    private readonly SchedulingSettings _schedulingSettings;
    private readonly IConfiguration _configuration;

    public Worker(ILogger<Worker> logger, IServiceScopeFactory serviceScopeFactory, SchedulingSettings schedulingOptions, IConfiguration configuration)
    {
        _logger = logger;
        _serviceScopeFactory = serviceScopeFactory;
        _schedulingSettings = schedulingOptions;
        _configuration = configuration;
    }
    private DateTime GetDefaultLastRunTime(string runType)
    {
        return runType switch
        {
            "Daily" => DateTime.Now.AddDays(-1),
            "Weekly" => DateTime.Now.AddDays(-7),
            "Hourly" => DateTime.Now.AddHours(-1),
            _ => DateTime.Now
        };
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
         //var dailyTask = Task.Run(() => RunDailyService(stoppingToken), stoppingToken);
       var intervalTask = Task.Run(() => RunIntervalService(stoppingToken), stoppingToken);

         await Task.WhenAll(intervalTask);

    }
    private async Task RunDailyService(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            var now = DateTime.Now;
            var nextRunTime = now.Date.AddHours(0).AddMinutes(5);
            if (now > nextRunTime)
            {
                nextRunTime = nextRunTime.AddDays(1); 
            }

            var sleepDuration = nextRunTime - now;
            LogManager.Instance.LogMessage($"Daily task scheduled to run at: {nextRunTime}. Sleeping for {sleepDuration.TotalMinutes} minutes.");
            await Task.Delay(10, stoppingToken);

            if (!await CheckAccountStatusAsync(stoppingToken))
            {
                _logger.LogWarning("Account is expired. Please contact Administration.");
                continue; 
            }

            try
            {
                LogManager.Instance.LogMessage("Starting daily task.");
                SetLastRun();
                await RunDailyTask();
                LogManager.Instance.LogMessage("Daily task completed.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while running the daily task.");
            }
        }
    }
    private async Task RunDailyTask()
    {
        // Add your daily task logic here
        LogManager.Instance.LogMessage("Running daily task...");
        await ADDUpdateProducts();
    }
    private async Task RunIntervalService(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            var now = DateTime.Now;

            // Do not run the interval task between 8 PM and 6 AM
            if (now.Hour >= 23 || now.Hour < 6)
            {
                var nextAllowedTime = now.Hour >= 20
                    ? now.Date.AddDays(1).AddHours(6) // 2 AM the next day
                    : now.Date.AddHours(6);          // 2 AM today

                var sleepDuration = nextAllowedTime - now;

                LogManager.Instance.LogMessage($"Skipping interval task as it's between 10 PM and 2 AM. Sleeping until {nextAllowedTime}.");
                await Task.Delay(2, stoppingToken);
                continue; // Skip this iteration
            }

            var lastRunTime = _schedulingSettings.LastRunTimeInterval ?? DateTime.Now;
            var nextRunTime = lastRunTime;

            // Ensure we’re not in the past
            while (nextRunTime <= now)
            {
                nextRunTime = nextRunTime.AddHours(_schedulingSettings.IntervalHours);
            }

            var sleepDurationForNextRun = nextRunTime - now;

            LogManager.Instance.LogMessage($"Interval task scheduled to run at: {nextRunTime}. Sleeping for {sleepDurationForNextRun.TotalMinutes} minutes.");
            await Task.Delay(2, stoppingToken);

            if (!await CheckAccountStatusAsync(stoppingToken))
            {
                _logger.LogWarning("Account is expired. Please contact Administration.");
                continue; // Skip this iteration and wait for the next interval
            }

            try
            {
                LogManager.Instance.LogMessage("Starting interval task.");
                SetLastRun();
                await RunIntervalTask();
                LogManager.Instance.LogMessage("Interval task completed.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while running the interval task.");
            }
        }
    }
    private async Task RunIntervalTask()
    {
        LogManager.Instance.LogMessage("Running interval task...");
        await ADDUpdateCustomers();
        await ADDUpdateOrders();
        await AddWooToSageOrders();
        await AddSagetoWooMedia();
    }
    private bool ShouldRunNow()
    {
        var now = DateTime.Now;

        _logger.LogInformation($"Checking schedule: Current time: {now}, Scheduled minute: {_schedulingSettings.Minute}, Scheduled hour: {_schedulingSettings.Hour}");

        if (_schedulingSettings.RunImmediatelyOnStart)
        {
            // If RunImmediatelyOnStart is true, run immediately once at startup.
            return true;
        }
        var lastRunTime = _schedulingSettings.LastRunTime.HasValue
            ? _schedulingSettings.LastRunTime.Value
            : DateTime.MinValue;

        var timeSinceLastRun = now - lastRunTime;

        _logger.LogInformation($"Checking schedule: Current Time: {now}, LastRunTime: {lastRunTime}, TimeSinceLastRun: {timeSinceLastRun.TotalMinutes} minutes");

        return _schedulingSettings.RunType switch
        {
            "Hourly" => timeSinceLastRun.TotalMinutes >= 60,
            "Daily" => timeSinceLastRun.TotalDays >= 1 && now.Hour == _schedulingSettings.Hour && now.Minute == _schedulingSettings.Minute,
            "Weekly" => timeSinceLastRun.TotalDays >= 7 && now.DayOfWeek.ToString() == _schedulingSettings.DayOfWeek &&
                        now.Hour == _schedulingSettings.Hour && now.Minute == _schedulingSettings.Minute,
            "Monthly" => timeSinceLastRun.TotalDays >= 30 && now.Day == 1 &&
                         now.Hour == _schedulingSettings.Hour && now.Minute == _schedulingSettings.Minute,
            _ => false
        };


    }
    private void UpdateLastRunIntervalInConfig()
    {
        // Update the LastRunTimeInterval in the appsettings.json file
        string appSettingsPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "appsettings.json");
        if (File.Exists(appSettingsPath))
        {
            var jsonString = File.ReadAllText(appSettingsPath);
            var config = JsonSerializer.Deserialize<Dictionary<string, object>>(jsonString);

            if (config.TryGetValue("Scheduling", out var schedulingObj) && schedulingObj is JsonElement schedulingElement)
            {
                var schedulingDict = schedulingElement.EnumerateObject().ToDictionary(
                    prop => prop.Name,
                    prop => prop.Value.ValueKind == JsonValueKind.String ? prop.Value.GetString() : prop.Value.GetRawText()
                );

                schedulingDict["LastRunTimeInterval"] = DateTime.Now.ToString("o");
                var updatedJsonString = JsonSerializer.Serialize(config, new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(appSettingsPath, updatedJsonString);
            }
        }
    }
    private void SetLastRun()
    {
        // Read and parse the JSON file
        string appSettingsPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "appsettings.json");

        if (!File.Exists(appSettingsPath))
        {
            _logger.LogError($"The file {appSettingsPath} does not exist.");
            _schedulingSettings.LastRunTime = DateTime.Now;
            return;
        }

        // Read and parse the JSON file
        var jsonString = File.ReadAllText(appSettingsPath);
        var config = JsonSerializer.Deserialize<Dictionary<string, object>>(jsonString);

        if (config.TryGetValue("Scheduling", out var schedulingObj) && schedulingObj is JsonElement schedulingElement)
        {
            var runType = schedulingElement.GetProperty("RunType").GetString();
            var lastRunTimeStr = schedulingElement.GetProperty("LastRunTime").GetString();

            // Check if LastRunTime exists, parse it, or set a default if missing
            var lastRunTime = !string.IsNullOrEmpty(lastRunTimeStr)
                ? DateTime.Parse(lastRunTimeStr)
                : GetDefaultLastRunTime(runType);
            _schedulingSettings.LastRunTime = lastRunTime;
            // Log and update LastRunTime with the current date and time
            _logger.LogInformation($"RunType: {runType}, LastRunTime: {lastRunTime}");
            var newRunTime = DateTime.Now;

            // Update LastRunTime in the configuration dictionary
            var schedulingDict = new Dictionary<string, object>
            {
                ["RunType"] = runType,
                ["Hour"] = schedulingElement.TryGetProperty("Hour", out var hour) ? hour.GetInt32() : 0,
                ["Minute"] = schedulingElement.TryGetProperty("Minute", out var minute) ? minute.GetInt32() : 0,
                ["DayOfWeek"] = schedulingElement.TryGetProperty("DayOfWeek", out var dayOfWeek) ? dayOfWeek.GetString() : "",
                ["LastRunTime"] = newRunTime.ToString("o"), // ISO 8601 format
                ["RunImmediatelyOnStart"] = false,
                ["DailyHour"] = schedulingElement.TryGetProperty("DailyHour", out var dailyHour) ? dailyHour.GetInt32() : 23,
                ["DailyMinute"] = schedulingElement.TryGetProperty("DailyMinute", out var dailyMinute) ? dailyMinute.GetInt32() : 30,
                ["IntervalHours"] = schedulingElement.TryGetProperty("IntervalHours", out var intervalHours) ? intervalHours.GetInt32() : 3,
                ["LastRunTimeInterval"] = newRunTime.ToString("o") // Update LastRunTimeInterval

            };

            config["Scheduling"] = JsonSerializer.SerializeToElement(schedulingDict);

            // Serialize and write the updated config back to JSON file
            var updatedJsonString = JsonSerializer.Serialize(config, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(appSettingsPath, updatedJsonString);

        }
        else
        {
            _logger.LogError("Scheduling configuration not found in appsettings.json.");
        }
    }
    private async Task ADDUpdateCustomers()
    {
        using (var scope = _serviceScopeFactory.CreateScope())
        {
            var SageService = scope.ServiceProvider.GetRequiredService<SageSrc.Customer.ISageToWoo>();
            await SageService.addSageToWooAsync();

        }
    }
    private async Task ADDUpdateProducts()
    {
        using (var scope = _serviceScopeFactory.CreateScope())
        {
            var SageService = scope.ServiceProvider.GetRequiredService<SageSrc.Product.ISageToWoo>();
            await SageService.addSageToWooAsync();
        }
    }
    private async Task ADDUpdateOrders()
    {
        using (var scope = _serviceScopeFactory.CreateScope())
        {
            var SageService = scope.ServiceProvider.GetRequiredService<SageSrc.SalesOrder.ISageToWoo>();
            await SageService.addSageToWooAsync();
        }
    }
    private async Task AddWooToSageCustomers()
    {
        using (var scope = _serviceScopeFactory.CreateScope())
        {
            var wooService = scope.ServiceProvider.GetRequiredService<WooSrc.IWooService>();
            await wooService.addCustomerWootoSage();
        }
    }
    private async Task AddWooToSageProducts()
    {
        using (var scope = _serviceScopeFactory.CreateScope())
        {
            var wooService = scope.ServiceProvider.GetRequiredService<WooSrc.IWooService>();
            await wooService.addProdcutWootoSage();
        }
    }
    private async Task AddWooToSageOrders()
    {
        using (var scope = _serviceScopeFactory.CreateScope())
        {
            var wooService = scope.ServiceProvider.GetRequiredService<WooSrc.IWooService>();
            await wooService.addOrderWootoSage();
        }
    }
    private async Task AddSagetoWooMedia()
    {
        using (var scope = _serviceScopeFactory.CreateScope())
        {
            var sageService = scope.ServiceProvider.GetRequiredService<SageSrc.Invoice.ISagetoPdf>();
            await sageService.addSageToPDF();
        }
    }
    private async Task<bool> CheckAccountStatusAsync(CancellationToken stoppingToken)
    {
        var apiKey = _configuration["Settings:api_key"];
        var checkUrl = $"https://sage.meernmeer.com/check_status.php?api_key={apiKey}";

        using (var httpClient = new HttpClient())
        {
            try
            {
                var response = await httpClient.GetAsync(checkUrl, stoppingToken);
                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync(stoppingToken);
                    var json = JsonDocument.Parse(content);
                    var success = json.RootElement.GetProperty("success").GetBoolean();
                    var status = json.RootElement.GetProperty("status").GetBoolean();

                    return success && status;
                }
                else
                {
                    LogManager.Instance.LogMessage($"Failed to check status. Status code: {response.StatusCode}","AccountExpire");
                }
            }
            catch (Exception ex)
            {
                LogManager.Instance.LogMessage($"Exception occurred while checking account status: {ex.Message}", "AccountExpire");
            }

            return false;
        }
    }

}
