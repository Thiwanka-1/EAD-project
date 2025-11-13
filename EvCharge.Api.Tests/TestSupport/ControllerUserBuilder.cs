using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;


namespace EvCharge.Api.Tests.TestSupport
{
    public static class ControllerUserBuilder
    {
        public static void AttachUser(ControllerBase controller, string role, string? subjectId = null)
        {
            var claims = new List<Claim> { new(ClaimTypes.Role, role) };
            if (!string.IsNullOrWhiteSpace(subjectId))
            {
                claims.Add(new(ClaimTypes.NameIdentifier, subjectId));
            }

            var identity = new ClaimsIdentity(claims, "TestAuth");
            controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext
                {
                    User = new ClaimsPrincipal(identity)
                }
            };
        }
    }
}
