using Microsoft.Extensions.Options;
using SageDataObject310;
using SageIntegration.Client;
using SageIntegration.Configuration;
using SageIntegration.Models;
using SageIntegration.SageRepository.Customer;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SageIntegration.SageRepository.Product
{
    public class ProductRepository : IProductRepository
    {
        private readonly SageConnectionManager _sageConnectionManager;
        private readonly SchedulingSettings _schedulingSettings;
        public ProductRepository(SageConnectionManager sageConnectionManager,SchedulingSettings schedulingOptions)
        {
            _sageConnectionManager = sageConnectionManager;
            _schedulingSettings = schedulingOptions;
        }
        public async Task<StockRecord> AddAsync(WooCommerceNET.WooCommerce.v3.Product wooProduct)
        {
            SageDataObject310.StockRecord oStockRecord=null;
            SageDataObject310.WorkSpace oWS = null;
            try
            {
                oWS = _sageConnectionManager.ConnectToSage();
                oStockRecord = (SageDataObject310.StockRecord)oWS.CreateObject("StockRecord");

                // Set STOCK_CODE with a fallback if wooProduct.sku is null
                oStockRecord.Fields.Item("STOCK_CODE").Value = wooProduct.sku ?? wooProduct.id.ToString();

                // Find existing stock record
                var bFlag = oStockRecord.Find(false);
                bool addEditFlag;

                // Determine whether to edit or add new
                if (bFlag)
                {
                    addEditFlag = oStockRecord.Edit();
                }
                else
                {
                    addEditFlag = oStockRecord.AddNew();
                }

                if (addEditFlag)
                {
                    LogManager.Instance.LogMessage("Mapping WooCommerce Product to Sage: " + wooProduct.sku, "Product");

                    // Directly assigning values from WooCommerce product to Sage stock record
                    oStockRecord.Fields.Item("STOCK_CODE").Value = wooProduct.sku ?? string.Empty; // Product Code
                    oStockRecord.Fields.Item("ITEM_TYPE").Value = wooProduct.meta_data.FirstOrDefault(m => m.key == "ItemType")?.value ?? "N/A"; // Item Type
                    oStockRecord.Fields.Item("DESCRIPTION").Value = wooProduct.description.Replace("<p>", "").Replace("</p>","") ?? wooProduct.name; // Description
                    oStockRecord.Fields.Item("STOCK_CAT").Value = wooProduct.categories.FirstOrDefault()?.name ?? "Default Category"; // Category
                    oStockRecord.Fields.Item("SUPP_UNIT_QTY").Value = (decimal?)1 ?? 1; // Default unit quantity as decimal
                    oStockRecord.Fields.Item("UNIT_WEIGHT").Value = wooProduct.weight != null ? Convert.ToDecimal(wooProduct.weight) : 0; // Weight
                    oStockRecord.Fields.Item("PRODUCT_BARCODE").Value = wooProduct.meta_data.FirstOrDefault(m => m.key == "BarCode")?.value ?? string.Empty; // Barcode
                    oStockRecord.Fields.Item("INACTIVE_FLAG").Value = wooProduct.stock_status == "outofstock" ? (byte)1 : (byte)0; // Inactive Flag as byte
                    oStockRecord.Fields.Item("LOCATION").Value = "Default Location"; // Default location
                    oStockRecord.Fields.Item("COUNTRY_CODE_OF_ORIGIN").Value = wooProduct.attributes.FirstOrDefault(a => a.name == "Country of Origin")?.options.FirstOrDefault() ?? "GB"; // Country Code

                    // Defaults
                    oStockRecord.Fields.Item("PURCHASE_NOMINAL_CODE").Value = wooProduct.meta_data.FirstOrDefault(m => m.key == "PurchaseNominalCode")?.value != null
                        ? Convert.ToInt32(wooProduct.meta_data.FirstOrDefault(m => m.key == "PurchaseNominalCode")?.value)
                        : (int?)null; // Purchase Nominal Code
                    oStockRecord.Fields.Item("PURCHASE_REF").Value = wooProduct.meta_data.FirstOrDefault(m => m.key == "SupplierAccount")?.value ?? "N/A"; // Supplier Account
                    oStockRecord.Fields.Item("TAX_CODE").Value = wooProduct.meta_data.FirstOrDefault(m => m.key == "TaxCode")?.value ?? "N/A"; // Tax Code
                    oStockRecord.Fields.Item("SUPPLIER_PART_NUMBER").Value = wooProduct.meta_data.FirstOrDefault(m => m.key == "PartNo")?.value ?? "N/A"; // Part No
                    oStockRecord.Fields.Item("DEPT_NUMBER").Value = wooProduct.meta_data.FirstOrDefault(m => m.key == "Department")?.value ?? "N/A"; // Department

                    // Sales Price
                    oStockRecord.Fields.Item("SALES_PRICE").Value = wooProduct.regular_price != null ? Convert.ToDecimal(wooProduct.regular_price) : 0m; // Sales Price
                    oStockRecord.Fields.Item("UNIT_OF_SALE").Value = wooProduct.meta_data.FirstOrDefault(m => m.key == "UnitOfSale")?.value ?? "N/A"; // Unit of Sale

                    // Status
                    oStockRecord.Fields.Item("QTY_IN_STOCK").Value = wooProduct.stock_quantity.HasValue ? wooProduct.stock_quantity.Value : 0; // Quantity in Stock
                    oStockRecord.Fields.Item("QTY_ALLOCATED").Value = 0; // Replace with actual logic if needed
                    oStockRecord.Fields.Item("QTY_ON_ORDER").Value = 0; // Replace with actual logic if needed
                    oStockRecord.Fields.Item("QTY_REORDER_LEVEL").Value = 0; // Replace with actual logic if needed
                    oStockRecord.Fields.Item("QTY_REORDER").Value = 0; // Replace with actual logic if needed

                    // Stock Take
                    oStockRecord.Fields.Item("STOCK_TAKE_DATE").Value = DateTime.Now; // Replace with actual logic if needed
                    oStockRecord.Fields.Item("QTY_LAST_STOCK_TAKE").Value = 0; // Replace with actual logic if needed


                    // Attempt to update the stock record and log success or failure
                    if (oStockRecord.Update())
                    {
                        LogManager.Instance.LogMessage("Mapped Sage Stock Record: " + wooProduct.sku, "Product");
                        return oStockRecord; // Successfully added/updated
                    }
                    else
                    {
                        LogManager.Instance.LogMessage("Error while updating products in Sage", "Product");
                        throw new Exception("The stock record could not be updated.");
                    }
                }
            }
            catch (Exception ex)
            {
                LogManager.Instance.LogMessage("Error while adding products to Sage: " + ex.Message, "Product");
                LogManager.Instance.LogException(ex);
                throw new Exception($"Error adding product: {ex.Message}", ex);
            }
            finally
            {
                _sageConnectionManager.Disconnect(oWS); // Ensure Sage connection is closed
                System.Runtime.InteropServices.Marshal.ReleaseComObject(oStockRecord);
                oStockRecord = null;
            }

            return null; // Explicitly return null if we reach this point (no product added)
        }


        public async Task<List<SageProductInfo>> GetAll()
        {
            LogManager.Instance.LogMessage("Getting Products from Sage","Product");
            SageDataObject310.StockRecord oStockRecord = null;
            SageDataObject310.WorkSpace oWS = null;
            List<SageProductInfo> stockRecords = new List<SageProductInfo>();
            
            try
            {
                oWS = _sageConnectionManager.ConnectToSage();

                // Create the SalesRecord object from the workspace
                oStockRecord = (SageDataObject310.StockRecord)oWS.CreateObject("StockRecord");
                oStockRecord.MoveFirst();
                do
                {
                    DateTime lastRunTime = (DateTime)(_schedulingSettings?.LastRunTime); // For example, filter for last 24 hours
                    DateTime recordModifiedDate = oStockRecord.Fields.Item("RECORD_MODIFY_DATE").Value;
                    DateTime recordCreatedDate = oStockRecord.Fields.Item("RECORD_CREATE_DATE").Value;
                    if (recordModifiedDate >= lastRunTime || recordCreatedDate >= lastRunTime)
                    {
                        try
                        {
                            stockRecords.Add(GetProductInfo(oStockRecord));
                        }
                        catch (Exception ex)
                        {
                            LogManager.Instance.LogMessage("Error while getting products from sage Error: " + ex.Message, "Product");
                            LogManager.Instance.LogException(ex);
                        }
                    }
                } while (oStockRecord.MoveNext());

                return stockRecords;

            }
            catch (Exception ex)
            {
                LogManager.Instance.LogMessage("Error while getting products from sage Error: " + ex.Message, "Product");
                LogManager.Instance.LogException(ex);
                return new List<SageProductInfo>();
            }
            finally
            {
                _sageConnectionManager.Disconnect(oWS);
                System.Runtime.InteropServices.Marshal.ReleaseComObject(oStockRecord);
                oStockRecord = null;
            }
        }
        public async  Task<SageProductInfo>  GETProduct(string sku, SageDataObject310.WorkSpace oWS)
        {
            SageDataObject310.StockRecord oStockRecord = null;
            SageProductInfo prc = new SageProductInfo();

            try
            {
                oStockRecord = (StockRecord)oWS.CreateObject("StockRecord");
                oStockRecord.Fields.Item("STOCK_CODE").Value = sku;
                var bFlag = oStockRecord.Find(false);
                if (bFlag)
                {
                    return GetProductInfo(oStockRecord);
                }
                return prc;
            }
            catch (Exception ex)
            {
                LogManager.Instance.LogMessage("Error While getting Customer from Sage ERROR:" + ex.Message, "Customer");
                LogManager.Instance.LogException(ex, "Customer");
                return new SageProductInfo();
            }
            finally
            {
                System.Runtime.InteropServices.Marshal.ReleaseComObject(oStockRecord);
                oStockRecord = null;
            }

        }
        private SageProductInfo GetProductInfo(SageDataObject310.StockRecord oStockRecord)
        {
            var stockRecord = new SageProductInfo
            {
                // Product Details
                ProductCode = GetFieldValue(oStockRecord, "STOCK_CODE", true),
                ItemType = GetFieldValue(oStockRecord, "ITEM_TYPE", true),
                Description = GetFieldValue(oStockRecord, "DESCRIPTION", true),
                Category = GetFieldValue(oStockRecord, "STOCK_CAT", true),
                SuppUnitQty = SafeConvertToDecimal(GetFieldValue(oStockRecord, "SUPP_UNIT_QTY", true)),
                Weight = SafeConvertToDecimal(GetFieldValue(oStockRecord, "UNIT_WEIGHT", true)),
                BarCode = GetFieldValue(oStockRecord, "PRODUCT_BARCODE", true),
                IsInactive = GetFieldValue(oStockRecord, "INACTIVE_FLAG", true) == "Y",
                Location = GetFieldValue(oStockRecord, "LOCATION", true),
                CountryCode = GetFieldValue(oStockRecord, "COUNTRY_CODE_OF_ORIGIN", true),

                // Defaults
                PurchaseNominalCode = SafeConvertToNullableInt32(GetFieldValue(oStockRecord, "PURCHASE_NOMINAL_CODE", true)),
                SupplierAccount = GetFieldValue(oStockRecord, "PURCHASE_REF", true),
                TaxCode = GetFieldValue(oStockRecord, "TAX_CODE", true),
                PartNo = GetFieldValue(oStockRecord, "SUPPLIER_PART_NUMBER", true),
                Department = GetFieldValue(oStockRecord, "DEPT_NUMBER", true),

                // Ordering
                LastCostPriceStandard = SafeConvertToDecimal(GetFieldValue(oStockRecord, "LAST_COST_DISCOUNT", true)),
                LastCostPriceDiscounted = SafeConvertToDecimal(GetFieldValue(oStockRecord, "LAST_COST_PRICE_DISCOUNTED", true)),
                LastOrderQty = SafeConvertToDecimal(GetFieldValue(oStockRecord, "LAST_ORDER_QUANTITY", true)),
                LastOrderDate = SafeConvertToNullableDateTime(GetFieldValue(oStockRecord, "LAST_PURCHASE_DATE", true)),

                // Sales Price
                Price = SafeConvertToDecimal(GetFieldValue(oStockRecord, "SALES_PRICE", true)),
                UnitOfSale = GetFieldValue(oStockRecord, "UNIT_OF_SALE", true),

                // Status
                InStock = SafeConvertToDecimal(GetFieldValue(oStockRecord, "QTY_IN_STOCK", true)),
                FreeStock = SafeConvertToDecimal(GetFieldValue(oStockRecord, "QTY_IN_STOCK", true)),
                Allocated = SafeConvertToDecimal(GetFieldValue(oStockRecord, "QTY_ALLOCATED", true)),
                OnOrder = SafeConvertToDecimal(GetFieldValue(oStockRecord, "QTY_ON_ORDER", true)),
                ReOrderLevel = SafeConvertToDecimal(GetFieldValue(oStockRecord, "QTY_REORDER_LEVEL", true)),
                ReOrderQty = SafeConvertToDecimal(GetFieldValue(oStockRecord, "QTY_REORDER", true)),

                // Stock Take
                StockTakeDate = SafeConvertToNullableDateTime(GetFieldValue(oStockRecord, "STOCK_TAKE_DATE", true)),
                StockTakeQuantity = SafeConvertToDecimal(GetFieldValue(oStockRecord, "QTY_LAST_STOCK_TAKE", true))
            };
            return stockRecord;
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
        private string GetFieldValue(SageDataObject310.StockRecord salesRecord, string fieldName, bool splitName = false)
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
    }
}
