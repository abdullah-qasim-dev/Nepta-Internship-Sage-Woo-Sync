using SageIntegration.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SageIntegration.WooServices
{
    public interface IWooService
    {
        public Task addCustomerWootoSage();
        public Task addProdcutWootoSage();
        Task addOrderWootoSage();
    }
}
