using Hangfire;
using ResumableFunctions.AspNetService;
using ResumableFunctions.Handler;
using ResumableFunctions.Handler.InOuts;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.


builder.Services
    .AddControllers()
    .AddResumableFunctions(
        new ResumableFunctionsSettings()
        .UseSqlServer()
        .SetCurrentServiceUrl("https://localhost:7140/"));

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();


var app = builder.Build();
app.ScanCurrentService();
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
