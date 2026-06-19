using SageIntegration.Client;
using SageIntegration.Configuration;
using SageIntegration.SageRepository.Product;
using SageIntegration.SageRepository.SalesOrder;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SageIntegration.SageRepository.Invoice
{
    public class InvoiceRepository : IInvoiceRepository
    {
        private readonly SageConnectionManager _sageConnectionManager;
        private readonly SchedulingSettings _schedulingSettings;
        private readonly IProductRepository _productRepository;
        public InvoiceRepository(SageConnectionManager sageConnectionManager, SchedulingSettings schedulingOptions, IProductRepository productRepository)
        {
            _sageConnectionManager = sageConnectionManager;
            _schedulingSettings = schedulingOptions;
            _productRepository = productRepository;
        }
        public async Task<List<SalesOrderInfo>> GetAll()
        {
            LogManager.Instance.LogMessage("Getting Sales Order from Sage", "Sales");
            SageDataObject310.InvoiceRecord oSopRecord = null;
            SageDataObject310.InvoiceItem oSopItem_Read = null;
            SageDataObject310.WorkSpace oWS = null;
            List<SalesOrderInfo> SopRecords = new List<SalesOrderInfo>();

            try
            {
                oWS = _sageConnectionManager.ConnectToSage();

                // Create the SalesRecord object from the workspace
                oSopRecord = (SageDataObject310.InvoiceRecord)oWS.CreateObject("InvoiceRecord");
                oSopItem_Read = (SageDataObject310.InvoiceItem)oWS.CreateObject("InvoiceItem");
                do
                {
                    DateTime lastRunTime = (DateTime)(_schedulingSettings?.LastRunTime); // For example, filter for last 24 hours
                    DateTime recordModifiedDate = oSopRecord.Fields.Item("RECORD_MODIFY_DATE").Value;
                    DateTime recordCreatedDate = oSopRecord.Fields.Item("RECORD_CREATE_DATE").Value;
                    if (recordModifiedDate >= lastRunTime || recordCreatedDate >= lastRunTime)
                    {
                        try
                        {
                            var salesOrder = new SalesOrderInfo
                            {
                                CustomerRef = GetFieldValue(oSopRecord, "ACCOUNT_REF"),
                                OrderNumber = SafeConvertToNullableInt32(GetFieldValue(oSopRecord, "ORDER_NUMBER")),
                                OrderType = GetFieldValue(oSopRecord, "ORDER_TYPE"),
                                InvRef = GetFieldValue(oSopRecord, "INVOICE_NUMBER"),
                                OrderDate = SafeConvertToNullableDateTime(GetFieldValue(oSopRecord, "INVOICE_DATE")),
                                TotalGBP = SafeConvertToDecimal(GetFieldValue(oSopRecord, "BASE_TOT_NET")),
                                CarriageGBP = SafeConvertToDecimal(GetFieldValue(oSopRecord, "CARR_NET")),
                                CARR_TAX = SafeConvertToDecimal(GetFieldValue(oSopRecord, "CARR_TAX")),
                                GrossGBP = SafeConvertToDecimal(GetFieldValue(oSopRecord, "ITEMS_NET")),
                                TotalVAT = SafeConvertToDecimal(GetFieldValue(oSopRecord, "ITEMS_TAX")),
                                Deduction = GetFieldValue(oSopRecord, "NETVALUE_DISCOUNT"),
                                Description = GetFieldValue(oSopRecord, "NETVALUE_DESCRIPTION"),
                                //status = MapSageToWooStatus(SafeConvertToInt32(GetFieldValue(oSopRecord, "STATUS"))),
                                // Initialize or set other properties as needed
                            };
                            string invoiceNumber = GetFieldValue(oSopRecord, "INVOICE_NUMBER");
                            string accountRef = GetFieldValue(oSopRecord, "ACCOUNT_REF");
                            string name = GetFieldValue(oSopRecord, "NAME");
                            string orderDate = GetFieldValue(oSopRecord, "ORDER_DATE");

                            // Display or process header details here
                            Console.WriteLine($"Invoice Number: {invoiceNumber}, Account Ref: {accountRef}, Name: {name}, Order Date: {orderDate}");

                            oSopItem_Read = (SageDataObject310.InvoiceItem)oSopRecord.Link;
                            // Move to the first item in the sales order
                            if (oSopItem_Read.MoveFirst())
                            {
                                do
                                {
                                    var orderItem = new OrderItem
                                    {
                                        ProductCode = GetItemFieldValue(oSopItem_Read, "STOCK_CODE"),
                                        Description = GetItemFieldValue(oSopItem_Read, "DESCRIPTION"),
                                        Quantity = SafeConvertToInt32(GetItemFieldValue(oSopItem_Read, "QTY_ORDER")),
                                        PricePerUnit = SafeConvertToDecimal(GetItemFieldValue(oSopItem_Read, "UNIT_PRICE")),
                                        NetAmount = SafeConvertToDecimal(GetItemFieldValue(oSopItem_Read, "NET_AMOUNT")),
                                        VAT = SafeConvertToDecimal(GetItemFieldValue(oSopItem_Read, "TAX_AMOUNT")),
                                    };

                                    salesOrder.Items.Add(orderItem);
                                }
                                while (oSopItem_Read.MoveNext());
                                SopRecords.Add(salesOrder);
                            }
                            //while (oSopRecord.MoveNext()) ;
                        }
                        catch (Exception ex)
                        {
                            LogManager.Instance.LogMessage("Error while getting order from sage Error: " + ex.Message, "Sales");
                            LogManager.Instance.LogException(ex);
                        }
                    }
                } while (oSopRecord.MoveNext());

                return SopRecords;

            }
            catch (Exception ex)
            {
                LogManager.Instance.LogMessage("Error while getting products from sage Error: " + ex.Message, "Product");
                LogManager.Instance.LogException(ex);
                return new List<SalesOrderInfo>();
            }
            finally
            {
                _sageConnectionManager.Disconnect(oWS);
                System.Runtime.InteropServices.Marshal.ReleaseComObject(oSopRecord);
                oSopRecord = null;
                System.Runtime.InteropServices.Marshal.ReleaseComObject(oSopItem_Read);
                oSopItem_Read = null;
            }
        }
        private decimal SafeConvertToDecimal(string value)
        {
            return decimal.TryParse(value, out decimal result) ? result : 0m;
        }
        private int SafeConvertToInt32(string value)
        {
            return int.TryParse(value, out int result) ? result : 0;
        }
        private int? SafeConvertToNullableInt32(string value)
        {
            return int.TryParse(value, out int result) ? result : (int?)null;
        }

        private DateTime? SafeConvertToNullableDateTime(string value)
        {
            return DateTime.TryParse(value, out DateTime result) ? result : (DateTime?)null;
        }
        private string GetFieldValue(SageDataObject310.InvoiceRecord sopRecord, string fieldName, bool splitName = false)
        {
            try
            {
                var fieldValue = sopRecord.Fields.Item(fieldName)?.Value?.ToString();
                return fieldValue ?? string.Empty; // Return empty string if value is null
            }
            catch (Exception ex)
            {
                //_logger.LogWarning("Error retrieving field '{FieldName}': {Message}", fieldName, ex.Message);
                return string.Empty; // Return empty string if an error occurs
            }
        }
        private string GetItemFieldValue(SageDataObject310.InvoiceItem sopRecord, string fieldName, bool splitName = false)
        {
            try
            {
                var fieldValue = sopRecord.Fields.Item(fieldName)?.Value?.ToString();
                return fieldValue ?? string.Empty; // Return empty string if value is null
            }
            catch (Exception ex)
            {
                //_logger.LogWarning("Error retrieving field '{FieldName}': {Message}", fieldName, ex.Message);
                return string.Empty; // Return empty string if an error occurs
            }
        }
    }
}
