using Concurrency.Localization;
using Volo.Abp.AspNetCore.Mvc;

namespace Concurrency.Controllers;

/* Inherit your controllers from this class.
 */
public abstract class ConcurrencyController : AbpControllerBase
{
    protected ConcurrencyController()
    {
        LocalizationResource = typeof(ConcurrencyResource);
    }
}
