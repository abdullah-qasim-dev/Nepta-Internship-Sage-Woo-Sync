using SageIntegration.Models;
using SageIntegration.WooRepository.Customer;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SageIntegration.WooServices.Customer
{
    public class CustomerService : ICustomerService
    {
        private readonly ICustomerRepository _customerRepository;

        public CustomerService(ICustomerRepository customerRepository)
        {
            _customerRepository = customerRepository;
        }

        public async Task<List<WooCommerceNET.WooCommerce.v3.Customer>> getWooCustomers()
        {
            return await _customerRepository.GetAllAsync();
        }
    }
}
