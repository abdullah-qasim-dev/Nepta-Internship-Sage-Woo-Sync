using SageIntegration.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SageIntegration.SageRepository.Customer
{
    public interface ICustomerRepository
    {
        Task<List<SageCustomerInfo>> GetAllAsync();
        Task<SageCustomerInfo> GetByKey(string AcctRef);
        Task<SageDataObject310.SalesRecord> AddAsync(WooCommerceNET.WooCommerce.v3.Customer customer);
    }
}
