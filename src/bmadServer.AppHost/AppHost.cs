var builder = DistributedApplication.CreateBuilder(args);

var db = builder.AddPostgres("pgsql")
    .WithPgAdmin()
    .AddDatabase("bmadserver", "bmadserver_dev");

var apiService = builder.AddProject<Projects.bmadServer_ApiService>("apiservice")
    .WithHttpHealthCheck("/health")
    .WithReference(db)
    .WaitFor(db);

builder.AddProject<Projects.bmadServer_Web>("webfrontend")
    .WithExternalHttpEndpoints()
    .WithHttpHealthCheck("/health")
    .WithReference(apiService)
    .WaitFor(apiService);

builder.Build().Run();
