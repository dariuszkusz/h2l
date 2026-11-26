using System.Net;
using System.Net.Sockets;
using H2l.Application.Connectors.Contracts;
using H2l.Domain.Common;
using H2l.Infrastructure.Options;
using Microsoft.Extensions.Options;

namespace H2l.Infrastructure.Connectors.Security;

public sealed class SsrfGuard : ISsrfGuard
{
    private static readonly HashSet<string> BlockedMetadataHosts = new(StringComparer.OrdinalIgnoreCase)
    {
        "169.254.169.254",
        "169.254.170.2",
        "metadata.google.internal",
        "metadata.goog",
        "metadata",
        "fd00:ec2::254"
    };

    private readonly ConnectorSecurityOptions _options;
    private readonly IDnsResolver _dnsResolver;

    public SsrfGuard(
        IOptions<ConnectorSecurityOptions> options,
        IDnsResolver dnsResolver)
    {
        _options = options.Value;
        _dnsResolver = dnsResolver;
    }

    public async Task<Result<Uri>> ValidateUrlAsync(string url, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(url))
        {
            return Result.Failure<Uri>(Error.Validation("SSRF_URL_REQUIRED", "URL is required."));
        }

        if (!Uri.TryCreate(url.Trim(), UriKind.Absolute, out var uri))
        {
            return Result.Failure<Uri>(Error.Validation("SSRF_URL_INVALID", "URL must be a valid absolute URI."));
        }

        if (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps)
        {
            return Result.Failure<Uri>(Error.Validation(
                "SSRF_SCHEME_NOT_ALLOWED",
                "Only http and https URL schemes are allowed."));
        }

        if (string.IsNullOrWhiteSpace(uri.Host))
        {
            return Result.Failure<Uri>(Error.Validation("SSRF_HOST_REQUIRED", "URL host is required."));
        }

        var normalizedHost = uri.IdnHost.Trim().TrimEnd('.');
        if (BlockedMetadataHosts.Contains(normalizedHost))
        {
            return Result.Failure<Uri>(Error.Validation(
                "SSRF_CLOUD_METADATA_BLOCKED",
                "Access to cloud metadata endpoints is strictly prohibited."));
        }

        IPAddress[] addresses;
        if (IPAddress.TryParse(normalizedHost, out var literalIp))
        {
            addresses = [literalIp];
        }
        else
        {
            try
            {
                addresses = await _dnsResolver.ResolveHostAddressesAsync(normalizedHost, cancellationToken);
            }
            catch (Exception)
            {
                return Result.Failure<Uri>(Error.Validation(
                    "SSRF_DNS_RESOLUTION_FAILED",
                    "Target host could not be resolved."));
            }
        }

        if (addresses.Length == 0)
        {
            return Result.Failure<Uri>(Error.Validation(
                "SSRF_DNS_RESOLUTION_EMPTY",
                "Target host did not resolve to any IP address."));
        }

        var isHostExplicitlyAllowed = _options.AllowedHosts.Any(h =>
            !string.IsNullOrWhiteSpace(h)
            && string.Equals(h.Trim(), normalizedHost, StringComparison.OrdinalIgnoreCase));

        foreach (var rawAddress in addresses)
        {
            var address = rawAddress.IsIPv4MappedToIPv6 ? rawAddress.MapToIPv4() : rawAddress;

            if (IsCloudMetadataOrLinkLocal(address))
            {
                return Result.Failure<Uri>(Error.Validation(
                    "SSRF_CLOUD_METADATA_BLOCKED",
                    "Access to link-local and cloud metadata IP ranges is strictly prohibited."));
            }

            if (IsPrivateOrLoopback(address) && !_options.AllowPrivateNetworks && !isHostExplicitlyAllowed)
            {
                return Result.Failure<Uri>(Error.Validation(
                    "SSRF_PRIVATE_NETWORK_BLOCKED",
                    "Access to private or loopback networks is blocked by SSRF policy."));
            }
        }

        return Result.Success(uri);
    }

    private static bool IsCloudMetadataOrLinkLocal(IPAddress address)
    {
        if (address.AddressFamily == AddressFamily.InterNetwork)
        {
            var bytes = address.GetAddressBytes();
            // 169.254.0.0/16 (includes 169.254.169.254 and 169.254.170.2)
            return bytes[0] == 169 && bytes[1] == 254;
        }

        if (address.AddressFamily == AddressFamily.InterNetworkV6)
        {
            if (address.IsIPv6LinkLocal)
            {
                return true;
            }

            // AWS IPv6 IMDS: fd00:ec2::254
            var bytes = address.GetAddressBytes();
            if (bytes[0] == 0xfd && bytes[1] == 0x00 && bytes[2] == 0x0e && bytes[3] == 0xc2
                && bytes[14] == 0x02 && bytes[15] == 0x54)
            {
                return true;
            }
        }

        return false;
    }

    private static bool IsPrivateOrLoopback(IPAddress address)
    {
        if (IPAddress.IsLoopback(address))
        {
            return true;
        }

        if (address.Equals(IPAddress.Any) || address.Equals(IPAddress.IPv6Any))
        {
            return true;
        }

        if (address.AddressFamily == AddressFamily.InterNetwork)
        {
            var bytes = address.GetAddressBytes();
            return bytes[0] == 0 // 0.0.0.0/8
                || bytes[0] == 10 // 10.0.0.0/8
                || bytes[0] == 127 // 127.0.0.0/8
                || (bytes[0] == 172 && bytes[1] >= 16 && bytes[1] <= 31) // 172.16.0.0/12
                || (bytes[0] == 192 && bytes[1] == 168) // 192.168.0.0/16
                || (bytes[0] == 100 && bytes[1] >= 64 && bytes[1] <= 127) // 100.64.0.0/10 (CGNAT)
                || bytes[0] >= 224; // Multicast & reserved
        }

        if (address.AddressFamily == AddressFamily.InterNetworkV6)
        {
            if (address.IsIPv6Multicast || address.IsIPv6SiteLocal)
            {
                return true;
            }

            var bytes = address.GetAddressBytes();
            // Unique local addresses: fc00::/7
            return (bytes[0] & 0xfe) == 0xfc;
        }

        return true;
    }
}
