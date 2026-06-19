using SageIntegration.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SageIntegration.WooServices.Customer
{
    public interface ICustomerService
    {
        Task<List<WooCommerceNET.WooCommerce.v3.Customer>> getWooCustomers();
    }
}
