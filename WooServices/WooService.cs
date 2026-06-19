using SageIntegration.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SageRepo = SageIntegration.SageRepository;

namespace SageIntegration.WooServices
{
    public class WooService : IWooService
    {
        private readonly IServiceScopeFactory _serviceScopeFactory;
        private readonly ILogger<Worker> _logger;
        public WooService(IServiceScopeFactory serviceScopeFactory, ILogger<Worker> logger)
        {
            _serviceScopeFactory = serviceScopeFactory;
            _logger = logger;
        }
        public async Task addCustomerWootoSage()
        {
            using (var scope = _serviceScopeFactory.CreateScope())
            {
                var SagecustomerRepository = scope.ServiceProvider.GetRequiredService<SageRepo.Customer.ICustomerRepository>();
                var WoocustomerRepository = scope.ServiceProvider.GetRequiredService<SageIntegration.WooRepository.Customer.ICustomerRepository>();
                try
                {
                    // Fetch all customers
                    var customers = await WoocustomerRepository.GetAllAsync();
                    LogManager.Instance.LogMessage("Total Customers to WOO: " + customers.Count.ToString(), "Customer");

                    //Process or log the customers
                    foreach (var customer in customers)
                    {
                        try
                        {
                            await SagecustomerRepository.AddAsync(customer);
                        }
                        catch (Exception ex)
                        {
                            LogManager.Instance.LogMessage("Error While Adding or Updating Customer in Sage:" + customer.email + "ERROR:" + ex.Message, "Customer");
                            LogManager.Instance.LogException(ex, "Customer");
                        }

                    }



                }
                catch (Exception ex)
                {

                    LogManager.Instance.LogMessage("Error While Running Customer Service ERROR:" + ex.Message, "Customer");
                    LogManager.Instance.LogException(ex, "Customer");
                    _logger.LogError(ex, "Error occurred while retrieving customers.");
                }
            }

        }
        public async Task addProdcutWootoSage()
        {
            using (var scope = _serviceScopeFactory.CreateScope())
            {
                var SageProdcuRepository = scope.ServiceProvider.GetRequiredService<SageRepo.Product.IProductRepository>();
                var WooProdcutRepository = scope.ServiceProvider.GetRequiredService<SageIntegration.WooRepository.Product.IProductRepository>();
                try
                {
                    var products = await WooProdcutRepository.GetAll();
                    LogManager.Instance.LogMessage("Total Product from WOO: " + products.Count.ToString(), "Prdocut");

                    foreach (var prodcut in products)
                    {
                        try
                        {
                            await SageProdcuRepository.AddAsync(prodcut);
                        }
                        catch (Exception ex)
                        {
                            LogManager.Instance.LogMessage("Error While Adding or Updating Customer in Sage:" + prodcut.sku + "ERROR:" + ex.Message, "Customer");
                            LogManager.Instance.LogException(ex, "Customer");
                        }

                    }
                }
                catch (Exception ex)
                {

                    LogManager.Instance.LogMessage("Error While Running Product Service ERROR:" + ex.Message, "Product");
                    LogManager.Instance.LogException(ex, "Product");
                    _logger.LogError(ex, "Error occurred while retrieving customers.");
                }
            }
        }
        public async Task addOrderWootoSage()
        {
            using (var scope = _serviceScopeFactory.CreateScope())
            {
                var SageOrderRepository = scope.ServiceProvider.GetRequiredService<SageRepo.SalesOrder.ISalesOrder>();
                var WooOrderRepository = scope.ServiceProvider.GetRequiredService<SageIntegration.WooRepository.Order.IOrder>();
                var WoocustomerRepository = scope.ServiceProvider.GetRequiredService<SageIntegration.WooRepository.Customer.ICustomerRepository>();

                try
                {
                    var orders = await WooOrderRepository.GetAll();
                    LogManager.Instance.LogMessage("Total Orders from WOO: " + orders.Count.ToString(), "Order");

                    foreach (var order in orders)
                    {
                        try
                        {
                            var customer = new WooCommerceNET.WooCommerce.v3.Customer();
                            if(order.customer_id != null && order.customer_id != 0) {
                                try
                                {
                                    customer = await WoocustomerRepository.GetCustomer(order.customer_id.Value);
                                }
                                catch (Exception ex)
                                {

                                    LogManager.Instance.LogMessage("Error While finding customer for order from woo:" + order.customer_id + "ERROR:" + ex.Message, "Order");
                                    LogManager.Instance.LogException(ex, "Order");
                                }
                            }
                            await SageOrderRepository.AddOrUpdateOrder(order,customer);
                        }
                        catch (Exception ex)
                        {
                            LogManager.Instance.LogMessage("Error While Adding or Updating Order in Sage:" + order.order_key + "ERROR:" + ex.Message, "Customer");
                            LogManager.Instance.LogException(ex, "Order");
                        }

                    }
                }
                catch (Exception ex)
                {

                    LogManager.Instance.LogMessage("Error While Running Order Service ERROR:" + ex.Message, "Order");
                    LogManager.Instance.LogException(ex, "Order");
                    _logger.LogError(ex, "Error occurred while retrieving orders.");
                }
            }
        }

    }
}
