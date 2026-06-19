using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SageIntegration.SageRepository.Product
{
    public class SageProductInfo
    {
        public string ProductCode { get; set; }
        public string ItemType { get; set; } // Stock Item, Service Item, etc.
        public string Description { get; set; }
        public string Category { get; set; }
        public decimal SuppUnitQty { get; set; }
        public decimal Weight { get; set; }
        public string BarCode { get; set; }
        public bool IsInactive { get; set; }
        public string Location { get; set; }
        public string CountryCode { get; set; }

        // Defaults
        public int? PurchaseNominalCode { get; set; } // Nullable as it's blank in image
        public string SupplierAccount { get; set; }
        public string TaxCode { get; set; }
        public string PartNo { get; set; }
        public string Department { get; set; }

        // Ordering
        public decimal LastCostPriceStandard { get; set; }
        public decimal LastCostPriceDiscounted { get; set; }
        public decimal LastOrderQty { get; set; }
        public DateTime? LastOrderDate { get; set; } // Nullable in case date is not set

        // Sales Price
        public decimal Price { get; set; }
        public string UnitOfSale { get; set; }

        // Status
        public decimal InStock { get; set; }
        public decimal FreeStock { get; set; }
        public decimal Allocated { get; set; }
        public decimal OnOrder { get; set; }
        public decimal ReOrderLevel { get; set; }
        public decimal ReOrderQty { get; set; }

        // Stock Take
        public DateTime? StockTakeDate { get; set; }
        public decimal StockTakeQuantity { get; set; }
    }
}
