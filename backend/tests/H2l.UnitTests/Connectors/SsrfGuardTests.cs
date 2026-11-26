using System.Net;
using H2l.Infrastructure.Connectors.Security;
using H2l.Infrastructure.Options;
using Microsoft.Extensions.Options;
using NSubstitute;
using Xunit;

namespace H2l.UnitTests.Connectors;

public sealed class SsrfGuardTests
{
    private readonly IDnsResolver _dnsResolver = Substitute.For<IDnsResolver>();

    [Theory]
    [InlineData("http://169.254.169.254/latest/meta-data/")]
    [InlineData("http://169.254.170.2/v2/credentials")]
    [InlineData("http://metadata.google.internal/computeMetadata/v1/")]
    [InlineData("http://[fd00:ec2::254]/latest/meta-data/")]
    public async Task ValidateUrlAsync_AlwaysBlocksCloudMetadataEvenWhenPrivateNetworksAllowed(string url)
    {
        var sut = new SsrfGuard(
            Options.Create(new ConnectorSecurityOptions
            {
                AllowPrivateNetworks = true,
                AllowedHosts = ["169.254.169.254", "metadata.google.internal"]
            }),
            _dnsResolver);

        var result = await sut.ValidateUrlAsync(url);

        Assert.True(result.IsFailure);
        Assert.Equal("SSRF_CLOUD_METADATA_BLOCKED", result.Error.Code);
    }

    [Theory]
    [InlineData("file:///etc/passwd")]
    [InlineData("ftp://example.com/resource")]
    [InlineData("gopher://127.0.0.1:6379/_INFO")]
    public async Task ValidateUrlAsync_BlocksNonHttpSchemes(string url)
    {
        var sut = new SsrfGuard(Options.Create(new ConnectorSecurityOptions()), _dnsResolver);

        var result = await sut.ValidateUrlAsync(url);

        Assert.True(result.IsFailure);
        Assert.Equal("SSRF_SCHEME_NOT_ALLOWED", result.Error.Code);
    }

    [Theory]
    [InlineData("http://127.0.0.1:5678")]
    [InlineData("http://10.0.0.12:8080")]
    [InlineData("http://172.16.5.10:8080")]
    [InlineData("http://192.168.1.50:5678")]
    [InlineData("http://[::1]:8080")]
    public async Task ValidateUrlAsync_BlocksPrivateAndLoopbackByDefault(string url)
    {
        var sut = new SsrfGuard(
            Options.Create(new ConnectorSecurityOptions { AllowPrivateNetworks = false }),
            _dnsResolver);

        var result = await sut.ValidateUrlAsync(url);

        Assert.True(result.IsFailure);
        Assert.Equal("SSRF_PRIVATE_NETWORK_BLOCKED", result.Error.Code);
    }

    [Fact]
    public async Task ValidateUrlAsync_BlocksDnsHostResolvingToPrivateIpWhenNotAllowed()
    {
        _dnsResolver.ResolveHostAddressesAsync("internal-n8n.local", Arg.Any<CancellationToken>())
            .Returns([IPAddress.Parse("10.1.2.3")]);

        var sut = new SsrfGuard(
            Options.Create(new ConnectorSecurityOptions { AllowPrivateNetworks = false }),
            _dnsResolver);

        var result = await sut.ValidateUrlAsync("https://internal-n8n.local/webhook");

        Assert.True(result.IsFailure);
        Assert.Equal("SSRF_PRIVATE_NETWORK_BLOCKED", result.Error.Code);
    }

    [Fact]
    public async Task ValidateUrlAsync_AllowsPrivateIpWhenSelfHostedPrivateNetworksEnabled()
    {
        _dnsResolver.ResolveHostAddressesAsync("n8n", Arg.Any<CancellationToken>())
            .Returns([IPAddress.Parse("172.18.0.4")]);

        var sut = new SsrfGuard(
            Options.Create(new ConnectorSecurityOptions { AllowPrivateNetworks = true }),
            _dnsResolver);

        var result = await sut.ValidateUrlAsync("http://n8n:5678");

        Assert.True(result.IsSuccess);
        Assert.Equal("http://n8n:5678/", result.Value.ToString());
    }

    [Fact]
    public async Task ValidateUrlAsync_AllowsExplicitlyAllowedHostEvenWhenPrivateNetworksDisabled()
    {
        _dnsResolver.ResolveHostAddressesAsync("zeebe.internal", Arg.Any<CancellationToken>())
            .Returns([IPAddress.Parse("10.0.5.9")]);

        var sut = new SsrfGuard(
            Options.Create(new ConnectorSecurityOptions
            {
                AllowPrivateNetworks = false,
                AllowedHosts = ["zeebe.internal"]
            }),
            _dnsResolver);

        var result = await sut.ValidateUrlAsync("https://zeebe.internal:8080");

        Assert.True(result.IsSuccess);
    }

    [Fact]
    public async Task ValidateUrlAsync_AllowsPublicIpAddress()
    {
        _dnsResolver.ResolveHostAddressesAsync("cloud.n8n.io", Arg.Any<CancellationToken>())
            .Returns([IPAddress.Parse("93.184.216.34")]);

        var sut = new SsrfGuard(
            Options.Create(new ConnectorSecurityOptions { AllowPrivateNetworks = false }),
            _dnsResolver);

        var result = await sut.ValidateUrlAsync("https://cloud.n8n.io/webhook/test");

        Assert.True(result.IsSuccess);
    }
}
