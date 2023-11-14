using ResumableFunctions.AspNetService;
using ResumableFunctions.Handler.Core;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
var settings =
    new SqlServerResumableFunctionsSettings(null, "WaitManyMethodsWithExpression_Test")
    .SetCurrentServiceUrl("https://localhost:7220/");
//settings.CleanDbSettings.CompletedInstanceRetention = TimeSpan.FromSeconds(3);
//settings.CleanDbSettings.DeactivatedWaitTemplateRetention = TimeSpan.FromSeconds(3);
//settings.CleanDbSettings.PushedCallRetention = TimeSpan.FromSeconds(3);
builder.Services
    .AddControllers()
    .AddResumableFunctions(settings);

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

app.UseResumableFunctions();
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
