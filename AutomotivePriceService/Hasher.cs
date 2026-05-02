using System.Security.Cryptography;
using System.Text;

namespace AutomotivePriceService;

public class Hasher
{
    public string StableHash(string value)
    {
        byte[] bytes = Encoding.UTF8.GetBytes(value);
        byte[] hash = SHA256.HashData(bytes);

        return Convert.ToHexString(hash);
    }
}