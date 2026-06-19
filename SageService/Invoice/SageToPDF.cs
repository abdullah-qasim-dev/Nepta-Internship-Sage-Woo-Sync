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

namespace SageIntegration.SageService.Invoice
{
    public class SageToPDF :ISagetoPdf
    {

        private readonly IServiceScopeFactory _serviceScopeFactory;
        private readonly ILogger<Worker> _logger;
        private readonly IConfiguration _configuration;

        public SageToPDF(IServiceScopeFactory serviceScopeFactory, ILogger<Worker> logger, IConfiguration configuration)
        {
            _serviceScopeFactory = serviceScopeFactory;
            _logger = logger;
            _configuration = configuration;
        }
        public async Task addSageToPDF()
        {
            using (var scope = _serviceScopeFactory.CreateScope())
            {
                var SageInvoiceRepo = scope.ServiceProvider.GetRequiredService<SageRepo.Invoice.IInvoiceRepository>();
                var WooproductRepository = scope.ServiceProvider.GetRequiredService<SageIntegration.WooRepository.Product.IProductRepository>();
                var sagecustomerRepo = scope.ServiceProvider.GetRequiredService<SageRepo.Customer.ICustomerRepository>();
                var WooOrderRepository = scope.ServiceProvider.GetRequiredService<SageIntegration.WooRepository.Order.IOrder>();
                var WooCustomerRepository = scope.ServiceProvider.GetRequiredService<SageIntegration.WooRepository.Customer.ICustomerRepository>();
                try
                {
                    // Fetch all customers
                    var invoices = await SageInvoiceRepo.GetAll();

                    //Process or log the customers
                    foreach (var order in invoices)
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
                                if (IsValidEmail(customer.Email))
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
                                    catch (Exception ex) { LogManager.Instance.LogException(ex); }
                                }
                            }
                            foreach (var item in order.Items)
                            {
                                var product = await WooproductRepository.GetProduct(item.ProductCode);
                                if(product != null)
                                {
                                    item.ProductID = product.id;
                                }
                                else
                                {
                                    LogManager.Instance.LogMessage("Product Item Not Found: SKU:" + item.ProductCode, "Invoice");
                                }
                            }
                            if (wooCustomer.id != null)
                            {
                                var ExistingOrder = await WooOrderRepository.GetOrderById(wooCustomer.id.Value, Convert.ToUInt32(order.OrderNumber));
                                if (ExistingOrder == null && order.OrderNumber != null && order.OrderNumber != 0)
                                {
                                    ExistingOrder = await WooOrderRepository.GetOrderById((ulong)order.OrderNumber.Value);
                                }
                                string tempFilePath = Path.Combine(Path.GetTempPath(), $"invoice_order_{ExistingOrder.id.ToString()}_customer_{wooCustomer.id.ToString()}.pdf");
                                UploadPDF upload = new UploadPDF(_configuration);
                                var path = await upload.GenerateOrderPDF(order, tempFilePath, wooCustomer, ExistingOrder);
                                await upload.UploadPDFToWooCommerce(tempFilePath, $"invoice_order_{ExistingOrder.id.ToString()}_customer_{wooCustomer.id.ToString()}_invoice_no_{order.InvRef}.pdf");
                            }
                            

                        }
                        catch (Exception ex)
                        {

                            LogManager.Instance.LogMessage("Error While Uploading Invoice order: ERROR:" + ex.Message, "Invoice");
                            LogManager.Instance.LogException(ex, "Invoice");
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
