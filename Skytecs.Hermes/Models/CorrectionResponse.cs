using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Skytecs.Hermes.Services.AtolV2;

namespace Skytecs.Hermes.Models
{
    public class CorrectionResponse
    {
        public decimal Total { get; set; }
        public int FiscalDocumentNumber { get; set; }
        public string FiscalDocumentSign { get; set; }
        public DateTime FiscalDocumentDateTime { get; set; }
        public int ShiftNumber { get; set; }
        public string FnNumber { get; set; }
        public string RegistrationNumber { get; set; }
        public string FnsUrl { get; set; }
        
        public CorrectionResponse(FiscalParams data)
        {
            Total = data.Total;
            FiscalDocumentNumber = data.FiscalDocumentNumber;
            FiscalDocumentSign = data.FiscalDocumentSign;
            FiscalDocumentDateTime = data.FiscalDocumentDateTime;
            ShiftNumber = data.ShiftNumber;
            FnNumber = data.FnNumber;
            RegistrationNumber = data.RegistrationNumber;
            FnsUrl = data.FnsUrl;

        }
    }
}
