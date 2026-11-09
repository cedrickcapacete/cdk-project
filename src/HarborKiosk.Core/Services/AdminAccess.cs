using System.Security.Cryptography;
using System.Text;

namespace HarborKiosk.Core.Services;

public sealed class AdminAccess
{
    public const int MinPinLength = 4;
    public const int MaxPinLength = 8;

    private readonly byte[] _pinHash;

    public AdminAccess(string pin)
    {
        if (!IsWellFormed(pin))
        {
            throw new ArgumentException($"The admin PIN must be {MinPinLength}-{MaxPinLength} digits.", nameof(pin));
        }

        _pinHash = Hash(pin);
    }

    public static bool IsWellFormed(string? pin) =>
        pin is { Length: >= MinPinLength and <= MaxPinLength } && pin.All(c => c is >= '0' and <= '9');

    public bool Verify(string attempt) =>
        IsWellFormed(attempt) && CryptographicOperations.FixedTimeEquals(Hash(attempt), _pinHash);

    private static byte[] Hash(string pin) => SHA256.HashData(Encoding.UTF8.GetBytes(pin));
}
