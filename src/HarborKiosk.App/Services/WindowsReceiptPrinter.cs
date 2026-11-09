using System.Printing;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Documents;
using System.Windows.Media;
using HarborKiosk.Core.Hardware;

namespace HarborKiosk.App.Services;

/// <summary>Prints to the default Windows print queue (e.g. an 80mm thermal receipt printer). Must run on the UI thread.</summary>
public sealed class WindowsReceiptPrinter : IReceiptPrinter
{
    private const double ReceiptWidth = 280;

    public Task PrintAsync(string reference, string receiptText)
    {
        var queue = LocalPrintServer.GetDefaultPrintQueue();
        var document = new FlowDocument(new Paragraph(new Run(receiptText)))
        {
            FontFamily = new FontFamily("Consolas"),
            FontSize = 10,
            PageWidth = ReceiptWidth,
            ColumnWidth = ReceiptWidth,
            PagePadding = new Thickness(6),
        };

        var writer = PrintQueue.CreateXpsDocumentWriter(queue);
        writer.Write(((IDocumentPaginatorSource)document).DocumentPaginator);
        return Task.CompletedTask;
    }
}
