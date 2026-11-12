using System.Security.Cryptography;
using System.Text;

namespace Mes.Common;

/// <summary>
/// 占位口令摘要。接入正式环境时改为带盐的口令哈希。
/// </summary>
public static class PasswordHasher
{
    public static string Hash(string plain)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(plain));
        return Convert.ToHexString(bytes);
    }
}
