using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SageIntegration.SageRepository.SalesOrder
{
    public class SalesOrderInfo
    {
        public string CustomerRef; 

        // Order Details
        public int? OrderNumber { get; set; }
        public string OrderType { get; set; }
        public string InvRef { get; set; }
        public DateTime? OrderDate { get; set; }

        // List of Items
        public List<OrderItem> Items { get; set; } = new List<OrderItem>();
        public string Deduction { get; set; }
        public string Description { get; set; }
        // Totals
        public decimal TotalGBP { get; set; }
        public decimal TotalVAT { get; set; }
        public decimal CarriageGBP { get; set; }
        public decimal CARR_TAX { get; set; }
        public decimal GrossGBP { get; set; }

        //WooDetail
        public ulong? CustomerId { get; set; }
        public string status { get; set; }
    }
    public class OrderItem
    {
        public string ProductCode { get; set; }
        public string Description { get; set; }
        public int Quantity { get; set; }
        public decimal PricePerUnit { get; set; }
        public decimal NetAmount { get; set; }
        public decimal VAT { get; set; }

        public ulong? ProductID { get; set; }
    }
}
