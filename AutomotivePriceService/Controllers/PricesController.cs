using Microsoft.AspNetCore.Mvc;
using System.Text.Json;

namespace AutomotivePriceService.Controllers;

[ApiController]
[Route("/api/v1/[controller]")]
public class PricesController(
    Hasher hasher,
    Database database,
    PriceImportValidator validator,
    ILogger<PricesController> logger) : ControllerBase
{
    /// <summary>
    /// Modtage filen og beregne en hash for indhold
    /// placer indhold i en database tabel "inbox". Dette sikrer duplet detektion
    /// Valider header record
    /// Valider hver record en af gangen og indsæt i database tabel "carprices" hvis valid ellers ignorer record
    /// </summary>
    [HttpPost("import")]
    public ActionResult<PriceImportResult> Import([FromBody] JsonElement payload)
    {
        logger.LogInformation("called post api/v1/pricescontroller/import/");
        if (payload.ValueKind is JsonValueKind.Undefined or JsonValueKind.Null)
        {
            logger.LogError("Payload cannot be null");
            return BadRequest("Payload cannot be null");
        }

        var rawPayload = payload.GetRawText();
        var payloadHash = hasher.StableHash(rawPayload);

        V1PriceImportRoot? parsedPayload = JsonSerializer.Deserialize<V1PriceImportRoot>(rawPayload);

        if (database.CarPricesImportInbox.ContainsKey(payloadHash))
        {
            var msg = $"Duplicate payload for {parsedPayload?.manufacturer}";
            logger.LogWarning(msg);
            return new PriceImportResult { Errors = [msg] };
        }

        database.CarPricesImportInbox.Add(payloadHash, rawPayload);

        if (parsedPayload == null)
            return new PriceImportResult { Errors = ["Invalid payload format"] };

        var errors = new List<string>();

        validator.ValidateHeader(errors, parsedPayload!);
        if (errors.Any())
            return BadRequest(errors);

        HashSet<string> seenBefore = new();
        int importCount = 0;
        foreach (var model in parsedPayload.models ?? [])
        {
            int errorsCount = errors.Count;
            validator.ValidateModel(errors, model, seenBefore);
            if (errors.Count > errorsCount)
                continue;

            var record = new DatabasePriceImportModel()
            {
                Id = Guid.NewGuid(),
                CreateDate = DateTime.Now,
                ImportHash = payloadHash,
                ModelInfo = model,
            };
            database.CarPrices.Add(record);

            importCount++;
        }

        var result = new PriceImportResult
        {
            ImportedCount = importCount,
            FailedCount = errors.Count,
            Errors = errors.ToArray()
        };
        logger.LogInformation($"Import finished with {result.ImportedCount} import and {errors.Count} errors");

        return result;
    }

    [HttpGet("import/{modelId}")]
    public ActionResult<PriceResult> GetPrice(string modelId)
    {
        logger.LogInformation("called get api/v1/pricescontroller/import/" + modelId);

        if (string.IsNullOrWhiteSpace(modelId))
            return BadRequest("Model ID cannot be null");

        var price =
            database.CarPrices
                .Where(x => x.ModelInfo.modelId == modelId && x.ModelInfo.validFromAsDate < DateTime.Now)
                .OrderBy(x => x.ModelInfo.validFromAsDate)
                .ThenBy(x => x.CreateDate)
                .LastOrDefault();
        
        if (price == null)
            return NotFound();

        return new PriceResult
        {
            ImportHash = price.ImportHash,
            PriceInDkk = price.ModelInfo.priceListDKK,
        };
    }
}