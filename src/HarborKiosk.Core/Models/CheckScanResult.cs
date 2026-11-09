namespace HarborKiosk.Core.Models;

public sealed record CheckScanResult(byte[]? FrontImage, byte[]? BackImage, string? MicrLine);
