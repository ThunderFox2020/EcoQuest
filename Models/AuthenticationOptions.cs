using Microsoft.IdentityModel.Tokens;
using System.Text;

namespace EcoQuest.Models
{
    public static class AuthenticationOptions
    {
        public const string ISSUER = "Backend";
        public const string AUDIENCE = "Frontend";
        private const string KEY = "MySuperSecretKey";

        public static SymmetricSecurityKey GetSymmetricSecurityKey() => new SymmetricSecurityKey(Encoding.UTF8.GetBytes(KEY));
    }
}