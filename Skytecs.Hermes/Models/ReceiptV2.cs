using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Skytecs.Hermes.Models
{
    [Serializable]
    public class ReceiptV2
    {
        private ICollection<RecItemV2> _items { get; set; }
        public ICollection<RecItemV2> Items
        {
            get
            {
                if (_items == null)
                {
                    _items = new List<RecItemV2>();
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
                    _total = (double)Items.Sum(x => x.Price);
                }

                return _total;
            }
        }

        public bool IsPaydByCard { get; set; }
    }
}
