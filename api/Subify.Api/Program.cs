using System.Reflection;
using Scalar.AspNetCore;
using Subify.Api.Common.Extensions;
using Subify.Application;
using Subify.Infrastructure;

namespace Subify.Api;

public class Program
{
    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        builder.Services.AddApplicationServices();
        builder.Services.AddInfrastructureServices(builder.Configuration);

        builder.Services.AddHttpContextAccessor();
        builder.Services.AddEndpoints(Assembly.GetExecutingAssembly());

        builder.Services.AddOpenApi();

        var app = builder.Build();

        if (app.Environment.IsDevelopment())
        {
            app.MapOpenApi();
            app.MapScalarApiReference(options =>
            {
                options
                    .WithTitle("Subify OS API")
                    .WithTheme(ScalarTheme.Purple)
                    .WithDefaultHttpClient(ScalarTarget.CSharp, ScalarClient.HttpClient);
            });

            // Convenience: open API docs at root in development
            app.MapGet("/", () => Results.Redirect("/scalar/v1"))
                .ExcludeFromDescription();
        }

        // Avoid forcing HTTPS redirect when running the local http profile
        if (!app.Environment.IsDevelopment())
        {
            app.UseHttpsRedirection();
        }

        app.UseAuthentication();
        app.UseAuthorization();

        app.MapEndpoints();

        app.Run();
    }
}
