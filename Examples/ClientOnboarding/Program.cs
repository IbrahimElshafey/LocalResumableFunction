using ClientOnboarding.Services;
using ResumableFunctions.AspNetService;
using ResumableFunctions.Handler.InOuts;
using System.Diagnostics;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services
    .AddControllers()
    .AddResumableFunctions(
        new SqlServerResumableFunctionsSettings()
        .SetCurrentServiceUrl("https://localhost:7262"));
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddScoped<IClientOnboardingService, ClientOnboardingService>();
//builder.Services.AddScoped<ClientOnboardingWorkflow>();

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
try
{
    app.Run();
}
catch (Exception ex)
{
    Debug.Write(ex);
    throw;
}
