using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using MusicManager.Application.Services;


// For more information on enabling Web API for empty projects, visit https://go.microsoft.com/fwlink/?LinkID=397860

namespace MusicManager.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AWSS3APIController : ControllerBase
    {
        private readonly IAWSS3Repository _AWSS3Repository;

        public AWSS3APIController(IAWSS3Repository AWSS3Repository)
        {
            _AWSS3Repository = AWSS3Repository;
        }

        [HttpPost("GetS3SessionToken")]
        public async Task<IActionResult> GenerateS3SessionTokenAsync()
        {
            return Ok(await _AWSS3Repository.GenerateS3SessionTokenAsync());
        }
    }
}
