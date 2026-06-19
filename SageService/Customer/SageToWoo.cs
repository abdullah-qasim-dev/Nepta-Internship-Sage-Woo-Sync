using Microsoft.Extensions.DependencyInjection;
using SageIntegration.Configuration;
using SageIntegration.Models;
using SageIntegration.SageRepository.Customer;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Net;
using System.Security.Principal;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using SageRepo = SageIntegration.SageRepository;
namespace SageIntegration.SageService.Customer
{
    public class SageToWoo:ISageToWoo
    {

        private readonly IServiceScopeFactory _serviceScopeFactory;
        private readonly ILogger<Worker> _logger;
        public SageToWoo(IServiceScopeFactory serviceScopeFactory,ILogger<Worker> logger)
        {
            _serviceScopeFactory = serviceScopeFactory;
            _logger = logger;
        }
        public async Task addSageToWooAsync()
        {
            using (var scope = _serviceScopeFactory.CreateScope())
            {
                var SagecustomerRepository = scope.ServiceProvider.GetRequiredService<SageRepo.Customer.ICustomerRepository>();
                var WoocustomerRepository = scope.ServiceProvider.GetRequiredService<SageIntegration.WooRepository.Customer.ICustomerRepository>();
                try
                {
                    // Fetch all customers
                    var customers = await SagecustomerRepository.GetAllAsync();
                    LogManager.Instance.LogMessage("Total Customers FROM sAGE: " + customers.Count.ToString(), "Customer");

                    //Process or log the customers
                    foreach (var customer in customers)
                    {
                        try
                        {
                            if (!string.IsNullOrEmpty(customer.Email))
                            {
                                //string email = IsValidEmail(customer.Email)
                                //                ? customer.Email
                                //                : $"{customer.AccountRef}@kbrh.com";
                                string email = $"{customer.AccountRef}@kbrhcatering.co.uk";
                                var Woocustomer = await WoocustomerRepository.GetCustomer(email);
                                if (Woocustomer == null)
                                {
                                    try
                                    {
                                        //Woocustomer = await WoocustomerRepository.GetCustomer($"{customer.AccountRef}@kbrh.com");
                                        Woocustomer = await WoocustomerRepository.GetCustomer($"{customer.AccountRef}@kbrhcatering.co.uk");
                                    }
                                    catch (Exception ex) { LogManager.Instance.LogException(ex); }
                                    if (Woocustomer == null)
                                    {
                                        await WoocustomerRepository.AddAsync(customer);
                                    }
                                    else
                                    {
                                        await WoocustomerRepository.UpdateAsync(customer, Woocustomer);
                                    }
                                }
                                else
                                {
                                    await WoocustomerRepository.UpdateAsync(customer, Woocustomer);
                                }
                            }
                            else
                            {
                                await WoocustomerRepository.AddAsync(customer);
                            }

                        }
                        catch (Exception ex) {

                            LogManager.Instance.LogMessage("Error While Adding or Updating Customer:" + customer.AccountRef + "ERROR:" + ex.Message, "Customer");
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
        private string GetFieldValue(SageDataObject310.SalesRecord salesRecord, string fieldName, bool splitName = false)
        {
            try
            {
                var fieldValue = salesRecord.Fields.Item(fieldName)?.Value?.ToString();
                return fieldValue ?? string.Empty; // Return empty string if value is null
            }
            catch (Exception ex)
            {
                //_logger.LogWarning("Error retrieving field '{FieldName}': {Message}", fieldName, ex.Message);
                return string.Empty; // Return empty string if an error occurs
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
        //public void PrintCustomerInfo(SageCustomerInfo customer)
        //{
        //    // Print header
        //    LogManager.Instance.LogMessage($"--------------  Printing Customer {customer.AccountName}  --------------", "Customer");
        //    Console.WriteLine($"--------------  Printing Customer {customer.AccountName}  --------------");

        //    // Customer details
        //    Console.WriteLine($"Account Name: {customer.AccountName}");
        //    Console.WriteLine($"Account Manager: {customer.AccountManager}");
        //    Console.WriteLine($"Account Opened: {customer.AccountOpened}");
        //    Console.WriteLine($"Account Ref: {customer.AccountRef}");
        //    Console.WriteLine($"Account Status: {customer.AccountStatus}");

        //    LogManager.Instance.LogMessage($"Account Name: {customer.AccountName}", "Customer");
        //    LogManager.Instance.LogMessage($"Account Manager: {customer.AccountManager}", "Customer");
        //    LogManager.Instance.LogMessage($"Account Opened: {customer.AccountOpened}", "Customer");
        //    LogManager.Instance.LogMessage($"Account Ref: {customer.AccountRef}", "Customer");
        //    LogManager.Instance.LogMessage($"Account Status: {customer.AccountStatus}", "Customer");

        //    // Address
        //    Console.WriteLine($"Address: {customer.Address1}, {customer.Address2}, {customer.Address3}, {customer.Address4}, {customer.Address5}");
        //    LogManager.Instance.LogMessage($"Address: {customer.Address1}, {customer.Address2}, {customer.Address3}, {customer.Address4}, {customer.Address5}", "Customer");

        //    // Contact information
        //    Console.WriteLine($"Contact Name: {customer.ContactName}");
        //    Console.WriteLine($"Telephone: {customer.Telephone}");
        //    LogManager.Instance.LogMessage($"Contact Name: {customer.ContactName}", "Customer");
        //    LogManager.Instance.LogMessage($"Telephone: {customer.Telephone}", "Customer");

        //    // Email
        //    Console.WriteLine($"Email: {customer.Email}");
        //    LogManager.Instance.LogMessage($"Email: {customer.Email}", "Customer");

        //    // Pricing and discounting
        //    Console.WriteLine($"Discount Rate: {customer.DiscountRate}");
        //    Console.WriteLine($"Discount Type: {customer.DiscountType}");
        //    LogManager.Instance.LogMessage($"Discount Rate: {customer.DiscountRate}", "Customer");
        //    LogManager.Instance.LogMessage($"Discount Type: {customer.DiscountType}", "Customer");
        //}

    }
}
