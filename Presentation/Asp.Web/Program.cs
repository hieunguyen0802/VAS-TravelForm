using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Hosting;

namespace src.Web
{
    public class Program
    {
        public static void Main(string[] args)
        {
            BuildWebHost(args).Run();
        }

        public static IWebHost BuildWebHost(string[] args) =>
            WebHost.CreateDefaultBuilder(args)
                .UseIISIntegration()
                .UseStartup<Startup>()
                .UseKestrel(option => option.Limits.MaxRequestBodySize = 209715200)
                .Build();
    }
}
