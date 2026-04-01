using Alba;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;

namespace Tests.Common;

public static class AlbaHostFactory
{
    public static Task<IAlbaHost> CreateAsync<TEntryPoint>(
        string environment = "Testing",
        IDictionary<string, string?>? config = null)
        where TEntryPoint : class
    {
        return AlbaHost.For<TEntryPoint>(builder =>
        {
            builder.UseEnvironment(environment);

            if (config is { Count: > 0 })
            {
                builder.ConfigureAppConfiguration((_, cfg) => cfg.AddInMemoryCollection(config));
            }
        });
    }
}
