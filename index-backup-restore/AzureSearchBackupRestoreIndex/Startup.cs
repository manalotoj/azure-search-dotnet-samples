// This is a prototype tool that allows for extraction of data from a search index
// Since this tool is still under development, it should not be used for production usage
using AzureSearchBackupRestoreIndex;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Serilog;
using Microsoft.Extensions.Logging;

namespace AzureSearchBackupRestore
{
  public class Startup
  {
    readonly IConfigurationRoot configuration;
    readonly ImportExportSettings settings;

    ILoggerFactory Logger { get; }

    public Startup()
    {
      IConfigurationBuilder builder = new ConfigurationBuilder().AddJsonFile("local.appsettings.json");
      configuration = builder.Build();

      settings = new ImportExportSettings();
      configuration.GetSection("ImportExportSettings").Bind(settings);

      Log.Logger = new LoggerConfiguration()
        .WriteTo.Console()
        .WriteTo.Debug()
        .WriteTo.File($"{configuration["Logging:FilePath"]}\\log.txt")
        .CreateLogger();

      Logger = LoggerFactory.Create(builder =>
      {
        builder
          .AddConsole()
          .AddSerilog()
          .AddDebug();
      });
    }

    public void ConfigureServices(IServiceCollection services)
    {
      services.AddLogging();
      services.AddSingleton(Logger);
      services.AddSingleton(settings);
      services.AddSingleton(configuration);
      services.AddScoped(typeof(IndexImporter));
    }
  }
}
