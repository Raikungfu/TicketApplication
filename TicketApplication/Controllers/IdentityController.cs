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
        private readonly EmailService _emailService;

        public IdentityController(ApplicationDbContext applicationDbContext, EmailService emailService)
        {
            _applicationDbContext = applicationDbContext;
            _emailService = emailService;
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
                var existUser = await _applicationDbContext.Users.FirstOrDefaultAsync(x => x.Email == model.Email);
                if (existUser != null)
                {
                    return Json(new { success = false, message = "User exist! Login now!" });
                }
                var user = new User
                {
                    Email = model.Email,
                    Name = model.Name,
                    PhoneNumber = model.Phone,
                    Password = model.Password,
                    Role = "Customer"
                };

                _applicationDbContext.Users.Add(user);
                await _applicationDbContext.SaveChangesAsync();

                _emailService.SendRegistrationConfirmationMail(user.Email, user.Name);

                return Json(new { success = true, message = "Registration successful. Email sent!" });
            }

            return Json(new { success = false, message = "Registration failed. Please check your inputs." });
        }

        [HttpPost]
        public async Task<IActionResult> ForgotPassword([FromForm] ForgotPasswordDto model)
        {
            if (string.IsNullOrEmpty(model.Email))
            {
                return Json(new { success = false, message = "Email is required." });
            }

            var user = await _applicationDbContext.Users.FirstOrDefaultAsync(x => x.Email == model.Email);
            if (user == null)
            {
                return Json(new { success = false, message = "Email not found." });
            }

            var token = Guid.NewGuid().ToString();
            var resetLink = Url.Action("ResetPassword", "Identity", new { token, email = user.Email }, Request.Scheme);
            var subject = "Yêu cầu khôi phục mật khẩu";
            var body = $"Xin chào {user.Name},<br><br>Vui lòng nhấp vào liên kết dưới đây để khôi phục mật khẩu của bạn:<br><a href='{resetLink}'>Khôi phục mật khẩu</a>";

            _emailService.SendMail(subject, user.Email, body);

            return Ok("Liên kết khôi phục mật khẩu đã được gửi đến email của bạn.");
        }

        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return RedirectToAction("Index", "Home");
        }
    }

    public class ForgotPasswordDto
    {
        public string Email { get; set; }
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

