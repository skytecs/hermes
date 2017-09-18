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

namespace Skytecs.Hermes
{
    public class Program
    {
        public static void Main(string[] args)
        {


            var pathToExe = Process.GetCurrentProcess().MainModule.FileName;
            var pathToContentRoot = Path.GetDirectoryName(pathToExe);

            var host = new WebHostBuilder()
                .UseStartup<Startup>()
                .UseUrls("http://0.0.0.0:44444")
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
