using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SageIntegration.SageRepository.Customer
{
    public class SageCustomerInfo
    {
        public string AccountName { get; set; }
        public string AccountManager { get; set; }
        public string AccountOpened { get; set; }
        public string AccountRef { get; set; }
        public string AccountStatus { get; set; }

        public string Address1 { get; set; }
        public string Address2 { get; set; }
        public string Address3 { get; set; }
        public string Address4 { get; set; }
        public string Address5 { get; set; }

        public string ContactName { get; set; }
        public string Telephone { get; set; }
        public string Email { get; set; }

        public string DiscountRate { get; set; }
        public string DiscountType { get; set; }
    }

}
