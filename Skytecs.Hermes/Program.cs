using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting.WindowsServices;
using System.Diagnostics;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Skytecs.Hermes.Models;
using Microsoft.EntityFrameworkCore;

namespace Skytecs.Hermes
{
    public class Program
    {
        public static void Main(string[] args)
        {


            var pathToExe = Process.GetCurrentProcess().MainModule.FileName;
            var pathToContentRoot = Path.GetDirectoryName(pathToExe);

            var appSettingsConfiguration = new ConfigurationBuilder()
                .SetBasePath(pathToContentRoot)
                .AddJsonFile("appsettings.json")
                .Build();

            var host = new WebHostBuilder()
                .UseStartup<Startup>()
                .UseUrls(appSettingsConfiguration["urls"])
                .UseKestrel()
                .UseContentRoot(pathToContentRoot)
                //.UseApplicationInsights()
                .Build();

#if DEBUG
            host.Run();
#else
            host.RunAsService();
#endif
        }
    }
}
