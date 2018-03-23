using Newtonsoft.Json.Linq;
using System;

namespace Skytecs.Hermes
{
    [Serializable]
    public class FiscalPrinterData
    {
        public string Method { get; set; }
        public JObject Parameters { get; set; }
    }
}