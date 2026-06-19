using SageIntegration.SageRepository.SalesOrder;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SageIntegration.SageRepository.Invoice
{
    public interface IInvoiceRepository
    {
        Task<List<SalesOrderInfo>> GetAll();
    }
}
