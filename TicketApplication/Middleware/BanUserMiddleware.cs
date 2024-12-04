using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Identity;
using System.Security.Claims;
using TicketApplication.Data;
using TicketApplication.Models;

namespace TicketApplication.Middleware
{
    public class BanUserMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly IServiceProvider _serviceProvider;

        public BanUserMiddleware(RequestDelegate next, IServiceProvider serviceProvider)
        {
            _next = next;
            _serviceProvider = serviceProvider;
        }

        public async Task InvokeAsync(HttpContext httpContext)
        {
            using (var scope = _serviceProvider.CreateScope())
            {
                var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

                var userId = httpContext.User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (userId != null)
                {
                    var user = await dbContext.Users.FindAsync(userId);

                    if (user != null && user.IsBan)
                    {
                        await httpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
                        httpContext.Response.Redirect("/Identity/Login");
                        return;
                    }
                }
            }

            await _next(httpContext);
        }
    }
}
