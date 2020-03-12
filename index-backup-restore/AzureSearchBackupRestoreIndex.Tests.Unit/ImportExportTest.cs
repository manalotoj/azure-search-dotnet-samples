using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Net.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Azure.Search;
using AzureSearchBackupRestore;
using System.Collections.Generic;
using Microsoft.Extensions.Logging;
using Serilog;
using System.Reflection.Metadata;
using Microsoft.Extensions.DependencyInjection;

namespace AzureSearchBackupRestoreIndex.Tests.Unit
{
  [TestClass]
  public class ImportExportTest
  {
    private static SearchServiceClient SourceSearchClient;
    private static ISearchIndexClient SourceIndexClient;
    private static SearchServiceClient TargetSearchClient;
    private static ISearchIndexClient TargetIndexClient;

    private static ILogger<ImportExportTest> log;
    private static readonly ImportExportSettings settings;
    private readonly IndexImporter importer;

    public ImportExportTest()
    {
      Startup startup = new Startup();
      IServiceCollection services = new ServiceCollection();
      startup.ConfigureServices(services);
      IServiceProvider provider = services.BuildServiceProvider();

      log = provider.GetService<ILoggerFactory>().CreateLogger<ImportExportTest>();
      importer = provider.GetService<IndexImporter>();
    }

    [TestMethod]
    public void RestoreSmallJsonFile()
    {
      importer.RestoreIndexes("c:\\IndexBackup\\Source\\kmcsearch-index2.json");
    }

    [TestMethod]
    public void RestoreLargeJsonFile()
    {
      importer.RestoreIndexes("c:\\IndexBackup\\kmcsearch-index1.json");
    }

    [TestMethod, Ignore]
    public void GetTranslationContentLengths()
    {
      TraverseTranslations("c:\\IndexBackup\\kmcsearch-index1.json");
    }

    [TestMethod]
    public void WriteToFile()
    {
      log.LogInformation("test");
    }

    private void TraverseTranslations(string jsonFile)
    {
      List<string> properties = new List<string> { "translation", "transcription", "textKeyPhrases" };

      using (StreamReader streamReader = new StreamReader(jsonFile, Encoding.UTF8))
      {
        using (JsonTextReader reader = new JsonTextReader(streamReader))
        {
          reader.SupportMultipleContent = true;
          while (reader.Read())
          {
            if (reader.Value != null)
            {
              string property = reader.Value.ToString();
              if (reader.TokenType == JsonToken.PropertyName && properties.Contains(property))
              {
                reader.Read();
                string value = reader.Value != null ? reader.Value.ToString() : "empty";
                Debug.WriteLine($"{property} length: " + value.Length);
              }
            }
          }
        }
      }
    }
  }
}
