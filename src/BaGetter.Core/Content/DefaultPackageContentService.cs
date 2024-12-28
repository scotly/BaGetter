using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using BaGetter.Core.Indexing;
using BaGetter.Protocol.Models;
using NuGet.Versioning;

namespace BaGetter.Core;

/// <summary>
/// Implements the NuGet Package Content resource in NuGet's V3 protocol.
/// </summary>
public class DefaultPackageContentService : IPackageContentService
{
    private readonly IPackageService _packages;
    private readonly IPackageStorageService _storage;
    private readonly IPackageDownloadService _packageDownloadService;

    public DefaultPackageContentService(
        IPackageService packages,
        IPackageStorageService storage,
        IPackageDownloadService packageDownloadService)
    {
        _packages = packages ?? throw new ArgumentNullException(nameof(packages));
        _storage = storage ?? throw new ArgumentNullException(nameof(storage));
        _packageDownloadService = packageDownloadService;
    }

    public async Task<PackageVersionsResponse> GetPackageVersionsOrNullAsync(
        string id,
        CancellationToken cancellationToken = default)
    {
        var versions = await _packages.FindPackageVersionsAsync(id, cancellationToken);
        if (!versions.Any())
        {
            return null;
        }

        return new PackageVersionsResponse
        {
            Versions = versions
                .Select(v => v.ToNormalizedString())
                .Select(v => v.ToLowerInvariant())
                .ToList()
        };
    }

    public async Task<Stream> GetPackageContentStreamOrNullAsync(
        string id,
        NuGetVersion version,
        CancellationToken cancellationToken = default)
    {
        if (!await _packages.ExistsAsync(id, version, cancellationToken))
        {
            return null;
        }

        await _packages.AddDownloadAsync(id, version, cancellationToken);
        return await _storage.GetPackageStreamAsync(id, version, cancellationToken);
    }

    public async Task<Stream> GetPackageManifestStreamOrNullAsync(string id, NuGetVersion version, CancellationToken cancellationToken = default)
    {
        if (!await _packages.ExistsAsync(id, version, cancellationToken))
        {
            //包不存在，则自动下载
            bool isSuccess = await _packageDownloadService.PackageDownloadAsync(id, version.ToNormalizedString(), cancellationToken);
            if (!isSuccess) return null;

            //再检查一次
            if (!await _packages.ExistsAsync(id, version, cancellationToken))
                return null;
        }

        return await _storage.GetNuspecStreamAsync(id, version, cancellationToken);
    }

    public async Task<Stream> GetPackageReadmeStreamOrNullAsync(string id, NuGetVersion version, CancellationToken cancellationToken = default)
    {
        var package = await _packages.FindPackageOrNullAsync(id, version, cancellationToken);
        if (package == null || !package.HasReadme)
        {
            return null;
        }

        return await _storage.GetReadmeStreamAsync(id, version, cancellationToken);
    }

    public async Task<Stream> GetPackageIconStreamOrNullAsync(string id, NuGetVersion version, CancellationToken cancellationToken = default)
    {
        var package = await _packages.FindPackageOrNullAsync(id, version, cancellationToken);
        if (package == null || !package.HasEmbeddedIcon)
        {
            return null;
        }

        return await _storage.GetIconStreamAsync(id, version, cancellationToken);
    }
}
