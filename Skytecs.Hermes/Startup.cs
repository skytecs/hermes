﻿using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NLog.Web;
using NLog.Extensions.Logging;
using Skytecs.Hermes.Services;
using Skytecs.Hermes.Models;
using Skytecs.Hermes.Utilities;
using Microsoft.Extensions.Options;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Cors.Internal;
using System.Net.WebSockets;
using Microsoft.AspNetCore.Http;
using System.Threading;
using System.Text;
using System.Security.Cryptography;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using RestSharp;
using System.Net;
using Microsoft.EntityFrameworkCore;
using System.IO;

namespace Skytecs.Hermes
{
    public class Startup
    {
        private IOptions<CommonSettings> _config;
        private readonly string _specialFolder;

        public Startup(IHostingEnvironment env)
        {
            var builder = new ConfigurationBuilder()
                .SetBasePath(env.ContentRootPath)
                .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
                .AddJsonFile($"appsettings.{env.EnvironmentName}.json", optional: true, reloadOnChange: true)
                .AddEnvironmentVariables();

            env.ConfigureNLog("nlog.config");

            Configuration = builder.Build();

            _specialFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), "Skytecs", "Hermes");
            if (!Directory.Exists(_specialFolder))
            {
                Directory.CreateDirectory(_specialFolder);
            }
        }


        public IConfigurationRoot Configuration { get; }


        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddTransient<IFiscalPrinterService, AtolPrinterService>();
            services.AddTransient<ISessionStorage, TempStorage>();
            services.AddSingleton<CentrifugoClient>();
            services.AddTransient<IBarcodePrinterService, ZebraPrinterService>();

            services.AddCentrifugoHandler<FiscalPrinterNotificationHandler>();

            services.AddCors();
            services.AddMvc();
            services.Configure<FiscalPrinterSettings>(Configuration);
            services.Configure<CommonSettings>(Configuration);
            services.Configure<CentrifugoSettings>(Configuration);
            services.Configure<BarcodePrinterSettings>(Configuration);

            var settings = Configuration.Get<CommonSettings>();

            services.AddEntityFrameworkSqlite();
            services.AddDbContext<DataContext>(options => options.UseSqlite($"Filename={Path.Combine(_specialFolder, settings.CashboxDatabase)}"));


        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IHostingEnvironment env, ILoggerFactory loggerFactory, IOptions<CommonSettings> config, DataContext context)
        {
            _config = config;

            loggerFactory.AddConsole();
            loggerFactory.AddNLog();

            //db.Database.EnsureCreated();
            context.Database.Migrate();

            app.AddNLogWeb();

            app.UseMiddleware<BasicAuthenticationMiddleware>(_config.Value.Password);
            app.UseCors(options => options.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader().AllowCredentials());

            app.UseMvc();
            app.UseDeveloperExceptionPage();

            app.StartCentrifugoListener();
        }
    }

    public static class CentrifugoExtensions
    {
        public static IServiceCollection AddCentrifugoHandler<THandler>(this IServiceCollection services)
            where THandler : class, ICentrifugoHandler
        {
            return services.AddSingleton<ICentrifugoHandler, THandler>();
        }

        public static IApplicationBuilder StartCentrifugoListener(this IApplicationBuilder app)
        {
            var config = app.ApplicationServices.GetService<IOptions<CommonSettings>>();
            if (!config.Value.EnableCentrifugoListener)
            {
                return app;
            }
            
            var client = app.ApplicationServices.GetService<CentrifugoClient>();
            var handler = app.ApplicationServices.GetService<ICentrifugoHandler>();
            client.Connect().ContinueWith(x => client.Subscribe()).ContinueWith(x => client.Listen(handler));

            return app;
        }
    }

    public interface ICentrifugoHandler
    {
        void Handle(JObject jsonData);
    }

    public class FiscalPrinterNotificationHandler : ICentrifugoHandler
    {
        private readonly ILogger<FiscalPrinterNotificationHandler> _logger;
        private readonly IFiscalPrinterService _fiscalPrinterService;
        private readonly IBarcodePrinterService _barcodePrinterService;
        private readonly string _clinicUrl;
        private readonly DataContext _dataContext;

        public FiscalPrinterNotificationHandler(ILogger<FiscalPrinterNotificationHandler> logger, IFiscalPrinterService fiscalPrinterService, IBarcodePrinterService barcodePrinterService, DataContext dataContext, IOptions<CommonSettings> settings)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _fiscalPrinterService = fiscalPrinterService ?? throw new ArgumentNullException(nameof(fiscalPrinterService));
            _barcodePrinterService = barcodePrinterService ?? throw new ArgumentNullException(nameof(barcodePrinterService));
            _dataContext = dataContext ?? throw new ArgumentNullException(nameof(dataContext));
            _clinicUrl = (settings ?? throw new ArgumentNullException(nameof(dataContext))).Value.ClinicUrl;
        }
        public void Handle(JObject jsonData)
        {
            if (jsonData == null)
            {
                return;
            }
            var notification = jsonData.ToObject<FiscalPrinterNotification>();

            try
            {
                var client = new RestClient($"{_clinicUrl}/api/operation");

                var request = new RestRequest(Method.GET);
                request.AddHeader("cache-control", "no-cache");
                request.AddQueryParameter("operationId", notification.OperationId.ToString());

                IRestResponse response = client.Execute(request);
                if (response.StatusCode != HttpStatusCode.OK)
                {
                    var message = String.IsNullOrEmpty(response.Content) ? response.ErrorMessage : response.Content;
                    throw new InvalidOperationException($"Не удалось получить данные операции '{notification.OperationId}': {response.StatusCode} - {message}");
                }

                var parameters = JToken.Parse(response.Content).ToString();
                switch (notification.Method)
                {
                    case "opensession":
                        var openSessionData = JsonConvert.DeserializeObject<OpenSessionData>(parameters);
                        _fiscalPrinterService.OpenSession(openSessionData.CashierId, openSessionData.CashierName);
                        break;
                    case "receipt":
                        var receipt = JsonConvert.DeserializeObject<Receipt>(parameters);
                        _fiscalPrinterService.PrintReceipt(receipt);
                        break;
                    case "refund":
                        var refund = JsonConvert.DeserializeObject<Receipt>(parameters);
                        _fiscalPrinterService.PrintRefund(refund);
                        break;
                    case "xreport":
                        _fiscalPrinterService.PrintXReport();
                        break;
                    case "zreport":
                        _fiscalPrinterService.PrintZReport();
                        break;
                    case "correction":
                        var correction = JsonConvert.DeserializeObject<CorrectionReceipt>(parameters);
                        _fiscalPrinterService.PrintCorrection(correction);
                        break;

                    case "labels":
                        var printerData = JsonConvert.DeserializeObject<BarcodePrinterData>(parameters);
                        _barcodePrinterService.Send(printerData);
                        break;
                    default:
                        throw new Exception($"Передан несуществующий метод '{notification.Method}'");
                }

                var operation = new Operation
                {
                    ClinicOperationId = notification.OperationId,
                    Method = notification.Method,
                    Received = DateTime.Now
                };
                _dataContext.Operations.Add(operation);
                _dataContext.SaveChanges();

                client = new RestClient($"{_clinicUrl}/api/confirmOperation");

                request = new RestRequest(Method.POST);
                request.DateFormat = "json";
                request.AddHeader("cache-control", "no-cache");
                request.AddHeader("Content-Type", "application/json");
                request.AddParameter("OperationId", notification.OperationId.ToString());

                response = client.Execute(request);
                if (response.StatusCode != HttpStatusCode.OK)
                {
                    var message = String.IsNullOrEmpty(response.Content) ? response.ErrorMessage : response.Content;
                    throw new InvalidOperationException($"Не удалось подтвердить операцию '{notification.OperationId}': {response.StatusCode} - {message}");
                }

                operation.Confirmed = DateTime.Now;
                _dataContext.SaveChanges();
            }
            catch (Exception e)
            {
                _logger.Error(e);

                var client = new RestClient($"{_clinicUrl}/api/reportError");

                var request = new RestRequest(Method.POST);
                request.AddHeader("cache-control", "no-cache");
                request.AddParameter("application/json", JsonConvert.SerializeObject(
                    new
                    {
                        OperationId = notification.OperationId.ToString(),
                        ErrorMessage = e.ToString()
                    })
                    , ParameterType.RequestBody);


                IRestResponse response = client.Execute(request);
            }
        }
    }

    [Serializable]
    public class FiscalPrinterNotification
    {
        public string Method { get; set; }
        public long OperationId { get; set; }
    }

    public static class FiscalPrinterMethods
    {
        public static string Receipt { get { return "receipt"; } }
        public static string Refund { get { return "refund"; } }
        public static string OpenSession { get { return "openSession"; } }
        public static string XReport { get { return "xreport"; } }
        public static string ZReport { get { return "zreport"; } }
        public static string Correction { get { return "correction"; } }
    }
}
