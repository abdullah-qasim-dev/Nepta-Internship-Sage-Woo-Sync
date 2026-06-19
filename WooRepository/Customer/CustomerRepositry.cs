using Microsoft.Extensions.Options;
using SageDataObject310;
using SageIntegration.Configuration;
using SageIntegration.Models;
using SageIntegration.SageRepository.Customer;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using WooCommerceNET;
using WooCommerceNET.WooCommerce.v3;
using WooCommerceNET.WooCommerce.v3.Extension;

namespace SageIntegration.WooRepository.Customer
{
    public class CustomerRepositry : ICustomerRepository
    {
        private readonly WCObject _wc;

        private readonly SchedulingSettings _schedulingSettings;
        public CustomerRepositry(string url, string key, string secret, SchedulingSettings schedulingOptions)
        {
            var rest = new WooCommerceNET.RestAPI(url, key, secret);
            _wc = new WCObject(rest);
            _schedulingSettings = schedulingOptions;
        }
        public async Task AddAsync(SageCustomerInfo customer)
        {
            LogManager.Instance.LogMessage("Adding New Customer:" + customer.AccountRef, "Customer");
            // Determine first and last name based on ContactName or fallback to CompanyName
            string[] nameParts =  customer.AccountName?.Split(' ') ?? customer.ContactName?.Split(' ');
            string firstName = nameParts.Length > 0 ? nameParts[0] : "First";
            string lastName = nameParts.Length > 1 ? string.Join(" ", nameParts.Skip(1)) : "Last";

            //string email = IsValidEmail(customer.Email)
            //            ? customer.Email
            //            : $"{customer.AccountRef}@kbrh.com";
            string email = $"{customer.AccountRef}@kbrhcatering.co.uk";

            // Handle address defaults if any address fields are empty
            string address1 = string.IsNullOrWhiteSpace(customer.Address1) ? "Default Street" : customer.Address1;
            string city = string.IsNullOrWhiteSpace(customer.Address3) ? "London" : customer.Address3;
            string postcode = string.IsNullOrWhiteSpace(customer.Address5) ? "0000" : customer.Address5;

            var newCustomer = new WooCommerceNET.WooCommerce.v3.Customer
            {
                first_name = firstName,
                last_name = lastName,
                email = email,
                role = customer.AccountRef,
                billing = new WooCommerceNET.WooCommerce.v3.CustomerBilling
                {
                    first_name = firstName,
                    last_name = lastName,
                    address_1 = address1,
                    address_2 = customer.Address2,
                    city = city,
                    state = customer.Address4,
                    country = "GB",
                    phone = customer.Telephone,
                    postcode = postcode
                },
                shipping = new WooCommerceNET.WooCommerce.v3.CustomerShipping
                {
                    first_name = firstName,
                    last_name = lastName,
                    address_1 = address1,
                    address_2 = customer.Address2,
                    city = city,
                    state = customer.Address4,
                    country = "GB",
                    postcode = postcode
                },
                meta_data = new List<WooCommerceNET.WooCommerce.v2.CustomerMeta>
                {
                    new WooCommerceNET.WooCommerce.v2.CustomerMeta { key = "AccountName", value = customer.AccountName },
                    new WooCommerceNET.WooCommerce.v2.CustomerMeta { key = "AccountRef", value = customer.AccountRef },
                    new WooCommerceNET.WooCommerce.v2.CustomerMeta { key = "AccountManager", value = customer.AccountManager },
                    new WooCommerceNET.WooCommerce.v2.CustomerMeta { key = "AccountOpened", value = customer.AccountOpened },
                    new WooCommerceNET.WooCommerce.v2.CustomerMeta { key = "AccountStatus", value = customer.AccountStatus },
                    new WooCommerceNET.WooCommerce.v2.CustomerMeta { key = "DiscountRate", value = customer.DiscountRate },
                    new WooCommerceNET.WooCommerce.v2.CustomerMeta { key = "DiscountType", value = customer.DiscountType }
                }
            }; 

            try
            {
                var result = _wc.Customer.Add(newCustomer);
                //return result; 
            }
            catch (Exception ex)
            {
                LogManager.Instance.LogMessage("Error While Adding Customer:" + customer.AccountRef + "ERROR:" + ex.Message, "Customer");
                LogManager.Instance.LogException(ex, "Customer");
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
                return string.Empty;
            }
        }

        public Task DeleteAsync(int id)
        {
            throw new NotImplementedException();
        }

        public async Task<List<WooCommerceNET.WooCommerce.v3.Customer>> GetAllAsync()
        {
            try
            {
                int page = 1;
                int perPage = 50; // Number of customers per page
                List<WooCommerceNET.WooCommerce.v3.Customer> allCustomers = new List<WooCommerceNET.WooCommerce.v3.Customer>();
                while (true)
                {
                    var parameters = new Dictionary<string, string>
                    {
                        { "page", page.ToString() },
                        { "per_page", perPage.ToString() }
                    };
                    var customers = await _wc.Customer.GetAll(parameters);
                    if (customers.Count == 0)
                    {
                        break;
                    }
                    allCustomers.AddRange(customers);
                    page++; 
                }
                DateTime lastRunTime = (DateTime)(_schedulingSettings?.LastRunTime ?? DateTime.MinValue); // Set default if LastRunTime is null
                var recentCustomers = allCustomers.Where(customer =>
                {
                    DateTime? dateCreated = customer.date_created != null ? customer.date_created : (DateTime?)null;
                    DateTime? dateModified = customer.date_modified != null ? customer.date_modified : (DateTime?)null;
                    return (dateCreated.HasValue && dateCreated > lastRunTime) ||
                           (dateModified.HasValue && dateModified > lastRunTime);
                }).ToList();
                return recentCustomers; 
            }
            catch (Exception ex)
            {
                LogManager.Instance.LogException(ex);
                LogManager.Instance.LogMessage("Error while getting customers from Woo ERROR: "+ex.Message,"Customer");
                return new List<WooCommerceNET.WooCommerce.v3.Customer>();
            }
        }

        public async Task UpdateAsync(SageCustomerInfo updateCustomer, WooCommerceNET.WooCommerce.v3.Customer prevCustomer)
        {
            LogManager.Instance.LogMessage("Updating Customer:" + updateCustomer.AccountRef,"Customer");

            // Determine first and last name based on ContactName or fallback to CompanyName
            string[] nameParts = updateCustomer.AccountName.Split(' ') ?? updateCustomer.ContactName?.Split(' ');
            string firstName = nameParts.Length > 0 ? nameParts[0] : "First";
            string lastName = nameParts.Length > 1 ? string.Join(" ", nameParts.Skip(1)) : "Last";

            // Ensure email has a default if null
            //string email = !IsValidEmail(updateCustomer.Email) ? $"{updateCustomer.AccountRef.Trim()}@kbrh.com" : updateCustomer.Email.Trim();
            string email = $"{updateCustomer.AccountRef.Trim()}@kbrhcatering.co.uk";

            // Handle address defaults if any address fields are empty
            string address1 = string.IsNullOrWhiteSpace(updateCustomer.Address1) ? "Default Street" : updateCustomer.Address1;
            string city = string.IsNullOrWhiteSpace(updateCustomer.Address3) ? "London" : updateCustomer.Address3;
            string postcode = string.IsNullOrWhiteSpace(updateCustomer.Address5) ? "0000" : updateCustomer.Address5;

            // Update the WooCommerce customer with new values
            prevCustomer.first_name = firstName;
            prevCustomer.last_name = lastName;
            prevCustomer.email = email;
            prevCustomer.role = updateCustomer.AccountRef;
            prevCustomer.billing = new WooCommerceNET.WooCommerce.v3.CustomerBilling
            {
                first_name = firstName,
                last_name = lastName,
                address_1 = address1,
                address_2 = updateCustomer.Address2,
                city = city,
                state = updateCustomer.Address4,
                country = "GB",
                phone = updateCustomer.Telephone,
                postcode = postcode,
            };
            var metadataItems = new List<(string key, object value)>
            {
                ("AccountName", updateCustomer.AccountName),
                ("AccountRef", updateCustomer.AccountRef),
                ("AccountManager", updateCustomer.AccountManager),
                ("AccountOpened", updateCustomer.AccountOpened),
                ("AccountStatus", updateCustomer.AccountStatus),
                ("DiscountRate", updateCustomer.DiscountRate),
                ("DiscountType", updateCustomer.DiscountType)
            };

            foreach (var item in metadataItems)
            {
                // Check if the metadata item already exists
                var existingMeta = prevCustomer.meta_data.FirstOrDefault(m => m.key == item.key);

                if (existingMeta != null)
                {
                    // Update the existing metadata item
                    existingMeta.value = item.value;
                }
                else
                {
                    // Add new metadata item as it doesn't exist
                    prevCustomer.meta_data.Add(new WooCommerceNET.WooCommerce.v2.CustomerMeta
                    {
                        key = item.key,
                        value = item.value
                    });
                }
            }

            try
            {
                // Update the customer in WooCommerce
                await _wc.Customer.Update(Convert.ToUInt64(prevCustomer.id), prevCustomer);
            }
            catch (Exception ex)
            {
                LogManager.Instance.LogMessage("Error While Updating Customer:" + updateCustomer.AccountRef + "ERROR:" + ex.Message, "Customer");
                LogManager.Instance.LogException(ex, "Customer");
                //throw new Exception($"Error updating customer in WooCommerce: {ex.Message}", ex);
            }
        }

        public async Task<WooCommerceNET.WooCommerce.v3.Customer> GetCustomer(string email)
        {
            var customer = await _wc.Customer.Get(email);
            return customer;
        }
        public async Task<WooCommerceNET.WooCommerce.v3.Customer> GetCustomer(ulong uid)
        {
            var customer = await _wc.Customer.Get(uid);
            return customer;
        }
        public async Task<WooCommerceNET.WooCommerce.v3.Customer> GetCustomerByRole(string Acref)
        {
            try
            {
                var p = await _wc.Customer.GetAll(new Dictionary<string, string>() {
                                { "role", Acref }, // Filter by product name
                                { "per_page", "1" } // Limit to 1 result
                            });
                return p.FirstOrDefault();
            }
            catch (Exception ex)
            {
                return null;
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
