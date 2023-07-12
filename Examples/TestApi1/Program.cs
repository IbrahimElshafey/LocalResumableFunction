using ResumableFunctions.AspNetService;
using ResumableFunctions.Handler.InOuts;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

var settings = new SqlServerResumableFunctionsSettings();
settings.CleanDbSettings.CompletedInstanceRetention = TimeSpan.FromSeconds(1);
settings.CleanDbSettings.DeactivatedWaitTemplateRetention = TimeSpan.FromSeconds(1);
settings.CleanDbSettings.PushedCallRetention = TimeSpan.FromSeconds(1);
builder.Services
    .AddControllers()
    .AddResumableFunctions(
       settings
        .SetCurrentServiceUrl("https://localhost:7140/")
        //.SetDllsToScan("ReferenceLibrary")
        );

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();


var app = builder.Build();
app.RegisterCurrentService();
// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
