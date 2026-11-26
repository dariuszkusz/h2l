namespace H2l.Infrastructure.Options;

public sealed class ConnectorSecurityOptions
{
    public const string SectionName = "Connectors";

    public bool AllowPrivateNetworks { get; set; }

    public List<string> AllowedHosts { get; set; } = [];

    public int MaxOutboxRetries { get; set; } = 5;

    public int OutboxPollIntervalSeconds { get; set; } = 15;

    public int TaskExpirationPollIntervalSeconds { get; set; } = 60;

    public int DataRetentionPollIntervalMinutes { get; set; } = 60;

    public int CamundaPollIntervalSeconds { get; set; } = 10;

    public int CamundaLockExtendIntervalMinutes { get; set; } = 5;

    public bool EnableCamundaPullWorker { get; set; } = true;

    public bool UsePersistentQuartzStore { get; set; } = true;
}
