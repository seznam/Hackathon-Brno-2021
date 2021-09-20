using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.OpenApi.Models;

public class Program
{
    public static int Main(string[] args)
    {
        var host = Host.CreateDefaultBuilder(args)
            .UseDefaultServiceProvider((context,options) =>
        {
            options.ValidateScopes = true;
            options.ValidateOnBuild = true;
        }).ConfigureWebHostDefaults(webBuilder =>
        {
            webBuilder.UseStartup<Startup>();
        }).Build();

        host.Run();
        return 0;
    }
}