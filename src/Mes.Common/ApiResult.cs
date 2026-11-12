namespace Mes.Common;

public class ApiResult
{
    public bool Success { get; set; }

    public string Message { get; set; } = string.Empty;

    public static ApiResult Ok(string message = "成功") => new() { Success = true, Message = message };

    public static ApiResult Fail(string message) => new() { Success = false, Message = message };
}

public class ApiResult<T> : ApiResult
{
    public T? Data { get; set; }

    public static ApiResult<T> Ok(T data, string message = "成功")
        => new() { Success = true, Message = message, Data = data };

    public static new ApiResult<T> Fail(string message)
        => new() { Success = false, Message = message };
}

public sealed class PageResult<T>
{
    public PageResult(List<T> items, int total)
    {
        Items = items;
        Total = total;
    }

    public List<T> Items { get; }

    public int Total { get; }
}
