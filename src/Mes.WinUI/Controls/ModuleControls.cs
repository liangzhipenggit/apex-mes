using Mes.Common;

namespace Mes.WinUI;

public class UcHome : SecureUserControl
{
    public UcHome()
    {
        Padding = new Padding(UiTheme.SpaceLg);

        var card = new AntdUI.Panel
        {
            Dock = DockStyle.Fill,
            Shadow = UiTheme.CardShadow,
            Radius = UiTheme.CardRadius,
            BorderWidth = 0,
            Back = UiTheme.CardBack,
            Padding = new Padding(32, 28, 32, 28)
        };

        var title = new Label
        {
            Text = $"你好，{AppSession.RealName}",
            Font = UiTheme.UiFont(22F, FontStyle.Bold),
            ForeColor = UiTheme.TextPrimary,
            AutoSize = false,
            Dock = DockStyle.Top,
            Height = 44,
            TextAlign = ContentAlignment.MiddleLeft,
            BackColor = Color.Transparent
        };

        var tip = new AntdUI.Alert
        {
            Dock = DockStyle.Top,
            Height = 64,
            Margin = new Padding(0, UiTheme.SpaceMd, 0, UiTheme.SpaceLg),
            Icon = AntdUI.TType.Success,
            CloseIcon = false,
            TextTitle = "系统就绪",
            Text = "从左侧菜单打开业务模块。扫码、工单、标签与看板均可在此接入。"
        };

        var perms = new Label
        {
            Text = "当前权限：" + (AppSession.Permissions.Count == 0
                ? "（无）"
                : string.Join("，", AppSession.Permissions)),
            AutoSize = false,
            Dock = DockStyle.Top,
            Height = 72,
            ForeColor = UiTheme.MutedText,
            Font = UiTheme.UiFont(),
            TextAlign = ContentAlignment.TopLeft,
            BackColor = Color.Transparent
        };

        card.Controls.Add(perms);
        card.Controls.Add(tip);
        card.Controls.Add(title);
        Controls.Add(card);
    }
}

public class UcModule : SecureUserControl
{
    public UcModule(string title, params (string Text, string Permission)[] actions)
    {
        BackColor = Color.Transparent;
        Padding = new Padding(UiTheme.SpaceLg);

        var card = new AntdUI.Panel
        {
            Dock = DockStyle.Fill,
            Shadow = UiTheme.CardShadow,
            Radius = UiTheme.CardRadius,
            BorderWidth = 0,
            Back = UiTheme.CardBack,
            Padding = new Padding(UiTheme.SpaceXl)
        };

        var header = new Label
        {
            Text = title,
            Font = UiTheme.UiFont(UiTheme.FontSizeTitle, FontStyle.Bold),
            ForeColor = UiTheme.TextPrimary,
            AutoSize = false,
            Dock = DockStyle.Top,
            Height = 44,
            TextAlign = ContentAlignment.MiddleLeft,
            BackColor = Color.Transparent
        };

        var bar = new AntdUI.FlowPanel
        {
            Dock = DockStyle.Top,
            Height = 56,
            Align = AntdUI.TAlignFlow.LeftCenter
        };

        foreach (var action in actions)
        {
            var permission = action.Permission;
            var button = new PermissionButton
            {
                Text = action.Text,
                Type = AntdUI.TTypeMini.Primary,
                AutoSizeMode = AntdUI.TAutoSize.Width,
                Margin = new Padding(0, 0, UiTheme.SpaceSm, 0),
                PermissionCode = permission,
                Height = UiTheme.ControlHeight
            };
            button.Click += (_, _) =>
            {
                if (!AppSession.HasPermission(permission))
                {
                    UiNotify.Warn(FindForm(), "没有执行该操作的权限。");
                    return;
                }

                UiNotify.Info(FindForm(), $"已触发「{action.Text}」（{permission}）。业务页面后续在此接入。");
            };
            bar.Controls.Add(button);
        }

        var body = new AntdUI.Alert
        {
            Text = "模块骨架已打开。列表、查询和保存接到对应 Service 即可。",
            TextTitle = title,
            Icon = AntdUI.TType.Info,
            CloseIcon = false,
            Dock = DockStyle.Top,
            Height = 80,
            Margin = new Padding(0, UiTheme.SpaceSm, 0, 0)
        };

        card.Controls.Add(body);
        card.Controls.Add(bar);
        card.Controls.Add(header);
        Controls.Add(card);
    }
}
