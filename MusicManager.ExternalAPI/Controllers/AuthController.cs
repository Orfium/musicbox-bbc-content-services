using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using MusicManager.Application.Services;
using MusicManager.Core.ViewModules;
using System.Threading.Tasks;

namespace MusicManager.ExternalAPI.Controllers
{
    [Route("api/v{version:apiVersion}/[controller]")]
    [ApiController]
    [ApiVersion("1")]
    public class AuthController : ControllerBase
    {
        
        private readonly IAuthentication _auth;
        public AuthController(IConfiguration config, IAuthentication auth)
        {
            _auth = auth;
        }

        [AllowAnonymous]
        [HttpPost("token")]
        public async Task<IActionResult> Login([FromBody] UserModel userModel)
        {
            IActionResult response = Unauthorized();

            if (userModel != null)
            {
                var tokenString = await _auth.AuthenticateUser(userModel);
                response = Ok(new { token = tokenString });
            }

            return response;
        }


   

    }
}
