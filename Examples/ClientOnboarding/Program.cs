using ClientOnboarding.Services;
using ClientOnboarding.Workflow;
using ResumableFunctions.AspNetService;
using ResumableFunctions.Handler.InOuts;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services
    .AddControllers()
    .AddResumableFunctions(new ResumableFunctionsSettings().UseSqlServer());
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddTransient<IClientOnboardingService, ClientOnboardingService>();
//builder.Services.AddScoped<ClientOnboardingWorkflow>();

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
