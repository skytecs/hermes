using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

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
    }
}
