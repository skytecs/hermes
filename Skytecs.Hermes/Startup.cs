using System;
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
            services.AddMvc();
            services.Configure<FiscalPrinterSettings>(Configuration);
            services.Configure<ServiceSettings>(Configuration);
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IHostingEnvironment env, ILoggerFactory loggerFactory, IOptions<ServiceSettings> config)
        {
            _config = config;

            loggerFactory.AddConsole();
            loggerFactory.AddNLog();
            app.AddNLogWeb();
            
            app.UseMiddleware<BasicAuthenticationMiddleware>(_config.Value.Password);


            app.UseMvc();
            app.UseDeveloperExceptionPage();


        }
    }
}
