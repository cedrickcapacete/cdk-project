namespace HarborKiosk.Core.Hardware;

public interface IReceiptPrinter
{
    Task PrintAsync(string reference, string receiptText);
}

public sealed class FileReceiptPrinter : IReceiptPrinter
{
    private readonly string _directory;

    public FileReceiptPrinter(string directory)
    {
        _directory = directory;
    }

    public async Task PrintAsync(string reference, string receiptText)
    {
        Directory.CreateDirectory(_directory);
        await File.WriteAllTextAsync(Path.Combine(_directory, $"{reference}.txt"), receiptText);
    }
}
