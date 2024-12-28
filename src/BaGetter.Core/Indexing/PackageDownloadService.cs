using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace BaGetter.Core.Indexing;
public class PackageDownloadService : IPackageDownloadService
{
    public const string ServiceIndexV3Url = "https://azuresearch-ea.nuget.org/query";

    private readonly HttpClient _httpClient;
    private readonly ILogger<PackageDownloadService> _logger;
    private readonly IPackageIndexingService _indexer;
    public PackageDownloadService(HttpClient httpClient, ILogger<PackageDownloadService> logger, IPackageIndexingService indexer)
    {
        _httpClient = httpClient;
        _logger = logger;
        _indexer = indexer;
    }
    public async Task<bool> PackageDownloadAsync(string id, string version, CancellationToken cancellationToken)
    {
        _logger.LogInformation($"Fetching {id} downloads...");

        var results = new Dictionary<string, Dictionary<string, long>>();

        var packageJson = await GetServiceIndexV3Async(id.Trim());

        if (string.IsNullOrWhiteSpace(packageJson)) return false;

        var jObj = JsonConvert.DeserializeObject(packageJson) as JObject;

        var total = Convert.ToInt32(jObj["totalHits"].ToString());

        if (total <= 0) return false;

        var data = jObj["data"] as JArray;

        foreach (var item in data)
        {
            var packageName = item["id"].ToString();
            if (packageName.ToLowerInvariant() != id.Trim().ToLowerInvariant()) continue;

            var describe = item["description"].ToString();
            var iconUrl = item["iconUrl"].ToString();
            var licenseUrl = item["licenseUrl"].ToString();
            var packTypes = item["packageTypes"] as JArray;

            var versions = item["versions"] as JArray;
            foreach (var versionItem in versions)
            {
                if (!string.IsNullOrWhiteSpace(version) && versionItem["version"].ToString() != version)
                {
                    continue;
                }

                var versionJson = await GetPackageJsonAsync(versionItem["@id"].ToString());

                var versionJob = JsonConvert.DeserializeObject(versionJson) as JObject;

                using var packageStream = await GetDownloadV3Async(versionJob["packageContent"].ToString());

                await _indexer.IndexAsync(packageStream, cancellationToken);
            }
        }
        _logger.LogInformation($"Parsed {id} downloads");

        return true;
    }

    private async Task<string> GetServiceIndexV3Async(string id)
    {
        return await GetPackageJsonAsync($"{ServiceIndexV3Url}?q={id.ToLowerInvariant()}");
    }

    private async Task<string> GetPackageJsonAsync(string url)
    {
        var response = await _httpClient.GetAsync(url, HttpCompletionOption.ResponseHeadersRead);
        return await response.Content.ReadAsStringAsync();
    }

    private async Task<Stream> GetDownloadV3Async(string url)
    {
        _logger.LogInformation($"Downloading...");

        var fileStream = File.Open(Path.GetTempFileName(), FileMode.Create);
        var response = await _httpClient.GetAsync(url, HttpCompletionOption.ResponseHeadersRead);

        response.EnsureSuccessStatusCode();

        using (var networkStream = await response.Content.ReadAsStreamAsync())
        {
            await networkStream.CopyToAsync(fileStream);
        }

        fileStream.Seek(0, SeekOrigin.Begin);

        _logger.LogInformation($"Downloaded");

        return fileStream;
    }

}
