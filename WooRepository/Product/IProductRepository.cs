using SageIntegration.SageRepository.Product;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SageIntegration.WooRepository.Product
{
    public interface IProductRepository
    {
        Task<List<WooCommerceNET.WooCommerce.v3.Product>> GetAll();
        Task<WooCommerceNET.WooCommerce.v3.Product> AddAsync(SageProductInfo product);
        Task<WooCommerceNET.WooCommerce.v3.Product> GetProduct(string name);
        Task UpdateAsync(SageProductInfo sageProduct, WooCommerceNET.WooCommerce.v3.Product wooProduct);
    }
}
