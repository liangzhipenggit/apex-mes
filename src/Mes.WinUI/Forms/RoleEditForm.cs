using Mes.BLL;
using Mes.Models;

namespace Mes.WinUI;

public class RoleEditForm : AntdUI.Window
{
    private readonly RoleSaveModel _model;
    private readonly AntdUI.Input _roleName = new();
    private readonly AntdUI.Input _roleCode = new();
    private readonly AntdUI.Switch _enabled = new() { Checked = true };
    private readonly TreeView _perms = new()
    {
        CheckBoxes = true,
        BorderStyle = BorderStyle.FixedSingle,
        HideSelection = false,
        Font = new Font("Microsoft YaHei UI", 9.5F),
        ShowLines = true,
        FullRowSelect = true
    };
    private readonly AntdUI.Button _save = new()
    {
        Text = "保存",
        Type = AntdUI.TTypeMini.Primary,
        IconSvg = MenuIcons.Action.Save
    };
    private readonly AntdUI.Button _expandAll = new() { Text = "全部展开", Type = AntdUI.TTypeMini.Default };
    private readonly AntdUI.Button _collapseAll = new() { Text = "全部折叠", Type = AntdUI.TTypeMini.Default };
    private readonly AntdUI.Button _checkMenus = new() { Text = "仅勾选菜单", Type = AntdUI.TTypeMini.Default };
    private readonly AntdUI.Button _clearPerms = new() { Text = "清空权限", Type = AntdUI.TTypeMini.Default };
    private bool _saving;
    private bool _syncing;

    public RoleEditForm(RoleSaveModel model)
    {
        _model = model;
        Text = model.Id == 0 ? "新增角色 · 配置权限" : "修改角色 · 配置权限";
        StartPosition = FormStartPosition.CenterParent;
        ClientSize = new Size(720, 720);
        MinimumSize = new Size(680, 640);
        Resizable = true;
        MaximizeBox = true;
        MinimizeBox = false;
        BackColor = UiTheme.PageBack;

        var header = UiChrome.CreateDialogHeader(Text, "目录 / 菜单 / 按钮 分级授权", maximizable: true);

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

        var top = new Panel
        {
            Dock = DockStyle.Top,
            Height = 186,
            BackColor = UiTheme.CardBack,
            Padding = new Padding(UiTheme.SpaceXl, UiTheme.SpaceLg, UiTheme.SpaceXl, UiTheme.SpaceSm)
        };
        var y = 8;
        FormUi.AddField(top, "角色名", _roleName, ref y, 480);
        FormUi.AddField(top, "编码", _roleCode, ref y, 480);
        top.Controls.Add(FormUi.Caption("启用", 0, y));
        _enabled.SetBounds(FormUi.EditorLeft, y + 6, 64, 28);
        top.Controls.Add(_enabled);

        var tool = new Panel
        {
            Dock = DockStyle.Top,
            Height = 100,
            BackColor = UiTheme.CardBack,
            Padding = new Padding(UiTheme.SpaceXl, 0, UiTheme.SpaceXl, UiTheme.SpaceSm)
        };
        tool.Controls.Add(FormUi.Section("分配权限（菜单 + 按钮）", 0, 4));
        tool.Controls.Add(FormUi.Hint("可勾选目录、菜单与按钮。勾选按钮会自动勾选上级菜单；勾选上级会同步子项。", 0, 36, 640));
        _expandAll.SetBounds(0, 64, 104, 32);
        _collapseAll.SetBounds(112, 64, 104, 32);
        _checkMenus.SetBounds(224, 64, 116, 32);
        _clearPerms.SetBounds(348, 64, 104, 32);
        _expandAll.Click += (_, _) => _perms.ExpandAll();
        _collapseAll.Click += (_, _) => _perms.CollapseAll();
        _checkMenus.Click += (_, _) => CheckOnlyMenus();
        _clearPerms.Click += (_, _) => ClearAllChecks();
        tool.Controls.AddRange(_expandAll, _collapseAll, _checkMenus, _clearPerms);

        var treeHost = new Panel
        {
            Dock = DockStyle.Fill,
            BackColor = UiTheme.CardBack,
            Padding = new Padding(24, 0, 24, 8)
        };
        _perms.Dock = DockStyle.Fill;
        _perms.AfterCheck += OnAfterCheck;
        treeHost.Controls.Add(_perms);

        Controls.Add(treeHost);
        Controls.Add(tool);
        Controls.Add(top);
        Controls.Add(footer);
        Controls.Add(header);
        Load += async (_, _) => await BindAsync();
    }

    private async Task BindAsync()
    {
        _roleName.Text = _model.RoleName;
        _roleCode.Text = _model.RoleCode;
        _enabled.Checked = _model.Enabled;

        var result = await AppHost.Menus.QueryAsync(new MenuQuery());
        if (!result.Success || result.Data == null)
        {
            UiNotify.Warn(this, result.Message);
            return;
        }

        _syncing = true;
        _perms.BeginUpdate();
        _perms.Nodes.Clear();
        var lookup = result.Data.ToLookup(p => p.ParentId);

        void AddNodes(TreeNodeCollection nodes, long parentId)
        {
            foreach (var item in lookup[parentId].OrderBy(p => p.Sort).ThenBy(p => p.Id))
            {
                var node = new TreeNode(FormatNode(item))
                {
                    Tag = new PermNode(item),
                    ForeColor = item.PermType switch
                    {
                        PermissionType.Directory => UiTheme.Primary,
                        PermissionType.Menu => UiTheme.TextPrimary,
                        _ => UiTheme.MutedText
                    }
                };
                node.Checked = _model.PermissionIds.Contains(item.Id);
                nodes.Add(node);
                AddNodes(node.Nodes, item.Id);
            }
        }

        AddNodes(_perms.Nodes, 0);
        _perms.ExpandAll();
        _perms.EndUpdate();
        _syncing = false;
    }

    private static string FormatNode(SysPermission item)
    {
        var type = item.PermType switch
        {
            PermissionType.Directory => "目录",
            PermissionType.Menu => "菜单",
            _ => "按钮"
        };
        var form = item.PermType == PermissionType.Menu && !string.IsNullOrWhiteSpace(item.FormKey)
            ? $"  · {item.FormKey}"
            : string.Empty;
        return $"[{type}]  {item.PermName}{form}    ({item.PermCode})";
    }

    private void OnAfterCheck(object? sender, TreeViewEventArgs e)
    {
        if (_syncing || e.Node == null)
            return;

        _syncing = true;
        try
        {
            SetChildren(e.Node, e.Node.Checked);
            if (e.Node.Checked)
                CheckAncestors(e.Node);
        }
        finally
        {
            _syncing = false;
        }
    }

    private static void SetChildren(TreeNode node, bool check)
    {
        foreach (TreeNode child in node.Nodes)
        {
            child.Checked = check;
            SetChildren(child, check);
        }
    }

    private static void CheckAncestors(TreeNode node)
    {
        var parent = node.Parent;
        while (parent != null)
        {
            parent.Checked = true;
            parent = parent.Parent;
        }
    }

    private void CheckOnlyMenus()
    {
        _syncing = true;
        try
        {
            void Walk(TreeNodeCollection nodes)
            {
                foreach (TreeNode node in nodes)
                {
                    if (node.Tag is PermNode p)
                        node.Checked = p.PermType is PermissionType.Directory or PermissionType.Menu;
                    Walk(node.Nodes);
                }
            }

            Walk(_perms.Nodes);
        }
        finally
        {
            _syncing = false;
        }
    }

    private void ClearAllChecks()
    {
        _syncing = true;
        try
        {
            void Walk(TreeNodeCollection nodes)
            {
                foreach (TreeNode node in nodes)
                {
                    node.Checked = false;
                    Walk(node.Nodes);
                }
            }

            Walk(_perms.Nodes);
        }
        finally
        {
            _syncing = false;
        }
    }

    private async Task SaveAsync()
    {
        if (_saving)
            return;

        _model.RoleName = (_roleName.Text ?? string.Empty).Trim();
        _model.RoleCode = (_roleCode.Text ?? string.Empty).Trim();
        _model.Enabled = _enabled.Checked;
        _model.PermissionIds = CollectCheckedWithAncestors().ToList();

        if (_model.RoleName.Length == 0 || _model.RoleCode.Length == 0)
        {
            UiNotify.Warn(this, "角色名称和编码不能为空。");
            return;
        }

        _saving = true;
        _save.Loading = true;
        _save.Enabled = false;
        try
        {
            var result = await AppHost.Roles.SaveAsync(_model);
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

    private HashSet<long> CollectCheckedWithAncestors()
    {
        var ids = new HashSet<long>();

        void Walk(TreeNodeCollection nodes)
        {
            foreach (TreeNode node in nodes)
            {
                if (node.Checked && node.Tag is PermNode p)
                {
                    ids.Add(p.Id);
                    var parent = node.Parent;
                    while (parent != null)
                    {
                        if (parent.Tag is PermNode pp)
                            ids.Add(pp.Id);
                        parent = parent.Parent;
                    }
                }

                Walk(node.Nodes);
            }
        }

        Walk(_perms.Nodes);
        return ids;
    }

    private sealed class PermNode(SysPermission permission)
    {
        public long Id { get; } = permission.Id;

        public PermissionType PermType { get; } = permission.PermType;
    }
}
