using System;
using System.Threading;
using System.Threading.Tasks;

namespace RimWorldModUpdater.Models;

public interface IModSource
{
    public Task<string> FetchVersionAsync(CancellationToken cancellationToken);
    public Task<RimWorldModAbout?> FetchAboutAsync(CancellationToken cancellationToken);
    public Task DownloadAsync(RimWorldMod mod, IProgress<float> progress, CancellationToken cancellationToken);
}