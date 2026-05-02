namespace AutomotivePriceService.Controllers;

public class PriceImportResult
{
    public int ImportedCount { get; internal set; }
    public int FailedCount { get; internal set; }
    public string[] Errors { get; internal set; } = [];
}
