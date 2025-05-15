using Microsoft.Extensions.Localization;
using Concurrency.Localization;
using Volo.Abp.DependencyInjection;
using Volo.Abp.Ui.Branding;

namespace Concurrency;

[Dependency(ReplaceServices = true)]
public class ConcurrencyBrandingProvider : DefaultBrandingProvider
{
    private IStringLocalizer<ConcurrencyResource> _localizer;

    public ConcurrencyBrandingProvider(IStringLocalizer<ConcurrencyResource> localizer)
    {
        _localizer = localizer;
    }

    public override string AppName => _localizer["AppName"];
}
