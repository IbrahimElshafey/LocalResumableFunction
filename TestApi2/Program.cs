using Hangfire;
using ResumableFunctions.AspNetService;
using ResumableFunctions.Core;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddSingleton<IResumableFunctionSettings, Test2RfSettings>();
builder.Services.AddControllers().AddResumableFunctions(new Test2RfSettings());
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();


var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();
app.UseHangfireDashboard();


app.ScanCurrentService("https://localhost:7099/");

app.Run();
