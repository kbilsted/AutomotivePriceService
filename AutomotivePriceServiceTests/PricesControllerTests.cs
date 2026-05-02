using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using AutomotivePriceService;
using AutomotivePriceService.Controllers;
using Microsoft.AspNetCore.Mvc.Testing;

namespace AutomotivePriceServiceTests;

[TestClass]
public sealed class PricesControllerTests
{
    [TestMethod]
    public async Task Import_PostsSamplePriceFileToApi()
    {
        await using var application = new WebApplicationFactory<Program>();
        using var client = application.CreateClient();
        var payload = await File.ReadAllTextAsync(Path.Combine(AppContext.BaseDirectory, "assignment_1_sample_prices.json"));
        using var response = await client.PostAsync("/api/v1/prices/import", new StringContent(payload, Encoding.UTF8, "application/json"));

        Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
        var result = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.AreEqual(4, result.GetProperty("importedCount").GetInt32());
        Assert.AreEqual(4, result.GetProperty("failedCount").GetInt32());

        var errors = result.GetProperty("errors").EnumerateArray().Select(error => error.GetString()).ToArray();
        CollectionAssert.Contains(errors, "Illegal price 0 for model E-208-GT");
        CollectionAssert.Contains(errors, "Illegal price -1 for model 308-SW-ALLURE");
        CollectionAssert.Contains(errors, "Illegal validFrom 2026-13-01 for model 5008-ALLURE-PT130");
        CollectionAssert.Contains(errors, "duplicate entry 208-ACTIVE-PT100");

        // Get at price
        using var responseGet = await client.GetAsync("/api/v1/prices/import/208-ACTIVE-PT100");
        Assert.AreEqual(HttpStatusCode.OK, responseGet.StatusCode);
        PriceResult? priceResult = await responseGet.Content.ReadFromJsonAsync<PriceResult>();
        Assert.AreEqual(199900, priceResult!.PriceInDkk);
    }
}
