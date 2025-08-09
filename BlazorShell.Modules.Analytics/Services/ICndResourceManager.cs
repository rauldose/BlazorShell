using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BlazorShell.Modules.Analytics.Services
{
    public interface ICdnResourceManager
    {
        Task<bool> EnsureResourcesLoadedAsync();
        bool AreResourcesLoaded { get; }
    }
}
