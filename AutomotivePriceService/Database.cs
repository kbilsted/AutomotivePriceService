using AutomotivePriceService.Controllers;

namespace AutomotivePriceService;

public class Database
{
    public Dictionary</* hash */ string, /* payload */ string> CarPricesImportInbox = new();
    public List<DatabasePriceImportModel> CarPrices = new();
}

public class DatabasePriceImportModel
{
    public Guid Id { get; set; }
    public DateTime CreateDate { get; set; }
    public string ImportHash { get; set; } = string.Empty;
    public V1PriceImportModel ModelInfo { get; set; } = new();
}
