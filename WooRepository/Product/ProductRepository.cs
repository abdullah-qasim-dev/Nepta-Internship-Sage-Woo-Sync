using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using SageDataObject310;
using SageIntegration.Configuration;
using SageIntegration.Models;
using SageIntegration.SageRepository.Product;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WooCommerceNET.WooCommerce.v3;

namespace SageIntegration.WooRepository.Product
{
    public class ProductRepository : IProductRepository
    {
        private readonly WCObject _wc;

        private readonly SchedulingSettings _schedulingSettings;
        private readonly IConfiguration _configuration;
        public ProductRepository(string url, string key, string secret, SchedulingSettings schedulingOptions, IConfiguration configuration)
        {
            var rest = new WooCommerceNET.RestAPI(url, key, secret);
            _wc = new WCObject(rest);
            _schedulingSettings = schedulingOptions;
            _configuration= configuration;
        }

        public async Task<WooCommerceNET.WooCommerce.v3.Product> AddAsync(SageProductInfo sageProduct)
        {
            var username = _configuration["Sage:UserName"];
            LogManager.Instance.LogMessage("Adding Product:" + sageProduct.ProductCode, "Product");
            if (sageProduct.Price <= 0)
                sageProduct.Price = 0;

            // Safely map Sage fields to WooCommerce fields, handling potential nulls and defaults
            var newProduct = new WooCommerceNET.WooCommerce.v3.Product
            {
                
                name = sageProduct.Description +" - "+ username ?? "No description available", // Required field
                description = sageProduct.Description ?? "No description available", // Fallback if description is null
                sku = sageProduct.ProductCode, // Required field, mapped to SKU
                type = "simple", 
                regular_price = (sageProduct.Price)/2, // Fallback if Price is invalid
                stock_quantity = sageProduct.FreeStock >= 0 ? Convert.ToInt32(sageProduct.FreeStock) : 0, // Fallback to 0 if InStock is null/invalid
                manage_stock = true, // Enable stock management in WooCommerce
                stock_status = sageProduct.InStock > 0 ? "instock" : "outofstock", // Set based on InStock value
                tax_class= "Standard Rate",
                categories = new List<ProductCategoryLine>
                {
                    new ProductCategoryLine { id = 1, name = "Default Category" } // Replace with actual category IDs
                },
                attributes = new List<ProductAttributeLine>
        {
            new ProductAttributeLine
            {
                name = "Unit of Sale",
                options = new List<string> { sageProduct.UnitOfSale ?? "Each" } // Default to "Each" if null
            },
            new ProductAttributeLine
            {
                name = "Country of Origin",
                options = new List<string> { sageProduct.CountryCode ?? "GB" } // Default to "Unknown" if null
            }
        },
                meta_data = new List<WooCommerceNET.WooCommerce.v2.ProductMeta>
        {
            // Additional metadata, with null checks
            new WooCommerceNET.WooCommerce.v2.ProductMeta { key = "ItemType", value = sageProduct.ItemType ?? "N/A" },
            new WooCommerceNET.WooCommerce.v2.ProductMeta { key = "Category", value = sageProduct.Category ?? "N/A" },
            new WooCommerceNET.WooCommerce.v2.ProductMeta { key = "SupplierAccount", value = sageProduct.SupplierAccount ?? "N/A" },
            new WooCommerceNET.WooCommerce.v2.ProductMeta { key = "TaxCode", value = sageProduct.TaxCode ?? "N/A" },
            new WooCommerceNET.WooCommerce.v2.ProductMeta { key = "PartNo", value = sageProduct.PartNo ?? "N/A" },
            new WooCommerceNET.WooCommerce.v2.ProductMeta { key = "Department", value = sageProduct.Department ?? "N/A" },
            new WooCommerceNET.WooCommerce.v2.ProductMeta { key = "LastOrderDate", value = sageProduct.LastOrderDate?.ToString("o") ?? "N/A" }, // ISO format, or "N/A" if null
            new WooCommerceNET.WooCommerce.v2.ProductMeta { key = "StockTakeDate", value = sageProduct.StockTakeDate?.ToString("o") ?? "N/A" } // ISO format, or "N/A" if null
        }
            };

            try
            {
                // Update the customer in WooCommerce
                return await _wc.Product.Add(newProduct);
            }
            catch (Exception ex)
            {
                LogManager.Instance.LogMessage("Error While Adding Product:" + sageProduct.ProductCode + "ERROR:" + ex.Message, "Product");
                LogManager.Instance.LogException(ex, "Product");
                return new WooCommerceNET.WooCommerce.v3.Product();
                //throw new Exception($"Error updating customer in WooCommerce: {ex.Message}", ex);
            }
            // Send the product to WooCommerce and return the response
        }
        private string GetFieldValue(SageDataObject310.StockRecord stockRecord, string fieldName, bool splitName = false)
        {
            try
            {
                var fieldValue = stockRecord.Fields.Item(fieldName)?.Value?.ToString();
                return fieldValue ?? string.Empty; // Return empty string if value is null
            }
            catch (Exception ex)
            {
                //_logger.LogWarning("Error retrieving field '{FieldName}': {Message}", fieldName, ex.Message);
                return string.Empty; // Return empty string if an error occurs
            }
        }
        public async Task<List<WooCommerceNET.WooCommerce.v3.Product>> GetAll()
        {
            try
            {
                List<WooCommerceNET.WooCommerce.v3.Product> allProducts = new List<WooCommerceNET.WooCommerce.v3.Product>();

                // Get the last run time for filtering new or modified products
                DateTime lastRunTime = (DateTime)(_schedulingSettings?.LastRunTime ?? DateTime.MinValue);

                // Only proceed if the last run time is valid
                if (lastRunTime != DateTime.MinValue)
                {
                    string afterDate = lastRunTime.ToUniversalTime().ToString("yyyy-MM-ddTHH:mm:ssZ"); // ISO8601 format for 'after'
                    string modifiedAfterDate = lastRunTime.ToUniversalTime().ToString("yyyy-MM-ddTHH:mm:ssZ"); // ISO8601 format for 'modified_after'

                    int page = 1;
                    int perPage = 30; // Number of products per page

                    while (true)
                    {
                        var parameters = new Dictionary<string, string>
                {
                    { "page", page.ToString() },
                    { "per_page", perPage.ToString() },
                    { "after", afterDate }, // Limit response to resources published after lastRunTime
                    { "modified_after", modifiedAfterDate } // Limit response to resources modified after lastRunTime
                };

                        var products = await _wc.Product.GetAll(parameters);
                        if (products.Count == 0)
                        {
                            break; // Exit the loop if no more products are returned
                        }

                        allProducts.AddRange(products);
                        Console.WriteLine($"Count :" + allProducts.Count);
                        page++;
                    }
                }
                else
                {
                    // Handle case where lastRunTime is invalid (e.g., perform full fetch or log an appropriate message)
                    LogManager.Instance.LogMessage("Last run time is not valid. No products fetched.", "Product");
                }

                return allProducts; // Return the filtered list of products
            }
            catch (Exception ex)
            {
                LogManager.Instance.LogException(ex);
                LogManager.Instance.LogMessage("Error while getting products from Woo ERROR: " + ex.Message, "Product");
                return new List<WooCommerceNET.WooCommerce.v3.Product>(); // Return an empty list on error
            }
        }

        public async Task<WooCommerceNET.WooCommerce.v3.Product> GetProduct(string name)
        {
            try
            {
                var p = await _wc.Product.GetAll(new Dictionary<string, string>() {
                                { "sku", name }, // Filter by product name
                                { "per_page", "1" } // Limit to 1 result
                            });
                return p.FirstOrDefault();
            }
            catch(Exception ex)
            {
                return null;
            }
           
        }
        public async Task UpdateAsync(SageProductInfo sageProduct, WooCommerceNET.WooCommerce.v3.Product wooProduct)
        {
            LogManager.Instance.LogMessage("Updating Product:" + wooProduct.name, "Product");
            // Map fields from SageProductInfo to the existing WooCommerce product, handling nulls and defaults
            wooProduct.name = sageProduct.Description ?? "No description available"; // Required field, map ProductCode to name
            wooProduct.description = sageProduct.Description ?? "No description available"; // Fallback if description is null
            wooProduct.sku = sageProduct.ProductCode; // Required field, map ProductCode to SKU
            wooProduct.type = wooProduct.type ?? "simple"; // Assume "simple" if type is not set

            // Price and Stock
            wooProduct.regular_price = sageProduct.Price > 0 ? sageProduct.Price : wooProduct.regular_price; // Update price if valid
            wooProduct.stock_quantity = sageProduct.FreeStock >= 0 ? Convert.ToInt32(sageProduct.FreeStock) : wooProduct.stock_quantity; // Update stock quantity if valid
            wooProduct.stock_status = sageProduct.InStock > 0 ? "instock" : "outofstock"; // Update stock status based on InStock

            // Update or add categories (assuming ID and name are placeholders; replace with actual category values as needed)
            wooProduct.categories = wooProduct.categories ?? new List<ProductCategoryLine>();
            if (wooProduct.categories.Count == 0)
            {
                wooProduct.categories.Add(new ProductCategoryLine { id = 1 });
            }

            // Update or add attributes with null checks
            wooProduct.attributes = wooProduct.attributes ?? new List<ProductAttributeLine>();
            if (!wooProduct.attributes.Any(a => a.name == "Unit of Sale"))
            {
                wooProduct.attributes.Add(new ProductAttributeLine
                {
                    name = "Unit of Sale",
                    options = new List<string> { sageProduct.UnitOfSale ?? "Each" }
                });
            }
            if (!wooProduct.attributes.Any(a => a.name == "Country of Origin"))
            {
                wooProduct.attributes.Add(new ProductAttributeLine
                {
                    name = "Country of Origin",
                    options = new List<string> { sageProduct.CountryCode ?? "Unknown" }
                });
            }

            // Update or add metadata for additional fields
            wooProduct.meta_data = wooProduct.meta_data ?? new List<WooCommerceNET.WooCommerce.v2.ProductMeta>();
            UpdateOrAddMetaData(wooProduct.meta_data, "ItemType", sageProduct.ItemType);
            UpdateOrAddMetaData(wooProduct.meta_data, "Category", sageProduct.Category);
            UpdateOrAddMetaData(wooProduct.meta_data, "SupplierAccount", sageProduct.SupplierAccount);
            UpdateOrAddMetaData(wooProduct.meta_data, "TaxCode", sageProduct.TaxCode);
            UpdateOrAddMetaData(wooProduct.meta_data, "PartNo", sageProduct.PartNo);
            UpdateOrAddMetaData(wooProduct.meta_data, "Department", sageProduct.Department);
            UpdateOrAddMetaData(wooProduct.meta_data, "LastOrderDate", sageProduct.LastOrderDate?.ToString("o"));
            UpdateOrAddMetaData(wooProduct.meta_data, "StockTakeDate", sageProduct.StockTakeDate?.ToString("o"));

            try
            {
                // Update the customer in WooCommerce
                await _wc.Product.Update(Convert.ToUInt64(wooProduct.id), wooProduct);
            }
            catch (Exception ex)
            {
                LogManager.Instance.LogMessage("Error While Updating Product:" + wooProduct.name + "ERROR:" + ex.Message, "Product");
                LogManager.Instance.LogException(ex, "Product");
                //throw new Exception($"Error updating customer in WooCommerce: {ex.Message}", ex);
            }
            // Send the updated product to WooCommerce
        }

        // Helper method to add or update metadata
        private void UpdateOrAddMetaData(List<WooCommerceNET.WooCommerce.v2.ProductMeta> metaData, string key, string value)
        {
            // Only add/update if value is not null; otherwise, retain existing metadata
            if (!string.IsNullOrWhiteSpace(value))
            {
                var existingMeta = metaData.FirstOrDefault(m => m.key == key);
                if (existingMeta != null)
                {
                    existingMeta.value = value;
                }
                else
                {
                    metaData.Add(new WooCommerceNET.WooCommerce.v2.ProductMeta { key = key, value = value });
                }
            }
        }

    }
}
