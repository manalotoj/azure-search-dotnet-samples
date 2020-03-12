// This is a prototype tool that allows for extraction of data from a search index
// Since this tool is still under development, it should not be used for production usage
using AzureSearchBackupRestoreIndex;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.IO;
using System.Net.Http;
using System.Text;
using Microsoft.Extensions.Logging;
using System.Linq;

namespace AzureSearchBackupRestore
{
  public class IndexImporter
  {
    private readonly ImportExportSettings settings;
    private readonly ILogger<IndexImporter> log;
    private readonly HttpClient httpClient;

    public IndexImporter(ImportExportSettings settings, ILoggerFactory loggerFactory) 
    {
      this.settings = settings;
      log = loggerFactory.CreateLogger<IndexImporter>();

      httpClient = new HttpClient();
      httpClient.DefaultRequestHeaders.Add("api-key", settings.TargetAdminKey);
    }

    public void RestoreIndexes()
    {
      try
      {
        // Target Service Uri
        Uri ServiceUri = new Uri("https://" + settings.TargetSearchServiceName + ".search.windows.net");
        // Target Index
        Uri uri = new Uri(ServiceUri, "/indexes/" + settings.TargetIndexName + "/docs/index");

        foreach (string fileName in Directory.GetFiles(settings.BackupDirectory, settings.SourceIndexName + "*.json"))
        {
          // Determine file length
          long length = new FileInfo(fileName).Length;
          Console.WriteLine($"Uploading documents from file {fileName} with length {length}");

          try
          {
            RestoreIndexes(fileName);
          }
          catch (Exception exc)
          {
            log.LogError(exc, $"Failed to upload indexes from file {fileName}");
          }
        }
      }
      catch (Exception ex)
      {
        Console.WriteLine("  Error: {0}", ex.Message.ToString());
      }
    }

    public void RestoreIndexes(string jsonFile)
    {
      using (StreamReader streamReader = new StreamReader(jsonFile, Encoding.UTF8))
      {
        using (JsonTextReader reader = new JsonTextReader(streamReader))
        {
          reader.SupportMultipleContent = true;

          JArray jsonArray = new JArray();
          JObject rootJObject = new JObject();
          rootJObject.Add(Constants.ValuePropertyName, jsonArray);

          int baseLength = JsonConvert.SerializeObject(rootJObject).Length;
          int currentLength = baseLength;
          var serializer = new JsonSerializer();

          var indexCount = 0;
          while (reader.Read())
          {
            // look for immediate child array element
            if (reader.TokenType == JsonToken.StartArray)
            {
              while (reader.Read() && reader.TokenType != JsonToken.EndArray)
              {
                indexCount += 1;

                JObject currentIndex = JObject.Load(reader);
                string json = JsonConvert.SerializeObject(currentIndex);

                if (json.Length + currentLength < settings.MaxRequestSize)
                {
                  currentLength += json.Length;
                  jsonArray.Add(currentIndex);
                }
                else
                {
                  if (jsonArray.Count > 0)
                  {
                    // send existing built up new json payload
                    ImportFromJson(JsonConvert.SerializeObject(rootJObject));
                  }

                  // reset new json
                  jsonArray = new JArray();
                  rootJObject = new JObject();
                  rootJObject.Add(Constants.ValuePropertyName, jsonArray);
                  currentLength = baseLength;

                  if (json.Length < settings.MaxRequestSize)
                  {
                    currentLength = baseLength + json.Length;
                    jsonArray.Add(currentIndex);
                  }
                  else
                  {
                    log.LogWarning($"Index within json file '{jsonFile}' at position {indexCount - 1} with length of {json.Length} exceeds maximum size.");
                    log.LogInformation($"Begin processing large index from '{jsonFile}' at position {indexCount - 1} using mergeOrUpload");
                    //FileInfo fileInfo = new FileInfo(jsonFile);
                    //File.WriteAllText($"c:\\temp\\indexes\\{fileInfo.Name}-{indexCount}.json", json);
                    try
                    {
                      RestoreIndex(currentIndex);
                    }
                    catch (Exception exc)
                    {
                      log.LogError(exc, "Failed to restore large index within json file  '{jsonFile}' at position {indexCount - 1}.");
                    }
                  }
                }
              }
            }
          }

          if (jsonArray.Count > 0)
          {
            ImportFromJson(JsonConvert.SerializeObject(rootJObject));
          }
        }
      }
    }

    protected virtual void RestoreIndex(JObject currentIndex)
    {
      string md5 = (string)currentIndex["md5"];
      foreach (var jtoken in currentIndex.Children())
      {
        var prop = jtoken as JProperty;
        if (prop != null && prop.Name != "md5")
        {
          JObject searchIndex = new JObject();
          searchIndex.Add("@search.action", "mergeOrUpload");
          searchIndex.Add("md5", md5);
          searchIndex.Add(prop);

          JArray jsonArray = new JArray();
          jsonArray.Add(searchIndex);
          JObject rootJObject = new JObject();
          rootJObject.Add(Constants.ValuePropertyName, jsonArray);

          ImportFromJson(JsonConvert.SerializeObject(rootJObject));
        }
      }
    }

    protected virtual void ImportFromJson(string json)
    {
      try
      {
        log.LogInformation($"Begin upload index json with length of {json.Length}");

        Uri ServiceUri = new Uri("https://" + settings.TargetSearchServiceName + ".search.windows.net");
        Uri uri = new Uri(ServiceUri, "/indexes/" + settings.TargetIndexName + "/docs/index");

        HttpResponseMessage response = AzureSearchHelper.SendSearchRequest(httpClient, HttpMethod.Post, uri, json);
        response.EnsureSuccessStatusCode();
        log.LogInformation($"Successfully uploaded index json with length of {json.Length}");
      }
      catch (Exception exc)
      {
        log.LogError(exc, exc.Message);
      }
    }
  }
}
