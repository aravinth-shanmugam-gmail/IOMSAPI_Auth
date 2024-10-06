using System;

namespace IOMSAPI_Auth.Models
{
    public class Customer
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string AddressLine { get; set; }
        public string State { get; set; }
        public string City { get; set; }
        public string Country { get; set; }
        public string ZipCode { get; set; }
        public string Phone1 { get; set; }
        public string Phone2 { get; set; }
        public string Email { get; set; }
        public string PasswordHash { get; set; }
        public string OauthProvider { get; set; }
        public string OauthId { get; set; }
        public string OtpCode { get; set; }
        public DateTime? OtpExpiry { get; set; }
    }
}
