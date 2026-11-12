using Mes.BLL;
using Mes.Models;

namespace Mes.WinUI;

public class UserEditForm : AntdUI.Window
{
    private readonly UserSaveModel _model;
    private readonly AntdUI.Input _userName = new();
    private readonly AntdUI.Input _realName = new();
    private readonly AntdUI.Input _password = new()
    {
        UseSystemPasswordChar = true,
        PlaceholderText = "留空则不修改"
    };
    private readonly AntdUI.Switch _enabled = new() { Checked = true };
    private readonly CheckedListBox _roles = new()
    {
        CheckOnClick = true,
        BorderStyle = BorderStyle.FixedSingle,
        IntegralHeight = false,
        Font = new Font("Microsoft YaHei UI", 9.5F)
    };
    private readonly AntdUI.Button _save = new()
    {
        Text = "保存",
        Type = AntdUI.TTypeMini.Primary,
        IconSvg = MenuIcons.Action.Save
    };
    private bool _saving;

    public UserEditForm(UserSaveModel model)
    {
        _model = model;
        Text = model.Id == 0 ? "新增用户" : "修改用户 · 分配角色";
        StartPosition = FormStartPosition.CenterParent;
        ClientSize = new Size(560, 620);
        MinimumSize = new Size(560, 620);
        Resizable = false;
        MaximizeBox = false;
        MinimizeBox = false;
        BackColor = UiTheme.PageBack;

        var header = UiChrome.CreateDialogHeader(Text, "基本信息 + 角色分配");

        var footer = new Panel
        {
            Dock = DockStyle.Bottom,
            Height = 72,
            BackColor = UiTheme.CardBack,
            Padding = new Padding(UiTheme.SpaceXl, UiTheme.SpaceMd, UiTheme.SpaceXl, UiTheme.SpaceMd)
        };
        _save.Size = new Size(120, UiTheme.ControlHeight);
        PageToolbar.StyleIcon(_save);
        var cancel = new AntdUI.Button { Text = "取消", Type = AntdUI.TTypeMini.Default, Size = new Size(96, UiTheme.ControlHeight) };
        cancel.Click += (_, _) => { DialogResult = DialogResult.Cancel; Close(); };
        _save.Click += async (_, _) => await SaveAsync();
        footer.Controls.Add(_save);
        footer.Controls.Add(cancel);
        footer.Resize += (_, _) =>
        {
            cancel.Location = new Point(footer.ClientSize.Width - cancel.Width, 12);
            _save.Location = new Point(cancel.Left - _save.Width - 12, 12);
        };

        var body = new Panel
        {
            Dock = DockStyle.Fill,
            BackColor = UiTheme.CardBack,
            Padding = new Padding(UiTheme.SpaceXl, UiTheme.SpaceLg, UiTheme.SpaceXl, UiTheme.SpaceMd),
            AutoScroll = true
        };

        var y = 8;
        FormUi.AddField(body, "用户名", _userName, ref y, 400);
        FormUi.AddField(body, "姓名", _realName, ref y, 400);
        FormUi.AddField(body, model.Id == 0 ? "密码" : "新密码", _password, ref y, 400);

        body.Controls.Add(FormUi.Caption("启用", 0, y));
        _enabled.SetBounds(FormUi.EditorLeft, y + 4, 64, 28);
        body.Controls.Add(_enabled);
        y += FormUi.RowHeight;

        body.Controls.Add(FormUi.Section("分配角色", 0, y));
        y += 32;
        body.Controls.Add(FormUi.Hint("勾选后，用户将继承对应角色下的菜单与按钮权限。", 0, y, 500));
        y += 28;
        _roles.SetBounds(0, y, 500, 220);
        body.Controls.Add(_roles);

        Controls.Add(body);
        Controls.Add(footer);
        Controls.Add(header);
        Load += async (_, _) => await BindAsync();
    }

    private async Task BindAsync()
    {
        _userName.Text = _model.UserName;
        _realName.Text = _model.RealName;
        _enabled.Checked = _model.Enabled;
        _password.Text = string.Empty;
        if (_model.Id == 0)
            _password.PlaceholderText = "必填";

        var roles = await AppHost.Roles.ListEnabledAsync();
        _roles.BeginUpdate();
        _roles.Items.Clear();
        if (!roles.Success || roles.Data == null)
        {
            _roles.EndUpdate();
            UiNotify.Warn(this, roles.Message);
            return;
        }

        foreach (var role in roles.Data)
        {
            var item = new RolePickItem(role);
            var idx = _roles.Items.Add(item);
            if (_model.RoleIds.Contains(role.Id))
                _roles.SetItemChecked(idx, true);
        }

        _roles.EndUpdate();
        if (_model.Id == 0)
            _userName.Focus();
    }

    private async Task SaveAsync()
    {
        if (_saving)
            return;

        var userName = (_userName.Text ?? string.Empty).Trim();
        var realName = (_realName.Text ?? string.Empty).Trim();
        var password = _password.Text ?? string.Empty;

        if (userName.Length == 0 || realName.Length == 0)
        {
            UiNotify.Warn(this, "用户名和姓名不能为空。");
            return;
        }

        if (_model.Id == 0 && password.Trim().Length == 0)
        {
            UiNotify.Warn(this, "新建用户必须设置密码。");
            _password.Focus();
            return;
        }

        _model.UserName = userName;
        _model.RealName = realName;
        _model.Password = password;
        _model.Enabled = _enabled.Checked;
        _model.RoleIds = _roles.CheckedItems.OfType<RolePickItem>().Select(x => x.Id).ToList();

        _saving = true;
        _save.Loading = true;
        _save.Enabled = false;
        try
        {
            var result = await AppHost.Users.SaveAsync(_model);
            if (!result.Success)
            {
                UiNotify.Warn(this, result.Message);
                return;
            }

            DialogResult = DialogResult.OK;
            Close();
        }
        finally
        {
            _saving = false;
            if (!IsDisposed)
            {
                _save.Loading = false;
                _save.Enabled = true;
            }
        }
    }

    private sealed class RolePickItem(SysRole role)
    {
        public long Id { get; } = role.Id;

        public override string ToString()
            => $"{role.RoleName}    ({role.RoleCode})";
    }
}
