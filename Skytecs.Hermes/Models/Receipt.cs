using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Skytecs.Hermes.Models
{
    public class Receipt
    {
        private ICollection<RecItem> _items { get; set; }
        public ICollection<RecItem> Items
        {
            get
            {
                if (_items == null)
                {
                    _items = new List<RecItem>();
                }

                return _items;
            }
            set
            {
                _items = value;
            }
        }

        private double? _sum;
        public double? Sum {
            get
            {
                if (!_sum.HasValue)
                {
                    _sum = Total;
                }

                return _sum;
            }
            set
            {
                _sum = value;
            }
        }


        private double? _total;
        public double? Total
        {
            get
            {
                if (!_total.HasValue)
                {
                    _total = (double)Items.Sum(x => x.Price * x.Quantity);
                }

                return _total;
            }
        }

        public bool IsPaydByCard { get; set; }
    }
}
