using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using SageIntegration;
using SageIntegration.WooRepository.Customer;
using SageIntegration.WooRepository.Order;
using SageIntegration.WooRepository.Product;
using SageIntegration.WooServices;
using SageIntegration.WooServices.Customer;
using SageRepo = SageIntegration.SageRepository;
using SageService = SageIntegration.SageService;
using Microsoft.Extensions.Options;
using SageIntegration.Client;
using SageIntegration.Configuration;
using SageIntegration.SageService.Invoice;
using SageIntegration.SageRepository.Invoice;

var host = Host.CreateDefaultBuilder(args)
    .ConfigureAppConfiguration((context, config) =>
    {
        // Load the configuration from appsettings.json
        config.AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);
    })
    .ConfigureServices((context, services) =>
    {
        // Bind the configuration to POCO classes
        var configuration = context.Configuration;

        var wooCommerceSettings = configuration.GetSection("WooCommerce").Get<WooCommerceSettings>();
        var sageSettings = configuration.GetSection("Sage").Get<Sage>();
        var schedulingSettings = configuration.GetSection("Scheduling").Get<SchedulingSettings>();

        // Register settings as singleton
        services.AddSingleton(wooCommerceSettings);
        services.AddSingleton(sageSettings);
        services.AddSingleton(schedulingSettings);

        // Register the services (repositories, connections, etc.)
        services.AddScoped<ICustomerRepository, CustomerRepositry>(provider =>
            new CustomerRepositry(wooCommerceSettings.Url, wooCommerceSettings.Key, wooCommerceSettings.Secret,
            schedulingSettings));

        services.AddScoped<IProductRepository, ProductRepository>(provider =>
            new ProductRepository(wooCommerceSettings.Url, wooCommerceSettings.Key, wooCommerceSettings.Secret,
            schedulingSettings, configuration));

        services.AddScoped<IOrder, Order>(provider =>
            new Order(wooCommerceSettings.Url, wooCommerceSettings.Key, wooCommerceSettings.Secret,
            schedulingSettings));

        // Register Sage connection manager
        services.AddScoped<SageConnectionManager>(provider =>
        {
            var sageSettings = provider.GetRequiredService<Sage>();
            return new SageConnectionManager(sageSettings.CompanyPath, sageSettings.UserName, sageSettings.Password, sageSettings.WorkSpace);
        });

        // Register repositories and services for Sage
        services.AddScoped<SageRepo.Customer.ICustomerRepository, SageRepo.Customer.CustomerRepository>();
        services.AddScoped<SageRepo.Product.IProductRepository, SageRepo.Product.ProductRepository>();
        services.AddScoped<SageRepo.SalesOrder.ISalesOrder, SageRepo.SalesOrder.SalesOrder>();

        // Register services for interacting with WooCommerce
        services.AddScoped<SageService.Customer.ISageToWoo, SageService.Customer.SageToWoo>();
        services.AddScoped<SageService.Product.ISageToWoo, SageService.Product.SageToWoo>();
        services.AddScoped<SageService.SalesOrder.ISageToWoo, SageService.SalesOrder.SageToWoo>();
        services.AddScoped<IWooService, WooService>();
        services.AddScoped<ISagetoPdf, SageToPDF>();
        services.AddScoped<IInvoiceRepository, InvoiceRepository>();

        // Register any custom services like CustomerService
        services.AddScoped<CustomerService>();

        // Register the Worker as a hosted service
        services.AddHostedService<Worker>();

        // Add logging to the DI container
        services.AddLogging();
    })
    .UseWindowsService() // Ensures the application runs as a service
    .Build();

host.Run();
