using Skytecs.Hermes.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Skytecs.Hermes.Services
{
    public interface IFiscalPrinterService : IDisposable
    {
        SessionOpeningStatus OpenSession(int cashierId, string cashierName);
        PrinterOperationStatus PrintReceipt(Receipt reciept);
        PrinterOperationStatus PrintRefund(Receipt receipt);
        ZReportStatus PrintZReport();
        ZReportStatus PrintXReport();
    }
}
