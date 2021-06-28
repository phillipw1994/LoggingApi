using System.Security.Claims;
using System.Threading.Tasks;
using AspNet.Security.OpenIdConnect.Extensions;
using AspNet.Security.OpenIdConnect.Primitives;
using AspNet.Security.OpenIdConnect.Server;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;

namespace LoggingAuthApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuthorizationController : ControllerBase
    {
        [HttpPost("~/connect/token")]
        [Produces("application/json")]
        public async Task<IActionResult> Exchange()
        {
            var request = HttpContext.GetOpenIdConnectRequest();
            if (request.IsClientCredentialsGrantType())
            {
                var identity = new ClaimsIdentity(OpenIdConnectServerDefaults.AuthenticationScheme);
                identity.AddClaim(ClaimTypes.NameIdentifier, request.ClientId,
                    OpenIdConnectConstants.Destinations.AccessToken);

                // Create a new authentication ticket holding the user identity.
                var ticket = new AuthenticationTicket(
                    new ClaimsPrincipal(identity),
                    new AuthenticationProperties(),
                    OpenIdConnectServerDefaults.AuthenticationScheme);

                return SignIn(ticket.Principal, ticket.Properties, ticket.AuthenticationScheme);
            }

            return BadRequest(new OpenIdConnectResponse
            {
                Error = OpenIdConnectConstants.Errors.UnsupportedGrantType,
                ErrorDescription = "The specified grant type is not supported."
            });
        }
    }
}
