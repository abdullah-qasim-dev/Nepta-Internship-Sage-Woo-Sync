using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SageIntegration.Models
{
    public class Link
    {
        public string? Href { get; set; }
    }

    public class Links
    {
        public List<Link>? Self { get; set; }
        public List<Link>? Collection { get; set; }
    }
    public class WooCustomer
    {
        public int? Id { get; set; }
        public DateTime? DateCreatedGmt { get; set; }
        public DateTime? DateModified { get; set; }
        public DateTime? DateCreated { get; set; }
        public DateTime? DateModifiedGmt { get; set; }
        public string? Email { get; set; }
        public string? FirstName { get; set; }
        public string? LastName { get; set; }
        public string? Role { get; set; }
        public string? Username { get; set; }
        public WooBilling? Billing { get; set; }
        public WooShipping? Shipping { get; set; }
        public bool? IsPayingCustomer { get; set; }
        public string? AvatarUrl { get; set; }
        public List<object>? MetaData { get; set; }
        public Links? Links { get; set; }
    }
}
