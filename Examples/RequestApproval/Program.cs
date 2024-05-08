using RequestApproval.Controllers;
using ResumableFunctions.AspNetService;
using ResumableFunctions.Handler.Core;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services
    .AddControllers()
    .AddResumableFunctionsUi(
    new SqlServerResumableFunctionsSettings(null, "RequestApprovalWaitsDB")
    .SetCurrentServiceUrl("https://localhost:7003"));
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddScoped<IRequestApprovalService, RequestApprovalService>();
builder.Services.AddScoped<RequestApprovalWorkflow>();

var app = builder.Build();
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
