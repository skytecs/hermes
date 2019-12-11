using System;
using Microsoft.AspNetCore.Mvc;
using Skytecs.Hermes.Services;
using Skytecs.Hermes.Utilities;
using Skytecs.Hermes.Models;
using System.Net;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Cors;
using System.Linq;
using Skytecs.Hermes.Services.AtolV2;

namespace Skytecs.Hermes.Controllers
{
    [Authorize]
    [Route("api/v2")]
    public class FiscalPrinterV2Controller : Controller
    {
        private readonly ILogger<FiscalPrinterV2Controller> _logger;
        private readonly IFiscalPrinterV2Service _fiscalPrinterService;


        public FiscalPrinterV2Controller(ILogger<FiscalPrinterV2Controller> logger, IFiscalPrinterV2Service fiscalPrinterService)
        {
            Check.NotNull(logger, nameof(logger));
            Check.NotNull(fiscalPrinterService, nameof(fiscalPrinterService));

            _logger = logger;
            _fiscalPrinterService = fiscalPrinterService;
        }

        [Route("receipt")]
        public IActionResult PrintReceipt([FromBody]ReceiptV2 receipt)
        {
            try
            {
                return Ok(_fiscalPrinterService.PrintReceipt(receipt));
            }
            catch (Exception e)
            {
                _logger.Error(e);
                return StatusCode((int)HttpStatusCode.InternalServerError, e.Message);
            }
        }

        [Route("refund")]
        public IActionResult PrintRefund([FromBody]ReceiptV2 receipt)
        {
            try
            {
                return Ok(_fiscalPrinterService.PrintRefund(receipt));
            }
            catch (Exception e)
            {
                _logger.Error(e);
                return StatusCode((int)HttpStatusCode.InternalServerError, e.Message);
            }
        }

        [Route("correction")]
        public IActionResult PrintCorrection([FromBody]CorrectionReceipt receipt)
        {
            try
            {
                return Ok(_fiscalPrinterService.PrintCorrection(receipt));
            }
            catch (Exception e)
            {
                _logger.Error(e);
                return StatusCode((int)HttpStatusCode.InternalServerError, e.Message);
            }
        }


        [Route("opensession/{cashies}")]
        public IActionResult OpenSession(int cashies, string name)
        {
            try
            {
                return Ok(_fiscalPrinterService.OpenSession(cashies, name));
            }
            catch (Exception e)
            {
                _logger.Error(e);
                return StatusCode((int)HttpStatusCode.InternalServerError, e.Message);
            }
        }

        [Route("zreport")]
        public IActionResult PrintZReport()
        {
            try
            {
                return Ok(_fiscalPrinterService.PrintZReport());
            }
            catch (Exception e)
            {
                _logger.Error(e);
                return StatusCode((int)HttpStatusCode.InternalServerError, e.Message);
            }
        }

        [Route("xreport")]
        public IActionResult PrintXRepoprt()
        {
            try
            {
                _fiscalPrinterService.PrintXReport();
                return Ok();
            }
            catch (Exception e)
            {
                _logger.Error(e);
                return StatusCode((int)HttpStatusCode.InternalServerError, e.Message);
            }
        }

        [Route("check")]
        public IActionResult CheckConnection()
        {
            try
            {
                _fiscalPrinterService.CheckConnection();
                return Ok();
            }
            catch (Exception e)
            {
                _logger.Error(e);
                return StatusCode((int)HttpStatusCode.InternalServerError, e.Message);
            }
        }

    }
}
