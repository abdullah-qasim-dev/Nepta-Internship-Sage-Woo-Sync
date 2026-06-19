using Microsoft.Extensions.Options;
using SageDataObject310;
using SageIntegration.Client;
using SageIntegration.Configuration;
using SageIntegration.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static System.Net.Mime.MediaTypeNames;

namespace SageIntegration.SageRepository.Customer
{
    public class CustomerRepository :ICustomerRepository
    {
        private readonly SageConnectionManager _sageConnectionManager;
        private readonly SchedulingSettings _schedulingSettings;

        public CustomerRepository(SageConnectionManager sageConnectionManager, SchedulingSettings schedulingOptions)
        {
            _sageConnectionManager = sageConnectionManager;
            _schedulingSettings = schedulingOptions;

        }

        public async Task<SageDataObject310.SalesRecord> AddAsync(WooCommerceNET.WooCommerce.v3.Customer wooCustomer)
        {
            SageDataObject310.SalesRecord oSalesRecord =null;
            SageDataObject310.WorkSpace oWS = null;
            try
            {
                oWS = _sageConnectionManager.ConnectToSage();
                oSalesRecord = (SageDataObject310.SalesRecord)oWS.CreateObject("SalesRecord");
                var accountRef = wooCustomer.meta_data.FirstOrDefault(meta => meta.key == "AccountRef")?.value ?? string.Empty;

                // Set email field with null check
                oSalesRecord.Fields.Item("ACCOUNT_REF").Value = accountRef ?? string.Empty;
                var bFlag = oSalesRecord.Find(false);
                var addEditFlag = false;

                if (bFlag)
                {
                    addEditFlag = oSalesRecord.Edit();
                }
                else
                {
                    addEditFlag = oSalesRecord.AddNew();
                }

                if (addEditFlag)
                {
                    // Map fields with null-coalescing to handle potential nulls
                    oSalesRecord.Fields.Item("NAME").Value = wooCustomer.billing.company ?? $"{wooCustomer.first_name} {wooCustomer.last_name}";
                    //oSalesRecord.Fields.Item("ACCOUNT_REF").Value = wooCustomer.role ?? string.Empty;
                    oSalesRecord.Fields.Item("CONTACT_NAME").Value = $"{wooCustomer.first_name ?? "First"} {wooCustomer.last_name ?? "Last"}";
                    oSalesRecord.Fields.Item("TELEPHONE").Value = wooCustomer.billing.phone ?? string.Empty;
                    oSalesRecord.Fields.Item("E_MAIL").Value = wooCustomer.email ?? $"{wooCustomer.id}@example.com"; // default email if null

                    if (!bFlag)
                    {
                        oSalesRecord.Fields.Item("ACCOUNT_REF").Value = GenerateUniqueAccountRef(oWS);
                    }
                    // Address fields with null checks
                    oSalesRecord.Fields.Item("ADDRESS_1").Value = wooCustomer.billing.address_1 ?? string.Empty;
                    oSalesRecord.Fields.Item("ADDRESS_2").Value = wooCustomer.billing.address_2 ?? string.Empty;
                    oSalesRecord.Fields.Item("ADDRESS_3").Value = wooCustomer.billing.city ?? "Default City";
                    oSalesRecord.Fields.Item("ADDRESS_4").Value = wooCustomer.billing.state ?? "Default State";
                    oSalesRecord.Fields.Item("ADDRESS_5").Value = wooCustomer.billing.postcode ?? "00000";

                    // Map meta_data fields with null checks
                    var accountManager = wooCustomer.meta_data.FirstOrDefault(meta => meta.key == "AccountManager")?.value ?? string.Empty;
                    var accountName = wooCustomer.meta_data.FirstOrDefault(meta => meta.key == "AccountName")?.value ?? string.Empty;
                    var accountOpened = wooCustomer.meta_data.FirstOrDefault(meta => meta.key == "AccountOpened")?.value ?? string.Empty;
                    var accountStatus = wooCustomer.meta_data.FirstOrDefault(meta => meta.key == "AccountStatus")?.value ?? string.Empty;
                    var discountRate = wooCustomer.meta_data.FirstOrDefault(meta => meta.key == "DiscountRate")?.value ?? "0"; // Default to 0 if null
                    var discountType = wooCustomer.meta_data.FirstOrDefault(meta => meta.key == "DiscountType")?.value ?? string.Empty;

                    oSalesRecord.Fields.Item("ACCOUNT_MANAGER").Value = accountManager;
                    oSalesRecord.Fields.Item("Name").Value = accountName;
                    oSalesRecord.Fields.Item("ACCOUNT_OPENED").Value = accountOpened;
                    oSalesRecord.Fields.Item("ACCOUNT_STATUS").Value = accountStatus;
                    oSalesRecord.Fields.Item("DISCOUNT_RATE").Value = discountRate;
                    oSalesRecord.Fields.Item("DISCOUNT_TYPE").Value = discountType;

                    // Save record if update is successful
                    if (oSalesRecord.Update())
                    {
                        LogManager.Instance.LogMessage("Added customer in Sage "+wooCustomer.email, "Customer");
                        return oSalesRecord;
                    }
                    else
                    {
                        throw new Exception("The account could not be created.");
                    }
                }
                else
                {
                    LogManager.Instance.LogMessage("Error While adding Customer in Sage: " + wooCustomer.email, "Customer");
                    throw new Exception("Failed to add a new SalesRecord.");
                }
            }
            catch (Exception ex)
            {
                LogManager.Instance.LogMessage("Error While adding Customer in Sage ERROR: " + ex.Message, "Customer");
                LogManager.Instance.LogException(ex, "Customer");
                throw new Exception($"Error adding customer: {ex.Message}", ex);
            }
            finally
            {
                _sageConnectionManager.Disconnect(oWS);
                System.Runtime.InteropServices.Marshal.ReleaseComObject(oSalesRecord);
                oSalesRecord = null;
            }
        }
        private string GenerateUniqueAccountRef(SageDataObject310.WorkSpace oWS)
        {
            var random = new Random();
            string accountRef;
            SageDataObject310.SalesRecord tempRecord = (SageDataObject310.SalesRecord)oWS.CreateObject("SalesRecord");

            do
            {
                accountRef = $"CUST{random.Next(1000, 9999)}";
                tempRecord.Fields.Item("ACCOUNT_REF").Value = accountRef;
            }
            while (tempRecord.Find(false));

            return accountRef;
        }

        public async Task<List<SageCustomerInfo>> GetAllAsync()
        {
            SageDataObject310.SalesRecord oSalesRecord =null ;
            SageDataObject310.WorkSpace oWS = null;
            List<SageCustomerInfo> salesRecords = new List<SageCustomerInfo>();

            try
            {
                oWS = _sageConnectionManager.ConnectToSage();
                oSalesRecord = (SalesRecord)oWS.CreateObject("SalesRecord");
                oSalesRecord.MoveFirst();
                do
                {
                    DateTime lastRunTime = (DateTime)(_schedulingSettings?.LastRunTime); // For example, filter for last 24 hours
                    DateTime recordModifiedDate = oSalesRecord.Fields.Item("RECORD_MODIFY_DATE").Value;
                    DateTime recordCreatedDate = oSalesRecord.Fields.Item("RECORD_CREATE_DATE").Value;
                    if (recordModifiedDate >= lastRunTime || recordCreatedDate >= lastRunTime)
                    {
                        var customer = new SageCustomerInfo
                        {
                            AccountName = GetFieldValue(oSalesRecord, "NAME", true),
                            AccountManager = GetFieldValue(oSalesRecord, "ACCOUNT_MANAGER", true),
                            AccountOpened = GetFieldValue(oSalesRecord, "ACCOUNT_OPENED", true),
                            AccountRef = GetFieldValue(oSalesRecord, "ACCOUNT_REF", true),
                            AccountStatus = GetFieldValue(oSalesRecord, "ACCOUNT_STATUS", true),

                            Address1 = GetFieldValue(oSalesRecord, "ADDRESS_1", true),
                            Address2 = GetFieldValue(oSalesRecord, "ADDRESS_2", true),
                            Address3 = GetFieldValue(oSalesRecord, "ADDRESS_3", true),
                            Address4 = GetFieldValue(oSalesRecord, "ADDRESS_4", true),
                            Address5 = GetFieldValue(oSalesRecord, "ADDRESS_5", true),

                            ContactName = GetFieldValue(oSalesRecord, "CONTACT_NAME", true),
                            Telephone = GetFieldValue(oSalesRecord, "TELEPHONE", true),
                            Email = GetFieldValue(oSalesRecord, "E_MAIL", true),

                            DiscountRate = GetFieldValue(oSalesRecord, "DISCOUNT_RATE", true),
                            DiscountType = GetFieldValue(oSalesRecord, "DISCOUNT_TYPE", true)
                        };
                        salesRecords.Add(customer);
                    }
                    
                } while (oSalesRecord.MoveNext());
                return salesRecords;
            }
            catch (Exception ex)
            {
                LogManager.Instance.LogMessage("Error While getting Customer from Sage ERROR:" + ex.Message, "Customer");
                LogManager.Instance.LogException(ex, "Customer");
                return new List<SageCustomerInfo>();
            }
            finally
            {
                _sageConnectionManager.Disconnect(oWS);
                System.Runtime.InteropServices.Marshal.ReleaseComObject(oSalesRecord);
                oSalesRecord = null;
            }
        }
        public async Task<SageCustomerInfo> GetByKey(string AcctRef)
        {
            SageDataObject310.SalesRecord oSalesRecord = null;
            SageDataObject310.WorkSpace oWS = null;
            SageCustomerInfo salesRecords = new SageCustomerInfo();

            try
            {
                oWS = _sageConnectionManager.ConnectToSage();
                oSalesRecord = (SalesRecord)oWS.CreateObject("SalesRecord");
                oSalesRecord.Fields.Item("ACCOUNT_REF").Value = AcctRef;
                var bFlag = oSalesRecord.Find(false);
                if (bFlag)
                {
                    salesRecords = new SageCustomerInfo
                    {
                        AccountName = GetFieldValue(oSalesRecord, "NAME", true),
                        AccountManager = GetFieldValue(oSalesRecord, "ACCOUNT_MANAGER", true),
                        AccountOpened = GetFieldValue(oSalesRecord, "ACCOUNT_OPENED", true),
                        AccountRef = GetFieldValue(oSalesRecord, "ACCOUNT_REF", true),
                        AccountStatus = GetFieldValue(oSalesRecord, "ACCOUNT_STATUS", true),

                        Address1 = GetFieldValue(oSalesRecord, "ADDRESS_1", true),
                        Address2 = GetFieldValue(oSalesRecord, "ADDRESS_2", true),
                        Address3 = GetFieldValue(oSalesRecord, "ADDRESS_3", true),
                        Address4 = GetFieldValue(oSalesRecord, "ADDRESS_4", true),
                        Address5 = GetFieldValue(oSalesRecord, "ADDRESS_5", true),

                        ContactName = GetFieldValue(oSalesRecord, "CONTACT_NAME", true),
                        Telephone = GetFieldValue(oSalesRecord, "TELEPHONE", true),
                        Email = GetFieldValue(oSalesRecord, "E_MAIL", true),

                        DiscountRate = GetFieldValue(oSalesRecord, "DISCOUNT_RATE", true),
                        DiscountType = GetFieldValue(oSalesRecord, "DISCOUNT_TYPE", true)
                    };
                    return salesRecords;
                }
                return new SageCustomerInfo();
            }
            catch (Exception ex)
            {
                LogManager.Instance.LogMessage("Error While getting Customer from Sage ERROR:" + ex.Message, "Customer");
                LogManager.Instance.LogException(ex, "Customer");
                return new SageCustomerInfo();
            }
            finally
            {
                _sageConnectionManager.Disconnect(oWS);
                System.Runtime.InteropServices.Marshal.ReleaseComObject(oSalesRecord);
                oSalesRecord = null;
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
        void PrintSageToWooCommerceDetails(SageDataObject310.SalesRecord customer)
        {
            // Account Detail
            var accountName = GetFieldValue(customer, "NAME", true);
            var accountManager = GetFieldValue(customer, "ACCOUNT_MANAGER", true);
            var accountOpened = GetFieldValue(customer, "ACCOUNT_OPENED", true);
            var accountRef = GetFieldValue(customer, "ACCOUNT_REF", true);
            var accountStatus = GetFieldValue(customer, "ACCOUNT_STATUS", true);

            LogManager.Instance.LogMessage($"--------------  Printing Customer {accountName}  --------------", "Customer");
            Console.WriteLine($"--------------  Printing Customer {accountName}  --------------");

            Console.WriteLine($"Account Name: {accountName}");
            Console.WriteLine($"Account Manager: {accountManager}");
            Console.WriteLine($"Account Opened: {accountOpened}");
            Console.WriteLine($"Account Ref: {accountRef}");
            Console.WriteLine($"Account Status: {accountStatus}");

            LogManager.Instance.LogMessage($"Account Name: {accountName}", "Customer");
            LogManager.Instance.LogMessage($"Account Manager: {accountManager}", "Customer");
            LogManager.Instance.LogMessage($"Account Opened: {accountOpened}", "Customer");
            LogManager.Instance.LogMessage($"Account Ref: {accountRef}", "Customer");
            LogManager.Instance.LogMessage($"Account Status: {accountStatus}", "Customer");

            // Address
            var address1 = GetFieldValue(customer, "ADDRESS_1", true);
            var address2 = GetFieldValue(customer, "ADDRESS_2", true);
            var address3 = GetFieldValue(customer, "ADDRESS_3", true);
            var address4 = GetFieldValue(customer, "ADDRESS_4", true);
            var address5 = GetFieldValue(customer, "ADDRESS_5", true);

            Console.WriteLine($"Address: {address1}, {address2}, {address3}, {address4}, {address5}");
            LogManager.Instance.LogMessage($"Address: {address1}, {address2}, {address3}, {address4}, {address5}", "Customer");

            // Contact Name and Telephone
            var contactName = GetFieldValue(customer, "CONTACT_NAME", true);
            var telephone = GetFieldValue(customer, "TELEPHONE", true);

            Console.WriteLine($"Contact Name: {contactName}");
            Console.WriteLine($"Telephone: {telephone}");
            LogManager.Instance.LogMessage($"Contact Name: {contactName}", "Customer");
            LogManager.Instance.LogMessage($"Telephone: {telephone}", "Customer");

            // Email
            var email = GetFieldValue(customer, "E_MAIL", true);
            Console.WriteLine($"Email: {email}");
            LogManager.Instance.LogMessage($"Email: {email}", "Customer");

            // Pricing and Discounting
            var discountRate = GetFieldValue(customer, "DISCOUNT_RATE", true);
            var discountType = GetFieldValue(customer, "DISCOUNT_TYPE", true);

            Console.WriteLine($"Discount Rate: {discountRate}");
            Console.WriteLine($"Discount Type: {discountType}");
            LogManager.Instance.LogMessage($"Discount Rate: {discountRate}", "Customer");
            LogManager.Instance.LogMessage($"Discount Type: {discountType}", "Customer");
        }
    }
}
