using SageIntegration.SageRepository.Customer;
using SageIntegration.SageRepository.SalesOrder;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SageIntegration.WooRepository.Order
{
    public interface IOrder
    {
        Task<List<WooCommerceNET.WooCommerce.v3.Order>> GetAll();
        Task AddAsync(SalesOrderInfo order, WooCommerceNET.WooCommerce.v3.Customer? Customer);
        Task UpdateAsync(SalesOrderInfo updatedOrderInfo, WooCommerceNET.WooCommerce.v3.Order existingOrder, WooCommerceNET.WooCommerce.v3.Customer? Customer);
        Task<WooCommerceNET.WooCommerce.v3.Order> GetOrderById(ulong id,ulong OrderNumber);
        Task<WooCommerceNET.WooCommerce.v3.Order> GetOrderById(ulong id);
    }
}
