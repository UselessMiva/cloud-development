using System.Reflection;
using Amazon.SimpleNotificationService;
using LocalStack.Client.Extensions;
using ResidentialProperty.FileService.Messaging;
using ResidentialProperty.FileService.Services;
using ResidentialProperty.ServiceDefaults;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    var assembly = Assembly.GetExecutingAssembly();
    var xmlPath = Path.Combine(AppContext.BaseDirectory, $"{assembly.GetName().Name}.xml");
    if (File.Exists(xmlPath))
        options.IncludeXmlComments(xmlPath);
});

// LocalStack для SNS
builder.Services.AddLocalStack(builder.Configuration);
builder.Services.AddAwsService<IAmazonSimpleNotificationService>();

// MinIO клиент
builder.AddMinioClient("residentialproperty-minio");

// Регистрация сервисов
builder.Services.AddScoped<IS3Service, MinioService>();
builder.Services.AddScoped<SnsService>();

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var s3Service = scope.ServiceProvider.GetRequiredService<IS3Service>();
    await s3Service.EnsureBucketExists();

    var subscriptionService = scope.ServiceProvider.GetRequiredService<SnsService>();
    await subscriptionService.SubscribeEndpoint();
}

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.MapDefaultEndpoints();
app.MapControllers();

app.Run();
