namespace Mes.Models;

public class LoginResult
{
    public long UserId { get; set; }

    public string UserName { get; set; } = string.Empty;

    public string RealName { get; set; } = string.Empty;

    public List<string> Permissions { get; set; } = [];

    public List<MenuNodeDto> Menus { get; set; } = [];
}
