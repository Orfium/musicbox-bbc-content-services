using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using MusicManager.Core.Payload;
using MusicManager.Core.ViewModules;
using MusicManager.Logics.ServiceLogics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MusicManager.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class UserController : ControllerBase
    {
        private readonly IUserLogic _user;
        private readonly IOptions<AppSettings> _appSettings;

        public UserController(IUserLogic user,
            IOptions<AppSettings> appSettings)
        {
            _user = user;
            _appSettings = appSettings;
        }

        [HttpPost("upsertUser")]
        public async Task UpsertUsers(UserPayload userPayload)
        {
            await _user.UpsertUsers(userPayload);
        }

        [HttpGet("APIVersion")]
        public IActionResult APIVersion()
        {
            return Ok($"Music box Content API V - {_appSettings.Value.AppVersion}");
        }
    }
}
