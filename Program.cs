using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.CommandLine;
using System.CommandLine.NamingConventionBinder;
using System.IO;
using System.Threading.Tasks;

namespace owfsmq
{
    public static class Program
    {
        static async Task Main(string[] args)
        {
            var rootCommand = new RootCommand("Run service")
         {
            new Option<FileInfo>(new[]{ "--config", "-c" }, description: "Configuration file"){ IsRequired = true },
            new Option<LogLevel>(new[]{ "--logLevel", "-l" }, description: "Log level", getDefaultValue: () => LogLevel.Information),
         };
            rootCommand.Handler = CommandHandler.Create<FileInfo, LogLevel>(async (config, logLevel) =>
            {
                IServiceCollection serviceCollection = ConfigureServices(config, logLevel);
                IServiceProvider serviceProvider = serviceCollection.BuildServiceProvider();
                var service = serviceProvider.GetService<Service>();
                Console.CancelKeyPress += (s, e) => service.Stop();
                await service.Run();
            });

            await rootCommand.InvokeAsync(args);
        }

        private static IServiceCollection ConfigureServices(FileInfo configFile, LogLevel logLevel)
        {
            var serviceCollection = new ServiceCollection();
            serviceCollection
               .AddLogging(configure =>
               {
                   configure.AddConsole();
                   configure.SetMinimumLevel(logLevel);
               })
               .AddSingleton<IConfigProvider>(x => new JsonConfigProvider(configFile.FullName))
               .AddSingleton<IMqttClientService, MqttClientService>()
               .AddTransient<Service>();
            return serviceCollection;
        }
    }
}
