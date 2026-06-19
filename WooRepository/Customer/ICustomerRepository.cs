using SageIntegration.Models;
using SageIntegration.SageRepository.Customer;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SageIntegration.WooRepository.Customer
{
    public interface ICustomerRepository
    {
        Task<List<WooCommerceNET.WooCommerce.v3.Customer>> GetAllAsync();
        Task AddAsync(SageCustomerInfo customer);
        Task UpdateAsync(SageCustomerInfo updateCustomer, WooCommerceNET.WooCommerce.v3.Customer prevCustomer);
        Task DeleteAsync(int id);
        Task<WooCommerceNET.WooCommerce.v3.Customer> GetCustomer(string email);
        Task<WooCommerceNET.WooCommerce.v3.Customer> GetCustomerByRole(string Acref);
        Task<WooCommerceNET.WooCommerce.v3.Customer> GetCustomer(ulong uid);
    }
}
