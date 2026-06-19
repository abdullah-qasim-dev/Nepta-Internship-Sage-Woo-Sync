using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SageIntegration.SageRepository.Product
{
    public interface IProductRepository
    {
        Task<List<SageProductInfo>> GetAll();
        Task<SageProductInfo> GETProduct(string sku, SageDataObject310.WorkSpace oWS);
        Task<SageDataObject310.StockRecord> AddAsync(WooCommerceNET.WooCommerce.v3.Product product);
    }
}
