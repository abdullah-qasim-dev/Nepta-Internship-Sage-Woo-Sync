using Microsoft.Extensions.Options;
using SageDataObject310;
using SageIntegration.Client;
using SageIntegration.Configuration;
using SageIntegration.SageRepository.Product;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace SageIntegration.SageRepository.SalesOrder
{
    public class SalesOrder : ISalesOrder
    {
        private readonly SageConnectionManager _sageConnectionManager;
        private readonly SchedulingSettings _schedulingSettings;
        private readonly IProductRepository _productRepository;
        public SalesOrder(SageConnectionManager sageConnectionManager, SchedulingSettings schedulingOptions,IProductRepository productRepository)
        {
            _sageConnectionManager = sageConnectionManager;
            _schedulingSettings = schedulingOptions;
            _productRepository = productRepository;
        }
        public async Task<List<SalesOrderInfo>> GetAll()
        {
            LogManager.Instance.LogMessage("Getting Sales Order from Sage", "Sales");
            SageDataObject310.SopRecord oSopRecord =null;
            SageDataObject310.SopItem oSopItem_Read = null;
            SageDataObject310.WorkSpace oWS = null;
            List<SalesOrderInfo> SopRecords = new List<SalesOrderInfo>();

            try
            {
                oWS = _sageConnectionManager.ConnectToSage();

                // Create the SalesRecord object from the workspace
                oSopRecord = (SageDataObject310.SopRecord)oWS.CreateObject("SopRecord");
                oSopItem_Read = (SageDataObject310.SopItem)oWS.CreateObject("SopItem");
                do
                {
                    try
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
                                    OrderDate = SafeConvertToNullableDateTime(GetFieldValue(oSopRecord, "ORDER_DATE")),
                                    TotalGBP = SafeConvertToDecimal(GetFieldValue(oSopRecord, "BASE_TOT_NET")),
                                    CarriageGBP = SafeConvertToDecimal(GetFieldValue(oSopRecord, "CARR_NET")),
                                    GrossGBP = SafeConvertToDecimal(GetFieldValue(oSopRecord, "ITEMS_NET")),
                                    Deduction = GetFieldValue(oSopRecord, "NETVALUE_DISCOUNT"),
                                    Description = GetFieldValue(oSopRecord, "NETVALUE_DESCRIPTION"),
                                    status = MapSageToWooStatus(SafeConvertToInt32(GetFieldValue(oSopRecord, "STATUS"))),
                                    // Initialize or set other properties as needed
                                };
                                string invoiceNumber = GetFieldValue(oSopRecord, "INVOICE_NUMBER");
                                string accountRef = GetFieldValue(oSopRecord, "ACCOUNT_REF");
                                string name = GetFieldValue(oSopRecord, "NAME");
                                string orderDate = GetFieldValue(oSopRecord, "ORDER_DATE");

                                // Display or process header details here
                                Console.WriteLine($"Invoice Number: {invoiceNumber}, Account Ref: {accountRef}, Name: {name}, Order Date: {orderDate}");

                                oSopItem_Read = (SageDataObject310.SopItem)oSopRecord.Link;
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
                                        var sss = GetItemFieldValue(oSopItem_Read, "TAX_CODE");
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
                    }
                    catch(Exception ex)
                    {

                        LogManager.Instance.LogMessage("Error while getting order from sage Error: " + ex.Message, "Sales");
                        LogManager.Instance.LogException(ex);
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
        //public async Task<SopRecord> AddOrUpdateOrder(WooCommerceNET.WooCommerce.v3.Order wooOrder, WooCommerceNET.WooCommerce.v3.Customer customer)
        //{
        //    SageDataObject310.SopRecord oSopRecord;
        //    SageDataObject310.WorkSpace oWS = null;
        //    try
        //    {
        //        oWS = _sageConnectionManager.ConnectToSage();
        //        oSopRecord = (SageDataObject310.SopRecord)oWS.CreateObject("oSopP");

        //        // Set ORDER_NUMBER with a fallback if wooOrder.id is null
        //        oSopRecord.Fields.Item("ORDER_NUMBER").Value = wooOrder.number ?? wooOrder.id.ToString();

        //        // Check if the order exists
        //        bool orderExists = oSopRecord.Find(false);
        //        bool addEditFlag;

        //        // Determine whether to edit or add new
        //        if (orderExists)
        //        {
        //            addEditFlag = oSopRecord.Edit();
        //        }
        //        else
        //        {
        //            addEditFlag = oSopRecord.AddNew();
        //        }

        //        if (addEditFlag)
        //        {
        //            LogManager.Instance.LogMessage("Mapping WooCommerce Order to Sage: " + wooOrder.id, "Order");

        //            // Assign values from WooCommerce order to Sage order record
        //            oSopRecord.Fields.Item("ACCOUNT_REF").Value = customer.meta_data
        //                .FirstOrDefault(m => m.key == "AccountRef")?.value?.ToString() ?? "";
        //            oSopRecord.Fields.Item("ORDER_DATE").Value = wooOrder.date_created_gmt ?? DateTime.Now; // Order date
        //            oSopRecord.Fields.Item("CUST_ORDER_NUMBER").Value = wooOrder.number; // Customer Order Number
        //            oSopRecord.Fields.Item("NAME").Value = $"{wooOrder.billing.first_name} {wooOrder.billing.last_name}"; // Customer Name
        //            oSopRecord.Fields.Item("ADDRESS_1").Value = wooOrder.billing.address_1 ?? string.Empty;
        //            oSopRecord.Fields.Item("ADDRESS_2").Value = wooOrder.billing.address_2 ?? string.Empty;
        //            oSopRecord.Fields.Item("ADDRESS_3").Value = wooOrder.billing.city ?? string.Empty;
        //            oSopRecord.Fields.Item("ADDRESS_4").Value = wooOrder.billing.state ?? string.Empty;
        //            oSopRecord.Fields.Item("ADDRESS_5").Value = wooOrder.billing.postcode ?? string.Empty;
        //            oSopRecord.Fields.Item("CUST_TEL_NUMBER").Value = wooOrder.billing.phone ?? string.Empty;
        //            oSopRecord.Fields.Item("CURRENCY").Value = wooOrder.currency ?? "USD"; // Currency

        //            // Add or Update Order Line Items
        //            foreach (var item in wooOrder.line_items)
        //            {

        //                SageDataObject310.SopItem oSopItem = (SageDataObject310.SopItem)oWS.CreateObject("SopItem");
        //                oSopItem.Fields.Item("STOCK_CODE").Value = item.sku ?? item.product_id.ToString(); // Stock code
        //                oSopItem.Fields.Item("DESCRIPTION").Value = item.name; // Description
        //                oSopItem.Fields.Item("QTY_ORDER").Value = item.quantity; // Quantity ordered
        //                oSopItem.Fields.Item("UNIT_PRICE").Value = item.price; // Unit price
        //                oSopItem.Fields.Item("NET_AMOUNT").Value = item.total; // Net amount
        //                oSopItem.Fields.Item("TAX_AMOUNT").Value = item.total_tax; // Tax amount
        //                oSopItem.Fields.Item("TAX_CODE").Value = item.tax_class ?? "Standard"; // Tax code

        //                // Link item to order
        //                oSopRecord.Link.Add(oSopItem);
        //            }

        //            // Attempt to update the order record and log success or failure
        //            if (oSopRecord.Update())
        //            {
        //                LogManager.Instance.LogMessage("Mapped Sage Order Record: " + wooOrder.id, "Order");
        //                return oSopRecord; // Successfully added/updated
        //            }
        //            else
        //            {
        //                LogManager.Instance.LogMessage("Error while updating order in Sage", "Order");
        //                throw new Exception("The order record could not be updated.");
        //            }
        //        }
        //    }
        //    catch (Exception ex)
        //    {
        //        LogManager.Instance.LogMessage("Error while adding order to Sage: " + ex.Message, "Order");
        //        LogManager.Instance.LogException(ex);
        //        throw new Exception($"Error adding order: {ex.Message}", ex);
        //    }
        //    finally
        //    {
        //        _sageConnectionManager.Disconnect(oWS); // Ensure Sage connection is closed
        //    }

        //    return null; // Explicitly return null if we reach this point (no order added)
        //}
        public async Task<SageDataObject310.SopRecord> AddOrUpdateOrder(WooCommerceNET.WooCommerce.v3.Order wooOrder, WooCommerceNET.WooCommerce.v3.Customer customer)
        {
            SageDataObject310.SopPost oSopPost = null;
            SageDataObject310.SopRecord oSopRecord = null;
            SageDataObject310.WorkSpace oWS = null;
            SageDataObject310.SopItem oSopItem;
            try
            {
                oWS = _sageConnectionManager.ConnectToSage();
                
                oSopRecord = (SageDataObject310.SopRecord)oWS.CreateObject("SOPRecord");

                // Get next available order number from Sage
                //string orderNumber = oSopPost.GetNextNumber();

                // Set ORDER_NUMBER using WooCommerce order ID or new order number from Sage
                //oSopPost.Fields.Item("ORDER_NUMBER").Value = wooOrder.number ?? orderNumber;

                var sageOrderNumberMeta = wooOrder.meta_data?
            .FirstOrDefault(meta => meta.key == "sage_order_number")?.value?.ToString();

                // Use sage_order_number from WooCommerce if found, otherwise use WooCommerce order number or ID
                var orderNumber = sageOrderNumberMeta ?? wooOrder.number ?? wooOrder.id.ToString();
                // Check if the order exists (based on ORDER_NUMBER)
                oSopRecord.Fields.Item("ORDER_NUMBER").Value = orderNumber;
                bool orderExists = oSopRecord.Find(false);
                bool addEditFlag;

                // Determine whether to add or edit the order
                if (orderExists)
                {
                    _sageConnectionManager.Disconnect(oWS); // Ensure Sage connection is closed
                    System.Runtime.InteropServices.Marshal.ReleaseComObject(oSopRecord);
                    oSopRecord = null;
                    //System.Runtime.InteropServices.Marshal.ReleaseComObject(oSopPost);
                    //oSopPost = null;
                    return null;
                    //addEditFlag = oSopRecord.Edit();
                   // oSopPost = (SageDataObject310.SopPost)oWS.CreateObject("SOPPost");
                }
                else
                {
                    addEditFlag= true;
                    oSopPost = (SageDataObject310.SopPost)oWS.CreateObject("SOPPost");
                }

                if (addEditFlag)
                {
                    LogManager.Instance.LogMessage("Mapping WooCommerce Order to Sage: " + wooOrder.id, "Order");

                    // Assign values from WooCommerce order to Sage order header fields
                    oSopPost.Header("ACCOUNT_REF").Value = customer.meta_data?
                        .FirstOrDefault(m => m.key == "AccountRef")?.value?.ToString() ?? "";
                   oSopPost.Header("ORDER_DATE").Value = wooOrder.date_created_gmt ?? DateTime.Now;
                   oSopPost.Header("ORDER_NUMBER").Value = wooOrder.number ?? wooOrder.id.ToString();
                   oSopPost.Header("CUST_ORDER_NUMBER").Value = wooOrder.number;
                   oSopPost.Header("NAME").Value = $"{wooOrder.billing.first_name} {wooOrder.billing.last_name}";
                   oSopPost.Header("ADDRESS_1").Value = wooOrder.billing.address_1 ?? string.Empty;
                   oSopPost.Header("ADDRESS_2").Value = wooOrder.billing.address_2 ?? string.Empty;
                   oSopPost.Header("ADDRESS_3").Value = wooOrder.billing.city ?? string.Empty;
                   oSopPost.Header("ADDRESS_4").Value = wooOrder.billing.state ?? string.Empty;
                   oSopPost.Header("ADDRESS_5").Value = wooOrder.billing.postcode ?? string.Empty;
                   oSopPost.Header("CUST_TEL_NUMBER").Value = wooOrder.billing.phone ?? string.Empty;
                   oSopPost.Header("CURRENCY").Value = wooOrder.currency ?? "USD";
                   oSopPost.Header("STATUS").Value = MapWooToSageStatus(wooOrder.status);
                    // Clear existing line items if updating the order
                    if (orderExists)
                    {
                        SageDataObject310.SopItem oSopItem_Read = (SageDataObject310.SopItem)oWS.CreateObject("SopItem");
                        //oSopItem_Read = (SageDataObject310.SopItem)oSopRecord.Link;
                        //oSopItem_Read.MoveFirst();
                        //while (!oSopItem_Read.IsDeleted())
                        //{
                        //    if (!oSopItem_Read.MoveNext())
                        //    {
                        //        break;
                        //    }
                        //}

                        //oSopItem_Read.MoveFirst();
                        try
                        {
                            oSopItem_Read = (SageDataObject310.SopItem)oSopRecord.Link;
                            oSopItem_Read.MoveFirst();

                            bool hasMoreItems = true;
                            while (hasMoreItems)
                            {
                                // Process each item, if it's deleted or perform other tasks as necessary
                                if (!oSopItem_Read.IsDeleted())
                                {
                                    // Perform any processing needed for non-deleted items here
                                }

                                // Move to the next item, and if MoveNext() fails, end the loop
                                hasMoreItems = oSopItem_Read.MoveNext();
                            }
                        }
                        catch (Exception ex)
                        {
                            LogManager.Instance.LogException(ex);
                        }
                        finally
                        {
                            if (oSopItem_Read != null)
                            {
                                System.Runtime.InteropServices.Marshal.ReleaseComObject(oSopItem_Read);
                                oSopItem_Read = null;
                            }
                        }
                        //oSopPost.Items.MoveFirst(); // Navigate to the first line item
                        //while (oSopPost.Items.MoveNext()) // Loop through existing items
                        //{
                        //    if (oSopPost.Items.IsDeleted) // Check if the item is marked as deleted
                        //    {
                        //        //oSopPost.Items.Delete(); // Delete the item
                        //    }
                        //}
                    }

                    // Add or update line items from WooCommerce
                    foreach (var item in wooOrder.line_items)
                    {
                        oSopItem = oSopPost.Items.Add();
                        // = (SageDataObject310.SopItem)oWS.CreateObject("SopItem");
                        var product = await _productRepository.GETProduct(item.sku,oWS);
                        oSopItem.Fields.Item("STOCK_CODE").Value = product.ProductCode;
                        oSopItem.Fields.Item("DESCRIPTION").Value = product.Description;
                        //oSopItem.Fields.Item("NOMINAL_CODE").Value = product.PurchaseNominalCode;
                        oSopItem.Fields.Item("NOMINAL_CODE").Value = "4000";
                        oSopItem.Fields.Item("QTY_ORDER").Value = item.quantity;
                        oSopItem.Fields.Item("UNIT_PRICE").Value = item.price;
                        oSopItem.Fields.Item("NET_AMOUNT").Value = item.total;
                        oSopItem.Fields.Item("TAX_AMOUNT").Value = item.total_tax;
                        oSopItem.Fields.Item("TAX_CODE").Value =  "1";
                        oSopItem.Fields.Item("TAX_FLAG").Value = "1";

                    }

                    // Attempt to update the order record and log success or failure
                    if (oSopPost.Update())
                    {
                        LogManager.Instance.LogMessage("Mapped Sage Order Record: " + wooOrder.id, "Order");
                        return oSopRecord; // Successfully added/updated
                    }
                    else
                    {
                        LogManager.Instance.LogMessage("Error while updating order in Sage", "Order");
                        throw new Exception("The order record could not be updated.");
                    }
                }
            }
            catch (Exception ex)
            {
                LogManager.Instance.LogMessage("Error while adding order to Sage: " + ex.Message, "Order");
                LogManager.Instance.LogException(ex);
                throw new Exception($"Error adding order: {ex.Message}", ex);
            }
            finally
            {
                _sageConnectionManager.Disconnect(oWS); // Ensure Sage connection is closed
                System.Runtime.InteropServices.Marshal.ReleaseComObject(oSopRecord);
                oSopRecord = null;
                System.Runtime.InteropServices.Marshal.ReleaseComObject(oSopPost);
                oSopPost = null;

            }

            return null; // Explicitly return null if we reach this point (no order added)
        }


        public async Task<InvoicePost> AddOrUpdateOrderAsync(WooCommerceNET.WooCommerce.v3.Order wooOrder, WooCommerceNET.WooCommerce.v3.Customer customer)
        {
            SageDataObject310.InvoicePost oInvoicePost;
            SageDataObject310.WorkSpace oWS = null;
            try
            {
                oWS = _sageConnectionManager.ConnectToSage();
                oInvoicePost = (SageDataObject310.InvoicePost)oWS.CreateObject("InvoicePost");

                // Set the invoice type to standard product invoice
                oInvoicePost.Type = (SageDataObject310.InvoiceType)SageDataObject310.LedgerType.sdoLedgerInvoice;

                // Populate Invoice Header with WooCommerce customer and order information
                oInvoicePost.Header("ACCOUNT_REF").Value = customer.meta_data
                    .FirstOrDefault(m => m.key == "AccountRef")?.value?.ToString() ?? "";
                oInvoicePost.Header("NAME").Value = $"{wooOrder.billing.first_name} {wooOrder.billing.last_name}";
                oInvoicePost.Header("ADDRESS_1").Value = wooOrder.billing.address_1 ?? string.Empty;
                oInvoicePost.Header("ADDRESS_2").Value = wooOrder.billing.address_2 ?? string.Empty;
                oInvoicePost.Header("ADDRESS_3").Value = wooOrder.billing.city ?? string.Empty;
                oInvoicePost.Header("ADDRESS_4").Value = wooOrder.billing.state ?? string.Empty;
                oInvoicePost.Header("ADDRESS_5").Value = wooOrder.billing.postcode ?? string.Empty;
                oInvoicePost.Header("CUST_TEL_NUMBER").Value = wooOrder.billing.phone ?? string.Empty;
                oInvoicePost.Header("INVOICE_DATE").Value = wooOrder.date_created_gmt ?? DateTime.Now;
                oInvoicePost.Header("ORDER_NUMBER").Value = wooOrder.number ?? wooOrder.id.ToString();
                

                // Loop through each line item in WooCommerce order and add as invoice item
                foreach (var item in wooOrder.line_items)
                {
                    SageDataObject310.InvoiceItem oInvoiceItem = oInvoicePost.Items.Add();

                    oInvoiceItem.Fields.Item("STOCK_CODE").Value = item.sku;
                    oInvoiceItem.Fields.Item("QTY_ORDER").Value = item.quantity;
                    oInvoiceItem.Fields.Item("UNIT_PRICE").Value = item.price;
                    oInvoiceItem.Fields.Item("NET_AMOUNT").Value = item.total;
                    oInvoiceItem.Fields.Item("TAX_AMOUNT").Value = item.total_tax;
                    oInvoiceItem.Fields.Item("TAX_CODE").Value = item.tax_class ?? "Standard";
                    oInvoiceItem.Fields.Item("FULL_NET_AMOUNT").Value = item.total;
                }

                // Update the Invoice
                if (oInvoicePost.Update())
                {
                    LogManager.Instance.LogMessage("Invoice Posted Successfully: " + wooOrder.id, "Invoice");
                    return oInvoicePost; // Successfully added/updated
                }
                else
                {
                    LogManager.Instance.LogMessage("Error while updating invoice in Sage", "Invoice");
                    throw new Exception("The invoice record could not be updated.");
                }
            }
            catch (Exception ex)
            {
                LogManager.Instance.LogMessage("Error while adding invoice to Sage: " + ex.Message, "Invoice");
                LogManager.Instance.LogException(ex);
                throw new Exception($"Error adding invoice: {ex.Message}", ex);
            }
            finally
            {
                _sageConnectionManager.Disconnect(oWS); // Ensure Sage connection is closed
            }

            return null; // Explicitly return null if we reach this point (no invoice added)
        }


        private decimal SafeConvertToDecimal(string value)
        {
            return decimal.TryParse(value, out decimal result) ? result : 0m;
        }
        private int MapWooToSageStatus(string wooStatus)
        {
            switch (wooStatus.ToLower())
            {
                case "pending":
                    return 0; // sdoZeroAlloc - Blank, Blank
                case "processing":
                    return 1; // sdoPartAlloc - Part, Blank
                case "cancelled":
                    return 3; // sdoCancelled - Cancelled, Blank
                case "completed":
                    return 8; // sdoComplete - Blank, Complete
                case "on-hold":
                    return 9; // sdoHeldPart - Held, Part or sdoHeld - Held, Blank
                default:
                    return 0; // Default to 0 (sdoZeroAlloc) if the WooCommerce status is unrecognized
            }
        }

        private string MapSageToWooStatus(int sageStatus)
        {
            switch (sageStatus)
            {
                case 0: // sdoZeroAlloc - Blank, Blank
                    return "pending";
                case 1: // sdoPartAlloc - Part, Blank
                    return "processing";
                case 2: // sdoFullAlloc - Full, Blank
                    return "processing";
                case 3: // sdoCancelled - Cancelled, Blank
                    return "cancelled";
                case 4: // sdoPartZeroAlloc - Blank, Part
                    return "processing";
                case 5: // sdoPartPartAlloc - Part, Part
                    return "processing";
                case 6: // sdoPartFullAlloc - Full, Part
                    return "processing";
                case 7: // sdoPartCancelled - Cancel, Part
                    return "cancelled";
                case 8: // sdoComplete - Blank, Complete
                    return "completed";
                case 9: // sdoHeldPart - Held, Part
                case 10: // sdoHeld - Held, Blank
                    return "on-hold";
                default:
                    return "pending"; // Default to pending if the status is unrecognized
            }
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
        private string GetFieldValue(SageDataObject310.SopRecord sopRecord, string fieldName, bool splitName = false)
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
        private string GetItemFieldValue(SageDataObject310.SopItem sopRecord, string fieldName, bool splitName = false)
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
