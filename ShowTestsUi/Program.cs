using ResumableFunctions.AspNetService;
using ResumableFunctions.Handler.InOuts;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
var settings = 
    new SqlServerResumableFunctionsSettings(null, "SequenceFunction_Test")
    .SetCurrentServiceUrl("https://localhost:7219/");
//settings.CleanDbSettings.CompletedInstanceRetention = TimeSpan.FromSeconds(3);
//settings.CleanDbSettings.DeactivatedWaitTemplateRetention = TimeSpan.FromSeconds(3);
//settings.CleanDbSettings.PushedCallRetention = TimeSpan.FromSeconds(3);
builder.Services
    .AddControllers()
    .AddResumableFunctions(settings);

builder.Services.AddControllers();

var app = builder.Build();

app.UseResumableFunctions();
// Configure the HTTP request pipeline.

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
