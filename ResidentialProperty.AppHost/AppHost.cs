using Amazon;
using Aspire.Hosting.LocalStack.Container;

var builder = DistributedApplication.CreateBuilder(args);

// Redis
var redis = builder.AddRedis("redis")
    .WithRedisInsight(containerName: "redis-insight");

// API Gateway
var gateway = builder.AddProject<Projects.ResidentialProperty_ApiGateway>("residentialproperty-apigateway");

// AWS Config
var awsConfig = builder.AddAWSSDKConfig()
    .WithProfile("default")
    .WithRegion(RegionEndpoint.EUCentral1);

// LocalStack
var localstack = builder
    .AddLocalStack("residentialproperty-localstack", awsConfig: awsConfig, configureContainer: container =>
    {
        container.Lifetime = ContainerLifetime.Session;
        container.DebugLevel = 1;
        container.LogLevel = LocalStackLogLevel.Debug;
        container.Port = 4566;
        container.AdditionalEnvironmentVariables.Add("DEBUG", "1");
        container.AdditionalEnvironmentVariables.Add("SNS_CERT_URL_HOST", "sns.eu-central-1.amazonaws.com");
    });

// CloudFormation шаблон
var cloudFormationTemplate = "CloudFormation/residential-property-template-sns.yaml";
var awsResources = builder.AddAWSCloudFormationTemplate("resources", cloudFormationTemplate, "residential-property")
    .WithReference(awsConfig);

// MinIO
var minio = builder.AddMinioContainer("residentialproperty-minio");

// Три экземпляра API
var ports = new[] { 7283, 7284, 7285 };

for (var i = 0; i < ports.Length; i++)
{
    var port = ports[i];
    var service = builder.AddProject<Projects.ResidentialProperty_Api>($"generator-{i + 1}", launchProfileName: null)
        .WithHttpEndpoint(port)
        .WithReference(redis)
        .WithReference(awsResources)
        .WithEnvironment("Settings__MessageBroker", "SNS")
        .WaitFor(redis)
        .WaitFor(awsResources);

    gateway.WaitFor(service);
}

// Client
builder.AddProject<Projects.Client_Wasm>("client-wasm")
    .WaitFor(gateway);

// FileService 
var sink = builder.AddProject<Projects.ResidentialProperty_FileService>("fileservice", launchProfileName: null)
    .WithHttpEndpoint(5280)
    .WithReference(awsResources)
    .WithReference(minio)
    .WithEnvironment("Settings__MessageBroker", "SNS")
    .WithEnvironment("Settings__S3Hosting", "Minio")
    .WithEnvironment("AWS__Resources__SNSUrl", "http://host.docker.internal:5280/api/sns")
    .WithEnvironment("AWS__Resources__MinioBucketName", "residentialfiles")
    .WaitFor(awsResources)
    .WaitFor(minio);

sink.WithEnvironment("AWS__Resources__SNSUrl", "http://host.docker.internal:5280/api/sns");

sink.WithEnvironment("AWS__Resources__MinioBucketName", "residentialfiles")
    .WithReference(minio)
    .WaitFor(minio);

builder.UseLocalStack(localstack);

builder.Build().Run();