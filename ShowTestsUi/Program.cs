using ResumableFunctions.AspNetService;
using ResumableFunctions.Handler.InOuts;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services
    .AddControllers()
    .AddResumableFunctions(
        new SqlServerResumableFunctionsSettings(null, "ReplayInSubFunction_Test"));

builder.Services.AddControllers();

var app = builder.Build();

app.RegisterCurrentService();
// Configure the HTTP request pipeline.

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
