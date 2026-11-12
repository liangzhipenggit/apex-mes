namespace Mes.Models;

public sealed class LabelPrintData
{
    public string MaterialCode { get; set; } = string.Empty;

    public string BatchNo { get; set; } = string.Empty;

    public string Barcode { get; set; } = string.Empty;

    public int Copies { get; set; } = 1;
}

public readonly record struct PrintOutcome(bool Success, string Message);
