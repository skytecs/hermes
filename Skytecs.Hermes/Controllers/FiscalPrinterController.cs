using System;
using Microsoft.AspNetCore.Mvc;
using Skytecs.Hermes.Services;
using Skytecs.Hermes.Utilities;
using Skytecs.Hermes.Models;
using System.Net;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Authorization;

namespace Skytecs.Hermes.Controllers
{
    [Authorize]
    public class FiscalPrinterController : Controller
    {
        private readonly ILogger<FiscalPrinterController> _logger;
        private readonly IFiscalPrinterService _fiscalPrinterService;


        public FiscalPrinterController(ILogger<FiscalPrinterController> logger, IFiscalPrinterService fiscalPrinterService)
        {
            Check.NotNull(logger, nameof(logger));
            Check.NotNull(fiscalPrinterService, nameof(fiscalPrinterService));

            _logger = logger;
            _fiscalPrinterService = fiscalPrinterService;
        }

        [Route("api/receipt")]
        public IActionResult PrintReceipt([FromBody]Receipt receipt)
        {
            try
            {
                _fiscalPrinterService.PrintReceipt(receipt);
                return Ok();
            }
            catch (Exception e)
            {
                _logger.Error(e);
                return StatusCode((int)HttpStatusCode.InternalServerError, e.Message);
            }
        }

        [Route("api/refund")]
        public IActionResult PrintRefund([FromBody]Receipt receipt)
        {
            try
            {
                _fiscalPrinterService.PrintRefund(receipt);
                return Ok();
            }
            catch (Exception e)
            {
                _logger.Error(e);
                return StatusCode((int)HttpStatusCode.InternalServerError, e.Message);
            }
        }

        [Route("api/opensession/{cashies}")]
        public IActionResult OpenSession(int cashies, string name)
        {
            try
            {
                _fiscalPrinterService.OpenSession(cashies, name);
                return Ok();
            }
            catch (Exception e)
            {
                _logger.Error(e);
                return StatusCode((int)HttpStatusCode.InternalServerError, e.Message);
            }
        }

        [Route("api/zreport")]
        public IActionResult PrintZReport()
        {
            try
            {
                _fiscalPrinterService.PrintZReport();
                return Ok();
            }
            catch (Exception e)
            {
                _logger.Error(e);
                return StatusCode((int)HttpStatusCode.InternalServerError, e.Message);
            }
        }

        [Route("api/xreport")]
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

        [Route("api/check")]
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
