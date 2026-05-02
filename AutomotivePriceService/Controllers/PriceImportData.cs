namespace AutomotivePriceService.Controllers;

public class V1PriceImportRoot
{
    public string feedDate { get; set; } = string.Empty;
    public string manufacturer { get; set; } = string.Empty;
    public string currency { get; set; } = string.Empty;
    public V1PriceImportModel[] models { get; set; } = [];
}

public class V1PriceImportModel
{
    public string modelId { get; set; } = string.Empty;
    public string modelName { get; set; } = string.Empty;
    public int priceListDKK { get; set; }
    public string validFrom { get; set; } = string.Empty;
    public DateTime validFromAsDate { get; set; } 
}
