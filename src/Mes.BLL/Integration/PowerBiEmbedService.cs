using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Mes.Common;

namespace Mes.BLL;

public sealed class PowerBiEmbedResult
{
    public bool Success { get; init; }

    public string Message { get; init; } = string.Empty;

    /// <summary>组织账户嵌入地址。WebView2 直接导航。</summary>
    public string? NavigateUrl { get; init; }

    /// <summary>应用拥有数据时，用 powerbi-client 嵌入的完整页面。</summary>
    public string? Html { get; init; }
}

/// <summary>
/// 生成 Power BI 嵌入地址或嵌入页。服务主体需要在 Power BI 管理门户中启用，并加入目标工作区。
/// </summary>
public class PowerBiEmbedService
{
    private static readonly HttpClient Http = new() { Timeout = TimeSpan.FromSeconds(30) };

    public async Task<PowerBiEmbedResult> CreateAsync(PowerBiOptions options)
    {
        try
        {
            if (!string.IsNullOrWhiteSpace(options.EmbedUrl))
            {
                return new PowerBiEmbedResult
                {
                    Success = true,
                    NavigateUrl = options.EmbedUrl.Trim(),
                    Message = "已使用配置中的嵌入地址。"
                };
            }

            if (string.IsNullOrWhiteSpace(options.WorkspaceId) || string.IsNullOrWhiteSpace(options.ReportId))
            {
                return new PowerBiEmbedResult
                {
                    Success = false,
                    Message = "请在 appsettings.json 的 PowerBI 节点填写 WorkspaceId 和 ReportId。"
                };
            }

            var hasSecret = !string.IsNullOrWhiteSpace(options.TenantId)
                            && !string.IsNullOrWhiteSpace(options.ClientId)
                            && !string.IsNullOrWhiteSpace(options.ClientSecret);
            if (!hasSecret)
            {
                var url =
                    "https://app.powerbi.com/reportEmbed?reportId="
                    + Uri.EscapeDataString(options.ReportId.Trim())
                    + "&groupId="
                    + Uri.EscapeDataString(options.WorkspaceId.Trim())
                    + "&autoAuth=true";
                return new PowerBiEmbedResult
                {
                    Success = true,
                    NavigateUrl = url,
                    Message = "未配置服务主体，将使用组织账户登录后查看报表。"
                };
            }

            var aadToken = await RequestAadTokenAsync(options);
            var report = await GetReportAsync(options, aadToken);
            var embedToken = await GenerateEmbedTokenAsync(options, aadToken);
            return new PowerBiEmbedResult
            {
                Success = true,
                Html = BuildEmbedHtml(report.EmbedUrl, report.ReportId, embedToken),
                Message = "已获取嵌入令牌。"
            };
        }
        catch (Exception ex)
        {
            ErrorLog.Write(ex, "生成 Power BI 嵌入页");
            return new PowerBiEmbedResult
            {
                Success = false,
                Message = "无法打开 Power BI 报表，详细信息已写入本地日志。"
            };
        }
    }

    private static async Task<string> RequestAadTokenAsync(PowerBiOptions options)
    {
        var body = new Dictionary<string, string>
        {
            ["client_id"] = options.ClientId.Trim(),
            ["client_secret"] = options.ClientSecret,
            ["scope"] = "https://analysis.windows.net/powerbi/api/.default",
            ["grant_type"] = "client_credentials"
        };
        using var response = await Http.PostAsync(
            $"https://login.microsoftonline.com/{options.TenantId.Trim()}/oauth2/v2.0/token",
            new FormUrlEncodedContent(body));
        var json = await response.Content.ReadAsStringAsync();
        if (!response.IsSuccessStatusCode)
            throw new InvalidOperationException("Azure AD 令牌请求失败：" + response.StatusCode);

        using var doc = JsonDocument.Parse(json);
        return doc.RootElement.GetProperty("access_token").GetString()
               ?? throw new InvalidOperationException("Azure AD 响应中没有 access_token。");
    }

    private static async Task<(string EmbedUrl, string ReportId)> GetReportAsync(PowerBiOptions options, string aadToken)
    {
        using var request = new HttpRequestMessage(
            HttpMethod.Get,
            $"https://api.powerbi.com/v1.0/myorg/groups/{options.WorkspaceId.Trim()}/reports/{options.ReportId.Trim()}");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", aadToken);
        using var response = await Http.SendAsync(request);
        var json = await response.Content.ReadAsStringAsync();
        if (!response.IsSuccessStatusCode)
            throw new InvalidOperationException("读取报表失败：" + response.StatusCode);

        using var doc = JsonDocument.Parse(json);
        var embedUrl = doc.RootElement.GetProperty("embedUrl").GetString();
        var reportId = doc.RootElement.GetProperty("id").GetString();
        if (string.IsNullOrWhiteSpace(embedUrl) || string.IsNullOrWhiteSpace(reportId))
            throw new InvalidOperationException("报表响应缺少 embedUrl。");
        return (embedUrl, reportId);
    }

    private static async Task<string> GenerateEmbedTokenAsync(PowerBiOptions options, string aadToken)
    {
        using var request = new HttpRequestMessage(
            HttpMethod.Post,
            $"https://api.powerbi.com/v1.0/myorg/groups/{options.WorkspaceId.Trim()}/reports/{options.ReportId.Trim()}/GenerateToken");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", aadToken);
        request.Content = new StringContent("{\"accessLevel\":\"View\"}", Encoding.UTF8, "application/json");
        using var response = await Http.SendAsync(request);
        var json = await response.Content.ReadAsStringAsync();
        if (!response.IsSuccessStatusCode)
            throw new InvalidOperationException("生成嵌入令牌失败：" + response.StatusCode);

        using var doc = JsonDocument.Parse(json);
        return doc.RootElement.GetProperty("token").GetString()
               ?? throw new InvalidOperationException("GenerateToken 响应中没有 token。");
    }

    private static string BuildEmbedHtml(string embedUrl, string reportId, string embedToken)
    {
        var url = JsonSerializer.Serialize(embedUrl);
        var id = JsonSerializer.Serialize(reportId);
        var token = JsonSerializer.Serialize(embedToken);
        return $$"""
            <!DOCTYPE html>
            <html>
            <head>
              <meta charset="utf-8" />
              <style>html,body,#report{height:100%;margin:0;border:0;}</style>
              <script src="https://cdn.jsdelivr.net/npm/powerbi-client@2.23.1/dist/powerbi.min.js"></script>
            </head>
            <body>
              <div id="report"></div>
              <script>
                const models = window['powerbi-client'].models;
                powerbi.embed(document.getElementById('report'), {
                  type: 'report',
                  tokenType: models.TokenType.Embed,
                  accessToken: {{token}},
                  embedUrl: {{url}},
                  id: {{id}},
                  settings: { panes: { filters: { visible: false }, pageNavigation: { visible: true } } }
                });
              </script>
            </body>
            </html>
            """;
    }
}
