using SmartAIPropertyCitizen.Api.Application.Services;
using SmartAIPropertyCitizen.Api.Core.Interfaces;
using SmartAIPropertyCitizen.Api.Infrastructure.Data.Repositories;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddHttpClient();

// 1. Core & Infrastructure Layer Registration
builder.Services.AddScoped<ISqlRepository, SqlRepository>();

// 2. Application Layer Registration
builder.Services.AddScoped<IPropertyTaxService, PropertyTaxService>();
builder.Services.AddScoped<ISmartAiService, SmartAiService>();
builder.Services.AddScoped<ICitizenSearchService, CitizenSearchService>();
builder.Services.AddScoped<IOtpService, OtpService>();
builder.Services.AddScoped<ILandingService, LandingService>();

// 3. Presentation Layer Registration
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// 4. CORS Setup
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll",
        builder => builder.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader());
});

var app = builder.Build();

// 5. Middleware Pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseDefaultFiles();
app.UseStaticFiles();
app.UseRouting();
app.UseCors("AllowAll");
app.UseAuthorization();
app.MapControllers();

app.Run();

public partial class Program { }
