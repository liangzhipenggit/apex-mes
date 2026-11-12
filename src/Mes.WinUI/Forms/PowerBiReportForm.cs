using Mes.BLL;
using Microsoft.Web.WebView2.Core;
using Microsoft.Web.WebView2.WinForms;

namespace Mes.WinUI;

public class PowerBiReportForm : Form
{
    private readonly WebView2 _web = new() { Dock = DockStyle.Fill };
    private readonly AntdUI.Alert _status = new()
    {
        Dock = DockStyle.Top,
        Height = 48,
        Icon = AntdUI.TType.Info,
        CloseIcon = false,
        TextTitle = string.Empty,
        Text = "正在准备 Power BI 报表…",
        Margin = new Padding(0, 0, 0, UiTheme.SpaceSm)
    };
    private readonly PowerBiEmbedService _embed = new();

    public PowerBiReportForm()
    {
        Text = "生产看板";
        BackColor = UiTheme.PageBack;
        Controls.Add(_web);
        Controls.Add(_status);
    }

    protected override async void OnLoad(EventArgs e)
    {
        base.OnLoad(e);
        await OpenAsync();
    }

    private async Task OpenAsync()
    {
        _status.Text = "正在准备 Power BI 报表…";
        try
        {
            var result = await _embed.CreateAsync(AppHost.PowerBi);
            _status.Text = result.Message;
            _status.Icon = result.Success ? AntdUI.TType.Success : AntdUI.TType.Warn;
            if (!result.Success)
            {
                UiNotify.Warn(FindForm(), result.Message);
                return;
            }

            var profile = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "IndustrialMES",
                "WebView2");
            var env = await CoreWebView2Environment.CreateAsync(null, profile);
            await _web.EnsureCoreWebView2Async(env);

            if (!string.IsNullOrWhiteSpace(result.Html))
                _web.NavigateToString(result.Html);
            else if (!string.IsNullOrWhiteSpace(result.NavigateUrl))
                _web.Source = new Uri(result.NavigateUrl);
        }
        catch (WebView2RuntimeNotFoundException ex)
        {
            ErrorLogWrite(ex);
            _status.Icon = AntdUI.TType.Error;
            _status.Text = "本机未安装 WebView2 运行时。";
            UiNotify.Warn(FindForm(), "本机未安装 WebView2 运行时。请先安装 Microsoft Edge WebView2 Runtime。");
        }
        catch (Exception ex)
        {
            ErrorLogWrite(ex);
            _status.Icon = AntdUI.TType.Error;
            _status.Text = "打开报表失败。";
            UiNotify.Error(FindForm(), "打开报表失败，详细信息已写入本地日志。");
        }
    }

    private static void ErrorLogWrite(Exception ex) => Mes.Common.ErrorLog.Write(ex, "WebView2 加载 Power BI");
}
