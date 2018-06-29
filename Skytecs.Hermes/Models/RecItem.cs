using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Skytecs.Hermes.Services;

namespace Skytecs.Hermes.Models
{
    [Serializable]
    public class RecItem
    {
        public string Description { get; set; }
        public decimal Price { get; set; }
        public int Quantity { get; set; }
        public int VatInfo { get; set; }
        public decimal UnitPrice { get; set; }
        public string UnitName { get; set; }
        public VatType? TaxType { get; set; }
        public TaxationType? TaxationType { get; set; }
        public PaymentObject? PaymentObjectType { get; set; }
    }
}
