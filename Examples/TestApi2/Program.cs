using ResumableFunctions.Handler.Core;
using ResumableFunctions.Handler.Helpers;
using ResumableFunctions.MvcUi;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddResumableFunctionsCore(
    new SqlServerResumableFunctionsSettings()
        .SetCurrentServiceUrl("https://localhost:7099/")
        .SetDllsToScan("ReferenceLibrary"));
builder.Services
    .AddControllers()
    .AddResumableFunctionsMvcUi(
);

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();


var app = builder.Build();
app.Services.UseResumableFunctions();
app.UseResumableFunctionsUi();

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

