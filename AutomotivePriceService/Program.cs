
using AutomotivePriceService.Controllers;

namespace AutomotivePriceService;

public class Program
{
    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        AddLogging(builder);

        RegisterComponents(builder);

        builder.Services.AddHostedService<ApplicationLifetimeLogger>();

        builder.Services.AddControllers();

        AddSwashbuckle(builder);

        var app = builder.Build();

        if (app.Environment.IsDevelopment())
        {
            AddSwashbuckleGui(app);
        }

        app.UseHttpsRedirection();

        app.UseAuthorization();


        app.MapControllers();

        app.Run();
    }

    private static void RegisterComponents(WebApplicationBuilder builder)
    {
        builder.Services.AddSingleton<Hasher>();
        builder.Services.AddSingleton<Database>();
        builder.Services.AddSingleton<PriceImportValidator>();
    }

    private static void AddSwashbuckleGui(WebApplication app)
    {
        app.UseSwagger();
        app.UseSwaggerUI();
    }

    private static void AddSwashbuckle(WebApplicationBuilder builder)
    {
        builder.Services.AddEndpointsApiExplorer();
        builder.Services.AddSwaggerGen();
    }

    private static void AddLogging(WebApplicationBuilder builder)
    {
        builder.Logging.ClearProviders();
        builder.Logging.AddConsole();
        builder.Logging.AddDebug();
    }
}
