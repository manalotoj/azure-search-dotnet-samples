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
    private SearchServiceClient targetSearchClient;
    private ISearchIndexClient targetIndexClient;
    private ILogger<ImportExportTest> log;
    private readonly IndexImporter importer;
    private readonly ImportExportSettings settings;

    public ImportExportTest()
    {
      Startup startup = new Startup();
      IServiceCollection services = new ServiceCollection();
      startup.ConfigureServices(services);
      IServiceProvider provider = services.BuildServiceProvider();

      log = provider.GetService<ILoggerFactory>().CreateLogger<ImportExportTest>();
      settings = provider.GetService<ImportExportSettings>();
      importer = provider.GetService<IndexImporter>();


      targetSearchClient = new SearchServiceClient(settings.TargetSearchServiceName, new SearchCredentials(settings.TargetAdminKey));
      targetIndexClient = targetSearchClient.Indexes.GetClient(settings.TargetIndexName);
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

    [TestMethod]
    public void RestoreExtraLargeJsonFileUsingMergeOrUploadApproach()
    {
      importer.RestoreIndexes("c:\\IndexBackup\\kmcsearch-index86.json");
    }

    [TestMethod]
    public void RestoreIndex()
    {
      DeleteIndex();
      CreateTargetIndex();
    }

    [TestMethod]
    public void MergeOrUploadJsonFiles()
    {
      DeleteIndex();
      CreateTargetIndex();
      importer.RestoreIndexes("c:\\IndexBackup\\mergeOrUpload-sample-1.json");
      importer.RestoreIndexes("c:\\IndexBackup\\mergeOrUpload-sample-2.json");
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

    #region utils

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

    private bool DeleteIndex()
    {
      Console.WriteLine("\n  Delete target index {0} in {1} search service, if it exists", settings.TargetIndexName, settings.TargetSearchServiceName);
      // Delete the index if it exists
      try
      {
        targetSearchClient.Indexes.Delete(settings.TargetIndexName);
      }
      catch (Exception ex)
      {
        Console.WriteLine("  Error deleting index: {0}\r\n", ex.Message);
        Console.WriteLine("  Did you remember to set your SearchServiceName and SearchServiceApiKey?\r\n");
        return false;
      }

      return true;
    }

    private void CreateTargetIndex()
    {
      Console.WriteLine("\n  Create target index {0} in {1} search service", settings.TargetIndexName, settings.TargetSearchServiceName);
      // Use the schema file to create a copy of this index
      // I like using REST here since I can just take the response as-is


      string json = File.ReadAllText(settings.BackupDirectory + settings.SourceIndexName + ".schema");


      // Do some cleaning of this file to change index name, etc
      json = "{" + json.Substring(json.IndexOf("\"name\""));
      int indexOfIndexName = json.IndexOf("\"", json.IndexOf("name\"") + 5) + 1;
      int indexOfEndOfIndexName = json.IndexOf("\"", indexOfIndexName);
      json = json.Substring(0, indexOfIndexName) + settings.TargetIndexName + json.Substring(indexOfEndOfIndexName);

      Uri ServiceUri = new Uri("https://" + settings.TargetSearchServiceName + ".search.windows.net");
      HttpClient HttpClient = new HttpClient();
      HttpClient.DefaultRequestHeaders.Add("api-key", settings.TargetAdminKey);

      try
      {
        Uri uri = new Uri(ServiceUri, "/indexes");
        HttpResponseMessage response = AzureSearchHelper.SendSearchRequest(HttpClient, HttpMethod.Post, uri, json);
        response.EnsureSuccessStatusCode();
      }
      catch (Exception ex)
      {
        Console.WriteLine("  Error: {0}", ex.Message.ToString());
      }

    }

    #endregion utils
  }
}
