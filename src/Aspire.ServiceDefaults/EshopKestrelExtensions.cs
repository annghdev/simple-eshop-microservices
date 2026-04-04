using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Server.Kestrel.Core;

namespace Microsoft.AspNetCore.Builder;

/// <summary>
/// In Docker: HTTP/1.1 on 8080 (REST, metrics); optional gRPC uses HTTP/2 cleartext on 8082 (h2c cannot share a port with HTTP/1.1 without TLS).
/// Outside containers: Http1AndHttp2 on launch URLs (HTTPS dev or single HTTP port).
/// </summary>
public static class EshopKestrelExtensions
{
    public static WebApplicationBuilder ConfigureEshopDockerKestrel(
        this WebApplicationBuilder builder,
        bool grpcOnDedicatedPort8082 = false)
    {
        if (grpcOnDedicatedPort8082)
        {
            AppContext.SetSwitch("System.Net.Http.SocketsHttpHandler.Http2UnencryptedSupport", true);
        }

        // UseKestrel works with ConfigureWebHostBuilder; ConfigureKestrel extension targets IWebHostBuilder only.
        builder.WebHost.UseKestrel((_, options) =>
        {
            if (RunningInContainer())
            {
                options.ListenAnyIP(8080, lo => lo.Protocols = HttpProtocols.Http1);
                if (grpcOnDedicatedPort8082)
                {
                    options.ListenAnyIP(8082, lo => lo.Protocols = HttpProtocols.Http2);
                }
            }
            else
            {
                options.ConfigureEndpointDefaults(lo => lo.Protocols = HttpProtocols.Http1AndHttp2);
            }
        });

        return builder;
    }

    private static bool RunningInContainer() =>
        string.Equals(Environment.GetEnvironmentVariable("DOTNET_RUNNING_IN_CONTAINER"), "true", StringComparison.OrdinalIgnoreCase);
}
