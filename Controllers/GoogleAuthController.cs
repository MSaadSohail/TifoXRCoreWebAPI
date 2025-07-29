// <copyright file="GoogleAuthController.cs" company="Global Mobile Software LLC">
// Copyright © 2025 All Rights Reserved
// </copyright>
// <author>Syed Hussain</author>
// <date>07/28/2025</date>
// <summary>Controller to handle google auth</summary>
using Google.Apis.Auth;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using GMS.TifoXRCoreWebAPI.Models;
using GMS.TifoXRCoreWebAPI.Data;

namespace GMS.TifoXRCoreWebAPI.Controllers
{
    [ApiController]
    [Route("api/auth")]
    public class GoogleAuthController : ControllerBase
    {
        private readonly AppDbContext _context;

        public GoogleAuthController(AppDbContext context)
        {
            _context = context;
        }

        [HttpPost("google")]
        public async Task<IActionResult> GoogleLogin([FromBody] TokenRequest request)
        {
            if (string.IsNullOrEmpty(request?.Token))
                return BadRequest(new { error = "Missing token" });

            try
            {
                var payload = await GoogleJsonWebSignature.ValidateAsync(request.Token);

                if (string.IsNullOrEmpty(payload.Email))
                    return BadRequest(new { error = "Invalid payload: no email" });

                var user = await _context.User.FirstOrDefaultAsync(u => u.user_name == payload.Email);

                // If user does not exist, create and insert
                if (user == null)
                {
                    user = new User
                    {
                        id = Guid.NewGuid().ToString(),
                        user_name = payload.Email,
                        platform_type_id = 2,
                        user_type_id = 1,
                        user_age_range_id = 3,
                        creation_time = DateTime.UtcNow,
                        modified_by = "system"
                    };

                    _context.User.Add(user);
                    await _context.SaveChangesAsync();
                }

                // If user exists, do nothing — just return their id
                return Ok(new { userId = user.id });
            }
            catch (Exception ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }
    }

    public class TokenRequest
    {
        public string Token { get; set; }
    }
}
