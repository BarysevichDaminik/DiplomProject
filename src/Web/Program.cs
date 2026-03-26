using System.Text.Json;

using Core.ReportModels;

namespace Web;

public static class Program
{
    private static readonly string[] PossiblePaths =
    [
                "efficiency.json",
                "../../efficiency.json",
                "../../../efficiency.json"
    ];

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNameCaseInsensitive = true,
    };

    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        builder.Services.AddCors(options =>
            options.AddDefaultPolicy(policy =>
                policy.AllowAnyOrigin()
                      .AllowAnyMethod()
                      .AllowAnyHeader()));

        var app = builder.Build();

        app.UseCors();

        app.MapGet("/api/report", async () =>
        {
            string? finalPath = PossiblePaths.FirstOrDefault(File.Exists);

            if (finalPath == null)
            {
                return Results.NotFound(new { error = "Report file not found." });
            }

            try
            {
                var json = await File.ReadAllTextAsync(finalPath);
                var report = JsonSerializer.Deserialize<EfficiencyReport>(json, JsonOptions);
                return Results.Ok(report);
            }
            catch (Exception ex)
            {
                return Results.Problem($"Internal error: {ex.Message}");
            }
        });

        app.MapPost("/api/report", async (EfficiencyReport report) =>
        {
            try
            {
                var json = JsonSerializer.Serialize(report, JsonOptions);
                await File.WriteAllTextAsync("efficiency.json", json);

                Console.WriteLine($"[API] Получен и сохранен новый отчет от {DateTime.Now}");
                return Results.Ok(new { message = "Report successfully updated" });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[API ERROR] {ex.Message}");
                return Results.Problem(ex.Message);
            }
        });

        app.Run();
    }
}