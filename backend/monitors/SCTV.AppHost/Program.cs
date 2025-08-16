var builder = DistributedApplication.CreateBuilder(args);

var authMigrations = builder.AddProject<Projects.Auth_Migrations>("AuthMigrations");
var streamingMigrations = builder.AddProject<Projects.Streaming_Migrations>("StreamingMigrations");

builder.AddProject<Projects.ApiGateway>("Gateway");

var authApi = builder.AddProject<Projects.Auth_API>("AuthApi")
	.WithReference(authMigrations)
	.WaitForCompletion(authMigrations);

var streamingApi = builder.AddProject<Projects.Streaming_API>("StreamingApi")
	.WithReference(streamingMigrations)
	.WaitForCompletion(streamingMigrations);

builder.Build().Run();
