namespace Mes.Models;

public class MenuNodeDto
{
    public long Id { get; set; }

    public string Title { get; set; } = string.Empty;

    public string? FormKey { get; set; }

    public string? PermissionCode { get; set; }

    public List<MenuNodeDto> Children { get; set; } = [];
}
