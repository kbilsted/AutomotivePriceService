using System.Globalization;

namespace AutomotivePriceService.Controllers;

public class PriceImportValidator
{
    public void ValidateHeader(List<string> errors, V1PriceImportRoot root)
    {
        if (string.IsNullOrWhiteSpace(root.manufacturer))
            errors.Add("Manufacturer is required");
        var feedDate = ParseDate(root.feedDate);
        if (feedDate == null || (feedDate.Value - DateTime.Now).TotalDays > 30)
            errors.Add("Feed date is invalid or too old");
        if (string.IsNullOrWhiteSpace(root.currency))
            errors.Add("Currency is required");
        if (root.currency != "DKK")
            errors.Add("Only DKK currency is supported");
    }

    public void ValidateModel(List<string> errors, V1PriceImportModel model, HashSet<string> seenBefore)
    {
        if (string.IsNullOrWhiteSpace(model.modelId))
        {
            errors.Add("Model ID is required");
        }

        if (seenBefore.Contains(model.modelId))
        {
            errors.Add($"duplicate entry {model.modelId}");
        }
        seenBefore.Add(model.modelId);

        if (string.IsNullOrWhiteSpace(model.modelName))
        {
            errors.Add($"modelname is required for {model.modelId}");
        }

        if (model.priceListDKK < 50000 || model.priceListDKK > 2000000)
        {
            errors.Add($"Illegal price {model.priceListDKK} for model {model.modelId}");
        }

        var validFrom = ParseDate(model.validFrom);
        if (validFrom == null || (validFrom.Value - DateTime.Now).TotalDays > 30)
        {
            errors.Add($"Illegal validFrom {model.validFrom} for model {model.modelId}");
        }
        else
        {
            model.validFromAsDate = validFrom.Value;
        }
    }

    DateTime? ParseDate(string s)
    {
        if (DateTime.TryParseExact(s, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var date))
            return date;
        return null;
    }
}
