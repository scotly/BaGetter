using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace BaGetter.Core.Indexing;
public interface IPackageDownloadService
{

    Task<bool> PackageDownloadAsync(string id, string version, CancellationToken cancellationToken);
}
