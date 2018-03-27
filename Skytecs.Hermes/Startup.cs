﻿using System;
using System.Collections.Generic;
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

namespace Skytecs.Hermes
{
    public class Startup
    {
        private IOptions<ServiceSettings> _config;

        public Startup(IHostingEnvironment env)
        {
            var builder = new ConfigurationBuilder()
                .SetBasePath(env.ContentRootPath)
                .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
                .AddJsonFile($"appsettings.{env.EnvironmentName}.json", optional: true, reloadOnChange: true)
                .AddEnvironmentVariables();

            env.ConfigureNLog("nlog.config");


            Configuration = builder.Build();
        }


        public IConfigurationRoot Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddTransient<IFiscalPrinterService, AtolPrinterService>();
            services.AddTransient<ISessionStorage, TempStorage>();
            services.AddSingleton<CentrifugoClient>();
            services.AddCors();
            services.AddMvc();
            services.Configure<FiscalPrinterSettings>(Configuration);
            services.Configure<ServiceSettings>(Configuration);
            services.Configure<CentrifugoSettings>(Configuration);
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IHostingEnvironment env, ILoggerFactory loggerFactory, IOptions<ServiceSettings> config)
        {
            _config = config;

            loggerFactory.AddConsole();
            loggerFactory.AddNLog();
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
        private static ILogger<CentrifugoClient> _logger;
        private static IFiscalPrinterService _fiscalPrinterService;
        private static string _clinicUrl;

        public static IApplicationBuilder StartCentrifugoListener(this IApplicationBuilder app)
        {
            try
            {
                _logger = app.ApplicationServices.GetService<ILogger<CentrifugoClient>>();
                _fiscalPrinterService = app.ApplicationServices.GetService<IFiscalPrinterService>();
                var config = app.ApplicationServices.GetService<IOptions<CentrifugoSettings>>();
                _clinicUrl = config.Value.ClinicUrl;
                var client = app.ApplicationServices.GetService<CentrifugoClient>();
                client.Connect()
                    .ContinueWith(x => client.Subscribe())
                    .ContinueWith(x => client.Listen(FiscalPrinterNotificationHandler));

            }
            catch (Exception e)
            {
                _logger.Error(e);
            }
            return app;
        }

        public static void FiscalPrinterNotificationHandler(JObject jsonData)
        {
            if (jsonData == null)
            {
                return;
            }
            var notification = jsonData.ToObject<FiscalPrinterNotification>();

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
                default:
                    throw new Exception($"Передан несуществующий метод '{notification.Method}'");
            }
            
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
    }
}
