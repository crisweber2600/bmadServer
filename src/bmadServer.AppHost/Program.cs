var builder = DistributedApplication.CreateBuilder(args);

var apiService = builder.AddProject<Projects.bmadServer_ApiService>("apiservice");

builder.AddProject<Projects.bmadServer_Web>("webfrontend")
    .WithExternalHttpEndpoints()
    .WithReference(apiService);

builder.Build().Run();
