using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Skytecs.Hermes.Services;
using Skytecs.Hermes.Services.AtolV2;

namespace Skytecs.Hermes.Models
{
    public class ReceiptResponse
    {
        public List<ReceiptResponseData> Data { get; set; } = new List<ReceiptResponseData>();
        public List<ReceiptResponseError> Errors { get; set; } = new List<ReceiptResponseError>();
    }

    public class ReceiptResponseData
    {
        public List<long> ContractItemIds { get; set; } = new List<long>();

        public decimal Total { get; set; }
        public int FiscalDocumentNumber { get; set; }
        public string FiscalDocumentSign { get; set; }
        public DateTime FiscalDocumentDateTime { get; set; }
        public int FiscalReceiptNumber { get; set; }
        public int ShiftNumber { get; set; }
        public string FnNumber { get; set; }
        public string RegistrationNumber { get; set; }
        public string FnsUrl { get; set; }

        public ReceiptResponseData(IEnumerable<long> ids, FiscalParams data)
        {
            ContractItemIds.AddRange(ids);

            Total = data.Total;
            FiscalDocumentNumber = data.FiscalDocumentNumber;
            FiscalDocumentSign = data.FiscalDocumentSign;
            FiscalDocumentDateTime = data.FiscalDocumentDateTime;
            FiscalReceiptNumber = data.FiscalReceiptNumber;
            ShiftNumber = data.ShiftNumber;
            FnNumber = data.FnNumber;
            RegistrationNumber = data.RegistrationNumber;
            FnsUrl = data.FnsUrl;
        }

    }

    public class ReceiptResponseError
    {
        public List<long> ContractItemIds { get; set; } = new List<long>();

        public string Message { get; set; }
        public ReceiptResponseError(IEnumerable<long> ids, string message)
        {
            ContractItemIds.AddRange(ids);
            Message = message;
        }

    }
}
