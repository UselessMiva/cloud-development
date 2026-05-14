var builder = DistributedApplication.CreateBuilder(args);

// Добавляем Redis
var redis = builder.AddRedis("redis")
    .WithRedisCommander();

var gateway = builder.AddProject<Projects.ResidentialProperty_ApiGateway>("residentialproperty-apigateway");
var ports = new[] { 7283, 7284, 7285 };

for (var i = 0; i < ports.Length; i++)
{
    var port = ports[i].ToString();
    var api = builder.AddProject<Projects.ResidentialProperty_Api>($"residentialproperty-api-{i}")
        .WithReference(redis)
        .WithEnvironment("ASPNETCORE_URLS", $"http://localhost:{port}")
        .WithEndpoint("http", endpoint =>
        {
            endpoint.IsProxied = false;
            endpoint.Port = ports[i];
            endpoint.TargetPort = ports[i];
        })
        .WaitFor(redis);

    gateway.WithReference(api).WaitFor(api);
}

// Добавляем клиентский проект
builder.AddProject<Projects.Client_Wasm>("client-wasm")
    .WaitFor(gateway);

builder.Build().Run();