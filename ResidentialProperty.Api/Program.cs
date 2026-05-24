using Amazon.SimpleNotificationService;
using LocalStack.Client.Extensions;
using ResidentialProperty.Api.Services.ResidentialPropertyGeneratorService;
using ResidentialProperty.ServiceDefaults;
using ResidentialProperty.Api.Messaging;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

// Redis 
builder.AddRedisDistributedCache("redis");

// AWS SNS через LocalStack
builder.Services.AddLocalStack(builder.Configuration);
builder.Services.AddAwsService<IAmazonSimpleNotificationService>();

// Регистрация сервисов
builder.Services.AddSingleton<ResidentialPropertyGenerator>();
builder.Services.AddScoped<IResidentialPropertyGeneratorService, ResidentialPropertyGeneratorService>();
builder.Services.AddScoped<ISnsPublisher, SnsPublisher>();

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new Microsoft.OpenApi.OpenApiInfo
    {
        Title = "Residential Property Generator API",
        Description = "API для генерации объектов жилого строительства",
        Version = "v1"
    });

    var xmlFilename = $"{System.Reflection.Assembly.GetExecutingAssembly().GetName().Name}.xml";
    var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFilename);
    if (File.Exists(xmlPath))
    {
        options.IncludeXmlComments(xmlPath);
    }

    var domainXmlPath = Path.Combine(AppContext.BaseDirectory, "ResidentialProperty.Domain.xml");
    if (File.Exists(domainXmlPath))
    {
        options.IncludeXmlComments(domainXmlPath);
    }
});

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.MapControllers();
app.MapDefaultEndpoints();

app.Run();
