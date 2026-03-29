using System.Collections;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;

namespace ImmichFrame.WebApi.Services;

public interface IAdminBasicAuthService
{
    bool HasUsers { get; }
    bool ValidateCredentials(string username, string password);
    string? GetCredentialVersion(string username);
}

public class AdminBasicAuthService : IAdminBasicAuthService
{
    private static readonly Regex EnvironmentKeyPattern = new(
        "^IMMICHFRAME_AUTH_BASIC_(?<name>[A-Z0-9_]+)_(?<kind>USER|HASH)$",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    private readonly List<AdminBasicAuthUser> _users;

    public AdminBasicAuthService()
        : this(Environment.GetEnvironmentVariables())
    {
    }

    internal AdminBasicAuthService(IDictionary environmentVariables)
    {
        _users = LoadUsers(environmentVariables);
    }

    public bool HasUsers => _users.Count > 0;

    public bool ValidateCredentials(string username, string password)
    {
        foreach (var user in _users)
        {
            if (!string.Equals(user.Username, username, StringComparison.Ordinal))
            {
                continue;
            }

            return VerifyPassword(password, user.Hash);
        }

        return false;
    }

    public string? GetCredentialVersion(string username)
    {
        var user = _users.FirstOrDefault(x => string.Equals(x.Username, username, StringComparison.Ordinal));
        return user == null ? null : ComputeCredentialVersion(user.Hash);
    }

    internal static List<AdminBasicAuthUser> LoadUsers(IDictionary environmentVariables)
    {
        var partialUsers = new Dictionary<string, PartialAdminBasicAuthUser>(StringComparer.OrdinalIgnoreCase);

        foreach (DictionaryEntry entry in environmentVariables)
        {
            var key = entry.Key?.ToString();
            var value = entry.Value?.ToString();
            if (string.IsNullOrWhiteSpace(key))
            {
                continue;
            }

            var match = EnvironmentKeyPattern.Match(key);
            if (!match.Success)
            {
                continue;
            }

            var name = match.Groups["name"].Value;
            var kind = match.Groups["kind"].Value;
            if (!partialUsers.TryGetValue(name, out var partialUser))
            {
                partialUser = new PartialAdminBasicAuthUser();
                partialUsers[name] = partialUser;
            }

            if (string.Equals(kind, "USER", StringComparison.OrdinalIgnoreCase))
            {
                partialUser.Username = value;
            }
            else if (string.Equals(kind, "HASH", StringComparison.OrdinalIgnoreCase))
            {
                partialUser.Hash = value;
            }
        }

        var users = new List<AdminBasicAuthUser>();
        var usernamesToSource = new Dictionary<string, string>(StringComparer.Ordinal);

        foreach (var (sourceName, partialUser) in partialUsers)
        {
            if (string.IsNullOrWhiteSpace(partialUser.Username) || string.IsNullOrWhiteSpace(partialUser.Hash))
            {
                continue;
            }

            var username = partialUser.Username.Trim();
            var hash = partialUser.Hash.Trim();

            if (usernamesToSource.TryGetValue(username, out var existingSource))
            {
                throw new InvalidOperationException(
                    $"Duplicate admin username '{username}' found in IMMICHFRAME_AUTH_BASIC_{existingSource}_USER and IMMICHFRAME_AUTH_BASIC_{sourceName}_USER.");
            }

            usernamesToSource[username] = sourceName;
            users.Add(new AdminBasicAuthUser(username, hash));
        }

        return users;
    }

    private static bool VerifyPassword(string password, string hash)
    {
        if (hash.StartsWith("$2", StringComparison.Ordinal))
        {
            try
            {
                return BCrypt.Net.BCrypt.Verify(password, hash);
            }
            catch (Exception)
            {
                return false;
            }
        }

        if (hash.StartsWith("$apr1$", StringComparison.Ordinal))
        {
            var computed = ApacheMd5Crypt.Hash(password, hash);
            return FixedTimeEquals(computed, hash);
        }

        if (hash.StartsWith("{SHA}", StringComparison.Ordinal))
        {
            // "{SHA}" is the legacy htpasswd SHA1 format. Keep it only for compatibility and
            // prefer stronger hashes such as bcrypt ("$2") or Apache MD5 ("$apr1$") in new configs.
            using var sha1 = SHA1.Create();
            var digest = sha1.ComputeHash(Encoding.UTF8.GetBytes(password));
            var computed = "{SHA}" + Convert.ToBase64String(digest);
            return FixedTimeEquals(computed, hash);
        }

        return false;
    }

    private static bool FixedTimeEquals(string left, string right)
    {
        var leftBytes = Encoding.UTF8.GetBytes(left);
        var rightBytes = Encoding.UTF8.GetBytes(right);
        return CryptographicOperations.FixedTimeEquals(leftBytes, rightBytes);
    }

    private static string ComputeCredentialVersion(string hash)
    {
        var digest = SHA256.HashData(Encoding.UTF8.GetBytes(hash));
        return Convert.ToHexString(digest);
    }

    internal sealed record AdminBasicAuthUser(string Username, string Hash);

    private sealed class PartialAdminBasicAuthUser
    {
        public string? Username { get; set; }
        public string? Hash { get; set; }
    }
}

internal static class ApacheMd5Crypt
{
    private const string Magic = "$apr1$";
    private const string Itoa64 = "./0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz";

    public static string Hash(string password, string hashOrSalt)
    {
        var salt = ExtractSalt(hashOrSalt);
        var passwordBytes = Encoding.UTF8.GetBytes(password);
        var saltBytes = Encoding.UTF8.GetBytes(salt);

        var initial = new List<byte>();
        initial.AddRange(passwordBytes);
        initial.AddRange(Encoding.ASCII.GetBytes(Magic));
        initial.AddRange(saltBytes);

        var alternate = Md5(passwordBytes, saltBytes, passwordBytes);
        for (var remaining = passwordBytes.Length; remaining > 0; remaining -= 16)
        {
            initial.AddRange(alternate.Take(Math.Min(16, remaining)));
        }

        for (var length = passwordBytes.Length; length > 0; length >>= 1)
        {
            initial.Add((length & 1) == 1 ? (byte)0 : passwordBytes[0]);
        }

        var final = Md5(initial.ToArray());

        for (var i = 0; i < 1000; i++)
        {
            var round = new List<byte>();

            if ((i & 1) == 1)
            {
                round.AddRange(passwordBytes);
            }
            else
            {
                round.AddRange(final);
            }

            if (i % 3 != 0)
            {
                round.AddRange(saltBytes);
            }

            if (i % 7 != 0)
            {
                round.AddRange(passwordBytes);
            }

            if ((i & 1) == 1)
            {
                round.AddRange(final);
            }
            else
            {
                round.AddRange(passwordBytes);
            }

            final = Md5(round.ToArray());
        }

        var encoded = string.Concat(
            To64((final[0] << 16) | (final[6] << 8) | final[12], 4),
            To64((final[1] << 16) | (final[7] << 8) | final[13], 4),
            To64((final[2] << 16) | (final[8] << 8) | final[14], 4),
            To64((final[3] << 16) | (final[9] << 8) | final[15], 4),
            To64((final[4] << 16) | (final[10] << 8) | final[5], 4),
            To64(final[11], 2));

        return $"{Magic}{salt}${encoded}";
    }

    private static byte[] Md5(params byte[][] buffers)
    {
        using var md5 = MD5.Create();
        foreach (var buffer in buffers)
        {
            md5.TransformBlock(buffer, 0, buffer.Length, null, 0);
        }

        md5.TransformFinalBlock([], 0, 0);
        return md5.Hash ?? [];
    }

    private static string ExtractSalt(string hashOrSalt)
    {
        var value = hashOrSalt.StartsWith(Magic, StringComparison.Ordinal)
            ? hashOrSalt[Magic.Length..]
            : hashOrSalt;

        var dollarIndex = value.IndexOf('$');
        if (dollarIndex >= 0)
        {
            value = value[..dollarIndex];
        }

        return value.Length > 8 ? value[..8] : value;
    }

    private static string To64(int value, int length)
    {
        var builder = new StringBuilder(length);
        while (length-- > 0)
        {
            builder.Append(Itoa64[value & 0x3f]);
            value >>= 6;
        }

        return builder.ToString();
    }
}
