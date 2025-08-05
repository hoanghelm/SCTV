var builder = DistributedApplication.CreateBuilder(args);

var authMigrations = builder.AddProject<Projects.Auth_Migrations>("AuthMigrations");

builder.AddProject<Projects.ApiGateway>("Gateway");

var authApi = builder.AddProject<Projects.Auth_API>("AuthApi")
	.WithReference(authMigrations)
	.WaitForCompletion(authMigrations);

builder.Build().Run();
