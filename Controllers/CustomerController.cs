using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using IOMSAPI_Auth.Models;
using IOMSAPI_Auth.Data;
using IOMSAPI_Auth.Services;

namespace IOMSAPI_Auth.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class CustomerController : ControllerBase
    {
        private readonly DatabaseContext _dbContext;
        private readonly OtpService _otpService;
        private static readonly ConcurrentDictionary<string, int> RegistrationAttempts = new ConcurrentDictionary<string, int>();
        private static readonly ConcurrentDictionary<string, RegisterRequest> TempRegistrations = new ConcurrentDictionary<string, RegisterRequest>();

        public CustomerController(DatabaseContext dbContext, OtpService otpService)
        {
            _dbContext = dbContext;
            _otpService = otpService;
        }

        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] RegisterRequest request)
        {
            if (_dbContext.Customers.Any(c => c.Email == request.Email || c.Phone1 == request.Phone1))
            {
                return BadRequest("Email or phone number already exists.");
            }

            if (RegistrationAttempts.TryGetValue(request.Phone1, out int attempts) && attempts >= 3)
            {
                return BadRequest("Rate limit exceeded. Please try again later.");
            }

            var otp = _otpService.GenerateOtp();
            TempRegistrations[request.Phone1] = request;
            RegistrationAttempts.AddOrUpdate(request.Phone1, 1, (key, oldValue) => oldValue + 1);

            await _otpService.SendOtpAsync(request.Phone1, otp);

            return Ok("Registration initiated. Please verify your phone number.");
        }

        [HttpPost("validate-otp")]
        public IActionResult ValidateOtp([FromBody] OtpValidationRequest request)
        {
            if (!TempRegistrations.TryGetValue(request.Phone1, out var tempRegistration))
            {
                return BadRequest("No registration found for this phone number.");
            }

            var customer = _dbContext.Customers.FirstOrDefault(c => c.Phone1 == request.Phone1);
            if (customer == null || customer.OtpExpiry < DateTime.UtcNow || customer.OtpCode != request.OtpCode)
            {
                return BadRequest("Invalid or expired OTP.");
            }

            var newCustomer = new Customer
            {
                Name = tempRegistration.Name,
                Phone1 = tempRegistration.Phone1,
                Email = tempRegistration.Email,
                PasswordHash = HashPassword(tempRegistration.Password),
                OtpCode = null,
                OtpExpiry = null
            };

            _dbContext.Customers.Add(newCustomer);
            _dbContext.SaveChanges();

            TempRegistrations.TryRemove(request.Phone1, out _);
            RegistrationAttempts.TryRemove(request.Phone1, out _);

            return Ok("Phone number validated and registration completed successfully.");
        }

        private string HashPassword(string password)
        {
            using (var sha256 = SHA256.Create())
            {
                var bytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(password));
                return Convert.ToBase64String(bytes);
            }
        }
    }

    public class RegisterRequest
    {
        public string Name { get; set; }
        public string Phone1 { get; set; }
        public string Email { get; set; }
        public string Password { get; set; }
    }

    public class OtpValidationRequest
    {
        public string Phone1 { get; set; }
        public string OtpCode { get; set; }
    }
}
