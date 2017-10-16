using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Skytecs.Hermes.Utilities;
using Microsoft.Extensions.Logging;
using System.Net;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Options;

namespace Skytecs.Hermes.Controllers
{
    [Authorize]
    public class BarcodePrinterController : Controller
    {
        private readonly ILogger<FiscalPrinterController> _logger;
        private readonly IOptions<ServiceSettings> _config;

        //private readonly IFiscalPrinterService _fiscalPrinterService;


        public BarcodePrinterController(ILogger<FiscalPrinterController> logger, IOptions<ServiceSettings> config)
        {
            Check.NotNull(logger, nameof(logger));
            Check.NotNull(config, nameof(config));
            //Check.NotNull(fiscalPrinterService, nameof(fiscalPrinterService));

            _logger = logger;
            _config = config;
            //_fiscalPrinterService = fiscalPrinterService;
        }

        [AcceptVerbs("Post")]
        [Route("api/printLabels")]
        public IActionResult PrintLabels([FromBody]BarcodePrinterData data)
        {
            try
            {
                if (data == null || !data.Labels.Any())
                {
                    throw new InvalidOperationException("нет этикеток для печати.");
                }

                _logger.Info("Вывод на печать");
                var labels = "";
                foreach (var label in data.Labels)
                {
                    labels += GetLabel(label);
                }

                RawPrinterHelper.Send(_config.Value.PrinterName, labels);


                return Ok();
            }
            catch (Exception e)
            {
                _logger.Error(e);
                return StatusCode((int)HttpStatusCode.InternalServerError, e.Message);
            }
        }

        private string GetLabel(Label label)
        {
            return String.Format(@"
I8,C,001
N
A60,5,0,2,1,1,N,""{4}""
A60,30,0,2,1,1,N,""{3}""
A60,55,0,2,1,1,N,""{1} {2}""
B80,80,0,2,2,6,70,B,""{0}""
P1
", label.Barcode, label.Date?.ToShortDateString() ?? "", label.Container, label.Company, label.Patient);

        }
    }


    public class BarcodePrinterData
    {
        public List<Label> Labels { get; set; }
    }

    public class Label
    {
        public string Barcode { get; set; }
        public DateTime? Date { get; set; }
        public string Container { get; set; }
        public string Company { get; set; }
        public string Patient { get; set; }
    }

}
