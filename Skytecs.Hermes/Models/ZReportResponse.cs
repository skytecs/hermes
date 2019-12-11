using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Skytecs.Hermes.Services.AtolV2;

namespace Skytecs.Hermes.Models
{
    public class ZReportResponse
    {
        public decimal Total { get; set; }
        public int FiscalDocumentNumber { get; set; }
        public string FiscalDocumentSign { get; set; }
        public DateTime FiscalDocumentDateTime { get; set; }
        public int ShiftNumber { get; set; }
        public string FnNumber { get; set; }
        public string RegistrationNumber { get; set; }
        public int ReceiptsCount { get; set; }
        public string FnsUrl { get; set; }

        public ZReportResponse(FiscalParams data)
        {
            Total = data.Total;
            FiscalDocumentNumber = data.FiscalDocumentNumber;
            FiscalDocumentSign = data.FiscalDocumentSign;
            FiscalDocumentDateTime = data.FiscalDocumentDateTime;
            ShiftNumber = data.ShiftNumber;
            FnNumber = data.FnNumber;
            RegistrationNumber = data.RegistrationNumber;
            ReceiptsCount = data.ReceiptsCount;
            FnsUrl = data.FnsUrl;

        }
    }
}
