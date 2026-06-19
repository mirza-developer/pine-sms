using Microsoft.IdentityModel.Tokens;
using System.Security.Cryptography;
using System.Text;

namespace PineAI.Identity.Utilities;

public static class CryptographyTools
{
    public static string GetHashedStringSha256StringBuilder(string data)
    {
        using var sha256 = SHA256.Create();
        var byteHash = sha256.ComputeHash(Encoding.UTF8.GetBytes(data));
        var sb = new StringBuilder();
        for (int i = 0; i < byteHash.Length; i++)
            sb.Append(byteHash[i].ToString("x2"));
        return sb.ToString();
    }

    public static bool ValidatePasswordInSHA256(string passHash, string password)
    {
        var hash = GetHashedStringSha256StringBuilder(password);
        return hash.Equals(passHash);
    }

    public static SigningCredentials GetJwtCredential(string key)
    {
        return new(GetSymmetricKey(key), SecurityAlgorithms.HmacSha256Signature);
    }

    public static SymmetricSecurityKey GetSymmetricKey(string passKey)
    {
        var key = Encoding.UTF8.GetBytes(passKey);
        return new(key);
    }
}
