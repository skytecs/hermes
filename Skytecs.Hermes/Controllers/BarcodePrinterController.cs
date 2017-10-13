using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Skytecs.Hermes.Utilities;
using Microsoft.Extensions.Logging;
using System.Net;
using Microsoft.AspNetCore.Authorization;

namespace Skytecs.Hermes.Controllers
{
    [Authorize]
    public class BarcodePrinterController : Controller
    {
        private readonly ILogger<FiscalPrinterController> _logger;
        //private readonly IFiscalPrinterService _fiscalPrinterService;


        public BarcodePrinterController(ILogger<FiscalPrinterController> logger)
        {
            Check.NotNull(logger, nameof(logger));
            //Check.NotNull(fiscalPrinterService, nameof(fiscalPrinterService));

            _logger = logger;
            //_fiscalPrinterService = fiscalPrinterService;
        }

        [AcceptVerbs("Post")]
        [Route("api/printLabels")]
        public IActionResult PrintLabels([FromBody]BarcodePrinterData data)
        {
            try
            {
                if (data == null || String.IsNullOrEmpty(data.Labels))
                {
                    throw new InvalidOperationException("нет этикеток для печати.");
                }

                _logger.Info("Вывод на печать");
                //RawPrinterHelper.Send(pd.PrinterSettings.PrinterName, data.Labels);
                RawPrinterHelper.Send("ZDesigner TLP 2844", data.Labels);


                return Ok();
            }
            catch (Exception e)
            {
                _logger.Error(e);
                return StatusCode((int)HttpStatusCode.InternalServerError, e.Message);
            }
        }
    }

    public class BarcodePrinterData
    {
        public string Labels { get; set; }
    }
}
