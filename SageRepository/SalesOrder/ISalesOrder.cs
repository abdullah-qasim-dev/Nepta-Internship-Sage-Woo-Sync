using SageDataObject310;
using SageIntegration.SageRepository.Product;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SageIntegration.SageRepository.SalesOrder
{
    public interface ISalesOrder
    {
        Task<List<SalesOrderInfo>> GetAll();
        Task<SageDataObject310.SopRecord> AddOrUpdateOrder(WooCommerceNET.WooCommerce.v3.Order wooOrder, WooCommerceNET.WooCommerce.v3.Customer customer);
        Task<InvoicePost> AddOrUpdateOrderAsync(WooCommerceNET.WooCommerce.v3.Order wooOrder, WooCommerceNET.WooCommerce.v3.Customer customer);

    }
}
