using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Skytecs.Hermes.Services;
using Skytecs.Hermes.Services.AtolV2;

namespace Skytecs.Hermes.Models
{
    [Serializable]
    public class RecItemV2
    {
        public IEnumerable<long> ContractItemIds { get; set; }

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
