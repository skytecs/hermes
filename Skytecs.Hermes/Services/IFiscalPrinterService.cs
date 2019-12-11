using Skytecs.Hermes.Models;
using Skytecs.Hermes.Services.Atol;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Skytecs.Hermes.Services
{
    public interface IFiscalPrinterService
    {
        void OpenSession(int cashierId, string cashierName);
        void PrintReceipt(Receipt reciept);
        void PrintRefund(Receipt receipt);
        void PrintCorrection(CorrectionReceipt receipt);
        void PrintZReport();
        void PrintXReport();
        void CheckConnection();
    }
}
