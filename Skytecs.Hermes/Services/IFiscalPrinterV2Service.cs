using Skytecs.Hermes.Models;
using Skytecs.Hermes.Services.AtolV2;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Skytecs.Hermes.Services
{
    public interface IFiscalPrinterV2Service
    {
        OpenSessionResponse OpenSession(int cashierId, string cashierName);
        ReceiptResponse PrintReceipt(ReceiptV2 reciept);
        ReceiptResponse PrintRefund(ReceiptV2 receipt);
        CorrectionResponse PrintCorrection(CorrectionReceipt receipt);
        ZReportResponse PrintZReport();
        void PrintXReport();
        void CheckConnection();
    }
}
