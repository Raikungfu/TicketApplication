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
using static System.Net.WebRequestMethods;
using Microsoft.AspNetCore.Http;
using Microsoft.IdentityModel.Tokens;
using Microsoft.Extensions.Caching.Memory;

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


        public IActionResult Login()
        {
            return Unauthorized();
        }

        public IActionResult AccessDenied()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Login([FromForm] LoginDto loginDto)
        {
            var user = await _applicationDbContext.Users
                         .FirstOrDefaultAsync(x => x.Email == loginDto.Email && !x.IsBan);

            if (ModelState.IsValid)
            {
                if (user != null)
                {
                    var passwordHasher = new PasswordHasher<User>();
                    var verificationResult = passwordHasher.VerifyHashedPassword(user, user.Password, loginDto.Password);

                    if (verificationResult == PasswordVerificationResult.Success)
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
                }

                return Json(new { success = false, message = "Invalid email or password." });
            }

            return Json(new { success = false, message = "Model is not valid." });
        }

        [HttpPost]
        public IActionResult RegisterRequestOtp([FromForm] UserRegistrationModel model)
        {
            if (model.Email.IsNullOrEmpty())
            {
                return Json(new { success = false, message = "Email is required." });
            }
            var existUser = _applicationDbContext.Users.Any(x => x.Email == model.Email);
            if (existUser)
                return Json(new { success = false, message = "Email already registered." });

            var otp = GenerateOtp();
            var expiryTime = DateTime.UtcNow.AddMinutes(5);

            HttpContext.Session.SetString("RegisterOtp", otp);
            HttpContext.Session.SetString("RegisterOtpExpiry", expiryTime.ToString());
            HttpContext.Session.SetString("RegisterEmail", model.Email);
            HttpContext.Session.SetString("RegisterPhone", model.Phone);
            HttpContext.Session.SetString("RegisterPassword", model.Password);
            HttpContext.Session.SetString("RegisterName", model.Name);

            _emailService.SendMail(
                title: "OTP for Registration",
                recip: model.Email,
                body: $"Your OTP code is: <b>{otp}</b>. It expires in 5 minutes."
            );

            return Json(new { success = true, message = "OTP sent to your email." });
        }


        [HttpPost]
        public IActionResult Register([FromBody] RegisterDto model)
        {
            var otp = HttpContext.Session.GetString("RegisterOtp");
            var expiryString = HttpContext.Session.GetString("RegisterOtpExpiry");
            var email = HttpContext.Session.GetString("RegisterEmail");
            var phone = HttpContext.Session.GetString("RegisterPhone");
            var pw = HttpContext.Session.GetString("RegisterPassword");
            var name = HttpContext.Session.GetString("RegisterName");

            if (otp == null || expiryString == null || email == null)
                return Json(new { success = false, message = "No OTP request found." });

            if (email != model.email)
                return Json(new { success = false, message = "Email mismatch." });

            if (otp != model.otpCode)
                return Json(new { success = false, message = "Invalid OTP." });

            if (DateTime.TryParse(expiryString, out var expiry) && expiry < DateTime.UtcNow)
                return Json(new { success = false, message = "OTP expired." });


            var passwordHasher = new PasswordHasher<User>();
            var user = new User
            {
                Email = email,
                Name = name,
                PhoneNumber = phone,
                Role = "Customer"
            };
            user.Password = passwordHasher.HashPassword(user, pw);

            _applicationDbContext.Users.Add(user);

            _applicationDbContext.SaveChanges();

            _emailService.SendRegistrationConfirmationMail(email, name);

            HttpContext.Session.Remove("RegisterOtp");
            HttpContext.Session.Remove("RegisterOtpExpiry");
            HttpContext.Session.Remove("RegisterEmail");
            HttpContext.Session.Remove("RegisterPhone");
            HttpContext.Session.Remove("RegisterPassword");
            HttpContext.Session.Remove("RegisterName");

            return Json(new { success = true, message = "Registration successful." });
        }


        /*
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
        }*/

        [HttpPost]
        public IActionResult ForgotPasswordRequestOtp([FromBody] VerifyOtpDto model)
        {
            if (string.IsNullOrEmpty(model.email))
                return Json(new { success = false, message = "Email is required." });

            var user = _applicationDbContext.Users.FirstOrDefault(x => x.Email == model.email);
            if (user == null)
                return Json(new { success = false, message = "Email not found." });

            var otp = GenerateOtp();
            var expiry = DateTime.UtcNow.AddMinutes(5);

            HttpContext.Session.SetString("Otp", otp);
            HttpContext.Session.SetString("OtpExpiry", expiry.ToString());
            HttpContext.Session.SetString("OtpEmail", model.email);

            _emailService.SendMail(
                title: "OTP for Password Reset",
                recip: model.email,
                body: $"Your OTP code is: <b>{otp}</b>. It expires in 5 minutes."
            );

            return Json(new { success = true, message = "OTP sent to your email." });
        }

        [HttpPost]
        public IActionResult VerifyOtp([FromBody] VerifyOtpDto dto)
        {
            var otp = HttpContext.Session.GetString("Otp");
            var expiryString = HttpContext.Session.GetString("OtpExpiry");
            var storedEmail = HttpContext.Session.GetString("OtpEmail");

            if (otp == null || expiryString == null || storedEmail == null)
                return Json(new { success = false, message = "No OTP request found." });

            if (storedEmail != dto.email)
                return Json(new { success = false, message = "Email mismatch." });

            if (otp != dto.otp)
                return Json(new { success = false, message = "Invalid OTP." });

            if (DateTime.TryParse(expiryString, out var expiry) && expiry < DateTime.UtcNow)
                return Json(new { success = false, message = "OTP expired." });

            HttpContext.Session.Remove("Otp");
            HttpContext.Session.Remove("OtpExpiry");
            HttpContext.Session.Remove("OtpEmail");

            return Json(new { success = true, message = "OTP verified successfully." });
        }

        [HttpPost]
        public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordDto model)
        {
            var user = await _applicationDbContext.Users.FirstOrDefaultAsync(x => x.Email == model.email);
            if (user == null)
                return Json(new { success = false, message = "Email not found." });

            var passwordHasher = new PasswordHasher<User>();

            user.Password = passwordHasher.HashPassword(user, model.newPassword);
            await _applicationDbContext.SaveChangesAsync();

            return Json(new { success = true, message = "Password reset successful." });
        }

        /*
        [HttpGet]
        public async Task UpdateAllPasswords()
        {
            // Lấy tất cả các user chưa có mật khẩu đã được băm.
            var passwordHasher = new PasswordHasher<User>();
            var users = await _applicationDbContext.Users.ToListAsync();

            foreach (var user in users)
            {
                user.Password = passwordHasher.HashPassword(user, user.Password);
                user.Rank = "Unknow";
                user.IsBan = false;
            }

            // Lưu lại các thay đổi trong DB.
            await _applicationDbContext.SaveChangesAsync();
        }

        private bool IsPasswordHashed(string password)
        {
            // Kiểm tra kiểu mật khẩu theo cơ chế của Identity
            return password.Contains('$'); // Dấu `$` thường có trong mật khẩu đã băm.
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
        }*/

        private string GenerateOtp()
        {
            var random = new Random();
            return random.Next(100000, 999999).ToString();
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

    public class VerifyOtpDto
    {
        public string email { get; set; }
        public string otp { get; set; }
    }

    public class ResetPasswordDto
    {
        public string email { get; set; }
        public string newPassword { get; set; }
    }

    public class RegisterDto
    {
        public string email { get; set; }
        public string otpCode { get; set; }
    }

    public class Otp
    {
        public int Id { get; set; }
        public string Email { get; set; }
        public string Code { get; set; }
        public DateTime Expiry { get; set; }
    }

}

