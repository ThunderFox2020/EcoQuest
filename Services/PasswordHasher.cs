using System.Security.Cryptography;
using System.Text;

namespace EcoQuest.Services
{
    public static class PasswordHasher
    {
        public static string Encrypt(string? password)
        {
            if (password == null)
                password = "";

            MD5 hash = MD5.Create();
            byte[] bytes = hash.ComputeHash(Encoding.UTF8.GetBytes(password));
            StringBuilder encryptedBuilder = new StringBuilder();

            for (int i = 0; i < bytes.Length; i++)
                encryptedBuilder.Append(bytes[i].ToString("x2"));

            return encryptedBuilder.ToString();
        }
    }
}