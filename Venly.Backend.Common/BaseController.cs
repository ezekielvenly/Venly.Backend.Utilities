using Microsoft.AspNetCore.Mvc;
using Venly.Backend.Common.Hmac;

namespace Venly.Backend.Common;

[ApiController]
[HmacAuthorize]
public abstract class BaseController : ControllerBase
{
}
