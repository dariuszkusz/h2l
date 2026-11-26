namespace H2l.Application.Licensing;

public sealed record LicenseUsage(
    int ActiveProcesses,
    int ActiveConnectors,
    int AdminUsers,
    int EndUsers);
