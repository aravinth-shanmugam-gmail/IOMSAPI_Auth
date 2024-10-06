using System;
using System.Threading.Tasks;
using FirebaseAdmin;
using FirebaseAdmin.Auth;
using Google.Apis.Auth.OAuth2;
using Microsoft.AspNetCore.Builder.Extensions;

namespace IOMSAPI_Auth.Services
{
    public class OtpService
    {
        private readonly Random _random = new Random();

        public OtpService()
        {
            FirebaseApp.Create(new AppOptions()
            {
                Credential = GoogleCredential.FromFile("path/to/serviceAccountKey.json")
            });
        }

        public string GenerateOtp()
        {
            return _random.Next(100000, 999999).ToString();
        }

        public async Task SendOtpAsync(string phoneNumber, string otp)
        {
            try
            {
                var user = await FirebaseAuth.DefaultInstance.GetUserByPhoneNumberAsync(phoneNumber);
                if (user != null)
                {
                    Console.WriteLine($"User already exists: {user.Uid}");
                }
            }
            catch (FirebaseAuthException ex) when (ex.AuthErrorCode == AuthErrorCode.UserNotFound)
            {
                var sessionInfo = await FirebaseAuth.DefaultInstance.CreateSessionCookieAsync(phoneNumber, new SessionCookieOptions());
                Console.WriteLine($"OTP sent to {phoneNumber}. Session Info: {sessionInfo}");
            }
        }
    }
}


