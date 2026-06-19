using Microsoft.Extensions.Options;
using SageIntegration.Configuration;
using SageIntegration.SageRepository.SalesOrder;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WooCommerceNET.WooCommerce.v3;

namespace SageIntegration.WooRepository.Order
{
    public class Order : IOrder
    {
        private readonly WCObject _wc;
        private readonly SchedulingSettings _schedulingSettings;

        public Order(string url, string key, string secret, SchedulingSettings schedulingOptions)
        {
            var rest = new WooCommerceNET.RestAPI(url, key, secret);
            _wc = new WCObject(rest);
            _schedulingSettings = schedulingOptions;

        }
        public async Task AddAsync(SalesOrderInfo order, WooCommerceNET.WooCommerce.v3.Customer? Customer)
        {
            var newOrder = new WooCommerceNET.WooCommerce.v3.Order
            {
                //id = (ulong?)order.OrderNumber,
                currency = "GBP",
                status = order.status,
                total = order.TotalGBP,
                total_tax = order.Items.Sum(item => item.VAT),
                customer_id = Customer?.id == null ? 0 : Customer.id,
                date_created = order.OrderDate,
                billing = new WooCommerceNET.WooCommerce.v2.OrderBilling
                {
                    first_name = Customer?.first_name,
                    last_name = Customer?.last_name,
                    address_1 = Customer?.billing?.address_1,
                    address_2 = Customer?.billing?.address_2,
                    city = Customer?.billing?.city,
                    state = Customer?.billing?.state,
                    country = "GB",
                    phone = Customer?.billing?.phone,
                    postcode = Customer?.billing?.postcode,
                },
                line_items = order.Items.Select(item => new WooCommerceNET.WooCommerce.v2.OrderLineItem
                {
                    product_id = item.ProductID,
                    name = item.Description,
                    quantity = item.Quantity,
                    subtotal = (item.Quantity * item.PricePerUnit),
                    total = item.NetAmount,
                    subtotal_tax = item.VAT, 
                    total_tax = item.VAT 
                }).ToList(),
                shipping_lines = new List<WooCommerceNET.WooCommerce.v2.OrderShippingLine>
                {
                    new WooCommerceNET.WooCommerce.v2.OrderShippingLine
                    {
                        method_title = "Standard Shipping",
                        method_id = order.OrderNumber.ToString() ,
                        total = order.CarriageGBP
                    }
                },
                meta_data = new List<WooCommerceNET.WooCommerce.v2.OrderMeta>
                {
                    new WooCommerceNET.WooCommerce.v2.OrderMeta
                    {
                        key = "sage_order_number",
                        value = order.OrderNumber.ToString()
                    }
                }
            };

            await _wc.Order.Add(newOrder);
        }
        public async Task<WooCommerceNET.WooCommerce.v3.Order> GetOrderById(ulong id, ulong orderNumer)
        {
            try
            {
                int page = 1;
                int perPage = 50; // Number of orders per page
                int maxPages = 20; // Set a reasonable limit to avoid excessive requests
                WooCommerceNET.WooCommerce.v3.Order foundOrder = null;

                while (page <= maxPages)
                {
                    var parameters = new Dictionary<string, string>
                {
                    { "customer", id.ToString() },    // Use filters as needed to narrow down results
                    { "orderby", "date" },
                    { "order", "desc" },
                    { "page", page.ToString() },
                    { "per_page", perPage.ToString() }
                };

                    var orders = await _wc.Order.GetAll(parameters);

                    // Check if any orders were retrieved
                    if (orders.Count == 0)
                    {
                        return null;
                    }

                    // Search for the order with the specific parent_id in the current batch
                    foundOrder = orders.FirstOrDefault(o => o.meta_data.Any(meta => meta.key == "sage_order_number" && meta.value.Equals(orderNumer.ToString())));

                    if (foundOrder != null)
                    {
                        return foundOrder;
                    }

                    page++; // Move to the next page
                }
                return null;
            }
            catch (Exception ex) {
                return null;
            }
        }
        public async Task UpdateAsync(SalesOrderInfo updatedOrderInfo, WooCommerceNET.WooCommerce.v3.Order existingOrder, WooCommerceNET.WooCommerce.v3.Customer? Customer)
        {
            existingOrder.currency = "GBP";  // Set the currency
            existingOrder.status = updatedOrderInfo.status;  // Set the currency
            existingOrder.total = updatedOrderInfo.TotalGBP;  // Update the total amount
            existingOrder.customer_id = Customer?.id ?? 0;  // Update the customer ID
            existingOrder.total_tax = updatedOrderInfo.Items.Sum(item => item.VAT);
            // Update billing information
            existingOrder.billing = new WooCommerceNET.WooCommerce.v2.OrderBilling
            {
                first_name = Customer?.first_name,
                last_name = Customer?.last_name,
                address_1 = Customer?.billing?.address_1,
                address_2 = Customer?.billing?.address_2,
                city = Customer?.billing?.city,
                state = Customer?.billing?.state,
                country = "GB",
                phone = Customer?.billing?.phone,
                postcode = Customer?.billing?.postcode,
            };

            // Update line items
            foreach (var updatedItem in updatedOrderInfo.Items)
            {
                // Check if the item already exists in the current line items by product ID
                var existingLineItem = existingOrder.line_items.FirstOrDefault(li => li.product_id == updatedItem.ProductID);

                if (existingLineItem != null)
                {
                    // Update the existing line item
                    existingLineItem.name = updatedItem.Description;
                    existingLineItem.quantity = updatedItem.Quantity;
                    existingLineItem.subtotal = (updatedItem.Quantity * updatedItem.PricePerUnit);
                    existingLineItem.total = updatedItem.NetAmount;
                    existingLineItem.subtotal_tax = updatedItem.VAT;
                    existingLineItem.total_tax = updatedItem.VAT;
                }
                else
                {
                    // Add new line item as it doesn't exist in the current order
                    existingOrder.line_items.Add(new WooCommerceNET.WooCommerce.v2.OrderLineItem
                    {
                        product_id = updatedItem.ProductID,
                        name = updatedItem.Description,
                        quantity = updatedItem.Quantity,
                        subtotal = (updatedItem.Quantity * updatedItem.PricePerUnit),
                        total = updatedItem.NetAmount,
                        subtotal_tax = updatedItem.VAT,
                        total_tax = updatedItem.VAT,
                    });
                }
            }

            // Update shipping information
            //existingOrder.shipping_lines = new List<WooCommerceNET.WooCommerce.v2.OrderShippingLine>
            //{
            //    new WooCommerceNET.WooCommerce.v2.OrderShippingLine
            //    {
            //       method_title = "Standard Shipping",
            //       method_id = updatedOrderInfo.OrderNumber.ToString() ,
            //       total = updatedOrderInfo.CarriageGBP
            //    }
            //};
            // Use the WooCommerce API to update the order
            await _wc.Order.Update((ulong)existingOrder.id, existingOrder);
        }
        public async Task<List<WooCommerceNET.WooCommerce.v3.Order>> GetAll()
        {
            try
            {
                List<WooCommerceNET.WooCommerce.v3.Order> allOrders = new List<WooCommerceNET.WooCommerce.v3.Order>();

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

                        var products = await _wc.Order.GetAll(parameters);
                        if (products.Count == 0)
                        {
                            break; // Exit the loop if no more products are returned
                        }

                        allOrders.AddRange(products);
                        Console.WriteLine($"Count :" + allOrders.Count);
                        page++;
                    }
                }
                else
                {
                    // Handle case where lastRunTime is invalid (e.g., perform full fetch or log an appropriate message)
                    LogManager.Instance.LogMessage("Last run time is not valid. No products fetched.", "Product");
                }

                return allOrders; // Return the filtered list of products
            }
            catch (Exception ex)
            {
                LogManager.Instance.LogException(ex);
                LogManager.Instance.LogMessage("Error while getting orders from Woo ERROR: " + ex.Message, "Order");
                return new List<WooCommerceNET.WooCommerce.v3.Order>(); // Return an empty list on error
            }
        }

        public Task<WooCommerceNET.WooCommerce.v3.Order> GetOrderById(ulong id)
        {
            return _wc.Order.Get(id);
        }
    }
}
