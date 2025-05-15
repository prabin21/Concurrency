using System;
using System.Collections.Generic;
using System.Text;
using Concurrency.Localization;
using Volo.Abp.Application.Services;

namespace Concurrency;

/* Inherit your application services from this class.
 */
public abstract class ConcurrencyAppService : ApplicationService
{
    protected ConcurrencyAppService()
    {
        LocalizationResource = typeof(ConcurrencyResource);
    }
}
