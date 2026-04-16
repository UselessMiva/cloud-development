using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Ocelot.DependencyInjection;
using Ocelot.Middleware;
using ResidentialProperty.ApiGateway.LoadBalancing;
using ResidentialProperty.ServiceDefaults;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

builder.Configuration.AddJsonFile("ocelot.json", optional: false, reloadOnChange: true);

var replicaNames = builder.Configuration.GetSection("GeneratorServices").Get<string[]>() ?? [];
var replicaWeights = builder.Configuration
    .GetSection("ReplicaWeights")
    .Get<Dictionary<string, int>>() ?? [];

var configOverrides = new List<KeyValuePair<string, string?>>();
var downstreamAddressWeights = new Dictionary<string, int>();

for (var i = 0; i < replicaNames.Length; i++)
{
    var replicaName = replicaNames[i];
    var serviceUrl = builder.Configuration[$"services:{replicaName}:http:0"];

    string downstreamHost, downstreamPort;
    if (!string.IsNullOrEmpty(serviceUrl) && Uri.TryCreate(serviceUrl, UriKind.Absolute, out var uri))
    {
        downstreamHost = uri.Host;
        downstreamPort = uri.Port.ToString();
        configOverrides.Add(new($"Routes:0:DownstreamHostAndPorts:{i}:Host", downstreamHost));
        configOverrides.Add(new($"Routes:0:DownstreamHostAndPorts:{i}:Port", downstreamPort));
    }
    else
    {
        downstreamHost = builder.Configuration[$"Routes:0:DownstreamHostAndPorts:{i}:Host"] ?? "localhost";
        downstreamPort = builder.Configuration[$"Routes:0:DownstreamHostAndPorts:{i}:Port"] ?? "0";
    }

    if (replicaWeights.TryGetValue(replicaName, out var weight))
    {
        downstreamAddressWeights[$"{downstreamHost}:{downstreamPort}"] = weight;
    }
}

if (configOverrides.Count > 0)
    builder.Configuration.AddInMemoryCollection(configOverrides);

builder.Services
    .AddOcelot(builder.Configuration)
    .AddCustomLoadBalancer((serviceProvider, route, serviceDiscovery) =>
        new WeightedRoundRobinBalancer(serviceDiscovery.GetAsync, downstreamAddressWeights));

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

var app = builder.Build();

app.MapDefaultEndpoints();
app.UseHealthChecks("/health");
app.UseHealthChecks("/alive", new HealthCheckOptions { Predicate = r => r.Tags.Contains("live") });
app.UseCors("AllowAll");
await app.UseOcelot();

app.Run();