using SageIntegration.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using SageRepo = SageIntegration.SageRepository;

namespace SageIntegration.SageService.SalesOrder
{
    public class SageToWoo :ISageToWoo
    {
        private readonly IServiceScopeFactory _serviceScopeFactory;
        private readonly ILogger<Worker> _logger;
        public SageToWoo(IServiceScopeFactory serviceScopeFactory, ILogger<Worker> logger)
        {
            _serviceScopeFactory = serviceScopeFactory;
            _logger = logger;
        }
        public async Task addSageToWooAsync()
        {
            using (var scope = _serviceScopeFactory.CreateScope())
            {
                var SageOrderRepo = scope.ServiceProvider.GetRequiredService<SageRepo.SalesOrder.ISalesOrder>();
                var WooproductRepository = scope.ServiceProvider.GetRequiredService<SageIntegration.WooRepository.Product.IProductRepository>();
                var sagecustomerRepo = scope.ServiceProvider.GetRequiredService<SageRepo.Customer.ICustomerRepository>();
                var WooOrderRepository = scope.ServiceProvider.GetRequiredService<SageIntegration.WooRepository.Order.IOrder>();
                var WooCustomerRepository = scope.ServiceProvider.GetRequiredService<SageIntegration.WooRepository.Customer.ICustomerRepository>();
                try
                {
                    // Fetch all customers
                    var orders = await SageOrderRepo.GetAll();
                    LogManager.Instance.LogMessage("Total Orders from Sage: " + orders.Count.ToString(), "Order");

                    //Process or log the customers
                    foreach (var order in orders)
                    {
                        try
                        {
                            WooCommerceNET.WooCommerce.v3.Customer wooCustomer = new WooCommerceNET.WooCommerce.v3.Customer();
                            var customer = await sagecustomerRepo.GetByKey(order.CustomerRef);
                            if (customer == null)
                            {
                                order.CustomerId = 0;
                            }
                            else
                            {
                                if(IsValidEmail(customer.Email))
                                {
                                    wooCustomer = await WooCustomerRepository.GetCustomer(customer.Email);
                                }
                                else
                                {
                                    try
                                    {
                                        //wooCustomer = await WooCustomerRepository.GetCustomer($"{order.CustomerRef}@kbrh.com");
                                        wooCustomer = await WooCustomerRepository.GetCustomer($"{order.CustomerRef}@kbrhcatering.co.uk");
                                    }
                                    catch (Exception ex) {LogManager.Instance.LogException(ex); }  
                                }
                            }
                            foreach(var item in order.Items)
                            {
                                var product = await WooproductRepository.GetProduct(item.ProductCode);
                                item.ProductID = product.id;
                            }
                            var ExistingOrder = await WooOrderRepository.GetOrderById(wooCustomer.id.Value,Convert.ToUInt32(order.OrderNumber));
                            if(ExistingOrder == null && order.OrderNumber !=null && order.OrderNumber !=0)
                            {
                                try
                                {
                                    ExistingOrder = await WooOrderRepository.GetOrderById((ulong)order.OrderNumber.Value);
                                }
                                catch (Exception ex) { }
                            }
                            if (ExistingOrder != null)
                            {
                                await WooOrderRepository.UpdateAsync(order, ExistingOrder, wooCustomer);
                            }
                            else
                            {
                                await WooOrderRepository.AddAsync(order, wooCustomer);
                            }
                        }
                        catch (Exception ex)
                        {

                            LogManager.Instance.LogMessage("Error While Adding or Updating order: ERROR:" + ex.Message, "Sales");
                            LogManager.Instance.LogException(ex, "Sales");
                        }

                    }

                }
                catch (Exception ex)
                {

                    LogManager.Instance.LogMessage("Error While Running Product Service ERROR:" + ex.Message, "Product");
                    LogManager.Instance.LogException(ex, "Product");
                    _logger.LogError(ex, "Error occurred while retrieving ppp.");
                }
            }

        }
        public static bool IsValidEmail(string email)
        {
            if (string.IsNullOrWhiteSpace(email))
                return false;

            // Syntactic validation
            string emailPattern = @"^[^@\s,]+@[^@\s]+\.[^@\s]+$";
            if (!Regex.IsMatch(email, emailPattern))
                return false;

            // Extract the domain
            string domain = email.Substring(email.IndexOf('@') + 1);

            // Check if the domain exists
            return DomainExists(domain);
        }

        private static bool DomainExists(string domain)
        {
            try
            {
                // Attempt to get DNS records for the domain
                var host = Dns.GetHostEntry(domain);
                return host.AddressList.Length > 0;
            }
            catch (SocketException)
            {
                // Domain does not exist
                return false;
            }
        }
    }
}
