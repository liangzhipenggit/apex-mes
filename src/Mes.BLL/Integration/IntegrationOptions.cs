namespace Mes.BLL;

public sealed class PowerBiOptions
{
    public string WorkspaceId { get; init; } = string.Empty;

    public string ReportId { get; init; } = string.Empty;

    public string TenantId { get; init; } = string.Empty;

    public string ClientId { get; init; } = string.Empty;

    public string ClientSecret { get; init; } = string.Empty;

    /// <summary>已有完整嵌入地址时直接打开，不再申请令牌。</summary>
    public string EmbedUrl { get; init; } = string.Empty;
}

public sealed class LabelOptions
{
    public string FastReportTemplate { get; init; } = string.Empty;

    public string BarTenderTemplate { get; init; } = string.Empty;

    public string BarTenderExe { get; init; } = string.Empty;

    public string PrinterName { get; init; } = string.Empty;
}
