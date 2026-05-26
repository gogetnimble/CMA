using System.Security.Cryptography;
using System.Text;

namespace Cma.Common.Extensions;

public static class HashExtensions
{
    public static string GenerateSHA512Hash(this string input)
    {
        var inputBytes = Encoding.UTF8.GetBytes(input);
        var hashBytes = SHA512.HashData(inputBytes);
        var base64Hash = Convert.ToBase64String(hashBytes);

        return base64Hash;
    }
}