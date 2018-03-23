using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Skytecs.Hermes.Models
{
    [Serializable]
    public class OpenSessionData
    {
        public int CashierId { get; set; }
        public string CashierName { get; set; }
    }
}
