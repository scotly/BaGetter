using BaGetter.Core.Indexing;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Threading;
using System.Threading.Tasks;

namespace BaGetter.Web;
public class DownloadModel : PageModel
{
    private readonly IPackageDownloadService _downloadsSource;
    public DownloadModel(IPackageDownloadService downloadsSource)
    {
        _downloadsSource = downloadsSource;
    }


    [BindProperty(Name = "q", SupportsGet = true)]
    public string Query { get; set; }

    [BindProperty(Name = "version", SupportsGet = true)]
    public string Version { get; set; }

    public async Task<IActionResult> OnGetAsync(CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid) return BadRequest();

        if (string.IsNullOrWhiteSpace(Query)) return Page();

        await _downloadsSource.PackageDownloadAsync(Query, Version, cancellationToken);
        return Page();
    }
}
