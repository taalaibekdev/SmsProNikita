using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace SmsProNikita.Tests;

public sealed class ServiceCollectionExtensionsTests
{
    [Fact]
    public void ClientIsRegistered()
    {
        using var provider = BuildProvider(o =>
        {
            o.Login = "login";
            o.Password = "passwd";
        });

        Assert.IsType<SmsProClient>(provider.GetRequiredService<ISmsProClient>());
    }

    [Theory]
    [InlineData(true, "https://smspro.nikita.kg/api/")]
    [InlineData(false, "http://smspro.nikita.kg/api/")]
    public void HttpClientIsConfiguredFromOptions(bool useSsl, string expectedBaseAddress)
    {
        using var provider = BuildProvider(o =>
        {
            o.Login = "login";
            o.Password = "passwd";
            o.UseSsl = useSsl;
        });

        var httpClient = provider
            .GetRequiredService<IHttpClientFactory>()
            .CreateClient(nameof(ISmsProClient));

        Assert.Equal(new Uri(expectedBaseAddress), httpClient.BaseAddress);
    }

    [Fact]
    public void MissingLoginFailsAtFirstResolution()
    {
        using var provider = BuildProvider(o => o.Password = "passwd");

        Assert.Throws<OptionsValidationException>(() => provider.GetRequiredService<IOptions<SmsProOptions>>().Value);
    }

    [Fact]
    public void MissingPasswordFailsAtFirstResolution()
    {
        using var provider = BuildProvider(o => o.Login = "login");

        Assert.Throws<OptionsValidationException>(() => provider.GetRequiredService<IOptions<SmsProOptions>>().Value);
    }

    [Fact]
    public void HostWithSchemeFailsValidation()
    {
        using var provider = BuildProvider(o =>
        {
            o.Login = "login";
            o.Password = "passwd";
            o.Host = "https://smspro.nikita.kg";
        });

        Assert.Throws<OptionsValidationException>(() => provider.GetRequiredService<IOptions<SmsProOptions>>().Value);
    }

    [Fact]
    public void ZeroTimeoutFailsValidation()
    {
        using var provider = BuildProvider(o =>
        {
            o.Login = "login";
            o.Password = "passwd";
            o.Timeout = TimeSpan.Zero;
        });

        Assert.Throws<OptionsValidationException>(() => provider.GetRequiredService<IOptions<SmsProOptions>>().Value);
    }

    [Fact]
    public void OptionsAreBoundFromConfigurationSection()
    {
        var settings = new Dictionary<string, string?>
        {
            ["SmsPro:Login"] = "config-login",
            ["SmsPro:Password"] = "config-passwd",
            ["SmsPro:UseSsl"] = "false",
            ["SmsPro:DefaultTestMode"] = "true",
        };

        var configuration = new ConfigurationBuilder().AddInMemoryCollection(settings).Build();
        var services = new ServiceCollection();
        services.AddSmsProNikita(configuration);

        using var provider = services.BuildServiceProvider();
        var options = provider.GetRequiredService<IOptions<SmsProOptions>>().Value;

        Assert.Equal("config-login", options.Login);
        Assert.Equal("config-passwd", options.Password);
        Assert.False(options.UseSsl);
        Assert.True(options.DefaultTestMode);
    }

    [Fact]
    public void NullServicesAreRejected()
    {
        IServiceCollection services = null!;

        Assert.Throws<ArgumentNullException>(() => services.AddSmsProNikita(o => o.Login = "login"));
    }

    private static ServiceProvider BuildProvider(Action<SmsProOptions> configure)
    {
        var services = new ServiceCollection();
        services.AddSmsProNikita(configure);
        return services.BuildServiceProvider();
    }
}
