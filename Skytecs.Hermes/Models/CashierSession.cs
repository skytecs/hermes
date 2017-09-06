using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Skytecs.Hermes.Models
{
    /// <summary>
    /// Смена кассира
    /// </summary>
    [Serializable]
    public class CashierSession
    {
        public long SessionId { get; set; }
        public string CashierName { get; set; }
        public DateTime SessionStart { get; set; }
        public int CashierId { get; set; }

        public CashierSession() { }
        public CashierSession(int cashierId, string cashierName)
        {
            CashierId = cashierId;
            CashierName = cashierName;
            SessionStart = DateTime.Now;
        }

    }
}
