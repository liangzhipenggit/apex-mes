using Mes.Models;

namespace Mes.BLL;

public static class MenuTreeBuilder
{
    /// <summary>
    /// 用已经授权的目录和菜单（不含按钮）生成树。空目录会被丢掉。
    /// </summary>
    public static List<MenuNodeDto> Build(IEnumerable<SysPermission> permissions)
    {
        var list = permissions
            .Where(p => p.PermType != PermissionType.Button)
            .GroupBy(p => p.Id)
            .Select(g => g.First())
            .OrderBy(p => p.Sort)
            .ThenBy(p => p.Id)
            .ToList();
        var lookup = list.ToLookup(p => p.ParentId);

        List<MenuNodeDto> BuildLevel(long parentId)
        {
            var nodes = new List<MenuNodeDto>();
            foreach (var item in lookup[parentId])
            {
                var children = BuildLevel(item.Id);
                var isMenu = item.PermType == PermissionType.Menu;
                if (isMenu && string.IsNullOrWhiteSpace(item.FormKey))
                    continue;
                if (!isMenu && children.Count == 0)
                    continue;

                nodes.Add(new MenuNodeDto
                {
                    Id = item.Id,
                    Title = item.PermName,
                    FormKey = item.FormKey,
                    PermissionCode = item.PermCode,
                    Children = children
                });
            }

            return nodes;
        }

        return BuildLevel(0);
    }
}
