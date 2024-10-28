using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using TicketApplication.Data;
using TicketApplication.Models;
using TicketApplication.Service;
using Microsoft.AspNetCore.Authorization;

namespace TicketApplication.Controllers
{
    [AllowAnonymous]
    public class IdentityController : Controller
    {
        private readonly ApplicationDbContext _applicationDbContext;

        public IdentityController(ApplicationDbContext applicationDbContext)
        {
            _applicationDbContext = applicationDbContext;
        }

        // GET: /Identity/
        public IActionResult Index()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Login([FromForm] LoginDto loginDto)
        {
            var user = await _applicationDbContext.Users
                         .FirstOrDefaultAsync(x => x.Email == loginDto.Email && x.Password == loginDto.Password);

            if (ModelState.IsValid)
            {
                if (user != null)
                {
                    var claims = new List<Claim>
            {
                new Claim(ClaimTypes.Name, user.Email),
                new Claim(ClaimTypes.NameIdentifier, user.Id),
                new Claim(ClaimTypes.Role, user.Role),
            };

                    var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);

                    await HttpContext.SignInAsync(
                        CookieAuthenticationDefaults.AuthenticationScheme,
                        new ClaimsPrincipal(claimsIdentity)
                    );

                    return Json(new { success = true, message = "Login successful!" });
                }
                else
                {
                    return Json(new { success = false, message = "Invalid email or password." });
                }
            }

            return Json(new { success = false, message = "Model is not valid." });
        }


        [HttpPost]
        public async Task<IActionResult> Register([FromForm] UserRegistrationModel model)
        {
            if (ModelState.IsValid)
            {
                _applicationDbContext.Users.Add(new User
                {
                    Email = model.Email,
                    Name = model.Name,
                    PhoneNumber = model.Phone,
                    Password = model.Password,
                    Role = "Admin"
                });
                await _applicationDbContext.SaveChangesAsync();

                return Json(new { success = true, message = "Registration successful!" });
            }

            return Json(new { success = false, message = "Registration failed. Please check your inputs." });
        }

        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return RedirectToAction("Index");
        }
    }

    public class LoginDto
    {
        public string Email { get; set; }
        public string Password { get; set; }
    }
    public class UserRegistrationModel
    {
        public string Name { get; set; }
        public string Phone { get; set; }
        public string Email { get; set; }
        public string Password { get; set; }
    }

}

