namespace H2l.Application.Licensing;

public sealed record LicenseLimits(
    int? MaxActiveProcesses,
    int? MaxActiveConnectors,
    int? MaxAdminUsers,
    int? MaxEndUsers);
