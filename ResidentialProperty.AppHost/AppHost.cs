var builder = DistributedApplication.CreateBuilder(args);

// Добавляем Redis
var redis = builder.AddRedis("redis")
    .WithRedisCommander();


var api0 = builder.AddProject<Projects.ResidentialProperty_Api>("residentialproperty-api-0")
    .WithReference(redis)
    .WithEnvironment("ASPNETCORE_URLS", "http://localhost:7283")
    .WithEndpoint("http", endpoint =>
    {
        endpoint.IsProxied = false;
        endpoint.Port = 7283;
        endpoint.TargetPort = 7283;
    })
    .WaitFor(redis);

var api1 = builder.AddProject<Projects.ResidentialProperty_Api>("residentialproperty-api-1")
    .WithReference(redis)
    .WithEnvironment("ASPNETCORE_URLS", "http://localhost:7284")
    .WithEndpoint("http", endpoint =>
    {
        endpoint.IsProxied = false;
        endpoint.Port = 7284;
        endpoint.TargetPort = 7284;
    })
    .WaitFor(redis);

var api2 = builder.AddProject<Projects.ResidentialProperty_Api>("residentialproperty-api-2")
    .WithReference(redis)
    .WithEnvironment("ASPNETCORE_URLS", "http://localhost:7285")
    .WithEndpoint("http", endpoint =>
    {
        endpoint.IsProxied = false;
        endpoint.Port = 7285;
        endpoint.TargetPort = 7285;
    })
    .WaitFor(redis);

var gateway = builder.AddProject<Projects.ResidentialProperty_ApiGateway>("residentialproperty-apigateway")
    .WithReference(api0)
    .WithReference(api1)
    .WithReference(api2);


// Добавляем клиентский проект
builder.AddProject<Projects.Client_Wasm>("client-wasm")
    .WithReference(gateway)
    .WaitFor(gateway);


builder.Build().Run();