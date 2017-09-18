using Skytecs.Hermes.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Skytecs.Hermes.Services
{
    public interface IFiscalPrinterService : IDisposable
    {
        void OpenSession(int cashierId, string cashierName);
        void PrintReceipt(Receipt reciept);
        void PrintRefund(Receipt receipt);
        void PrintZReport();
        void PrintXReport();
        void CheckConnection();
    }
}
