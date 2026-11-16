using Mes.Common;
using Mes.DAL;
using Mes.Models;
using SqlSugar;

namespace Mes.BLL;

public class WorkOrderService
{
    private static readonly object DemoSync = new();
    private static long _demoId = 2;
    private static readonly List<WorkOrder> DemoOrders =
    [
        new()
        {
            Id = 1,
            OrderNo = "WO20261110001",
            ProductCode = "P-1001",
            ProductName = "减速机壳体",
            PlanQty = 100,
            Status = WorkOrderStatus.Released,
            PlanStart = DateTime.Today,
            PlanEnd = DateTime.Today.AddDays(5),
            CreateTime = DateTime.Now
        },
        new()
        {
            Id = 2,
            OrderNo = "WO20261110002",
            ProductCode = "P-1002",
            ProductName = "轴承座",
            PlanQty = 40,
            Status = WorkOrderStatus.Draft,
            CreateTime = DateTime.Now
        }
    ];

    private readonly IRepository<WorkOrder> _repo = new Repository<WorkOrder>();

    public async Task<ApiResult<PageResult<WorkOrder>>> QueryAsync(WorkOrderQuery query)
    {
        try
        {
            var pageIndex = query.PageIndex < 1 ? 1 : query.PageIndex;
            var pageSize = query.PageSize < 1 ? 20 : query.PageSize;
            var orderNo = query.OrderNo?.Trim();

            if (AppHost.DemoMode)
            {
                List<WorkOrder> page;
                int total;
                lock (DemoSync)
                {
                    IEnumerable<WorkOrder> rows = DemoOrders.Where(w => !w.IsDeleted);
                    if (!string.IsNullOrEmpty(orderNo))
                        rows = rows.Where(w => w.OrderNo.Contains(orderNo, StringComparison.OrdinalIgnoreCase));
                    if (query.Status.HasValue)
                        rows = rows.Where(w => w.Status == query.Status.Value);
                    var list = rows.OrderByDescending(w => w.Id).ToList();
                    total = list.Count;
                    page = list.Skip((pageIndex - 1) * pageSize).Take(pageSize).Select(Clone).ToList();
                }

                return ApiResult<PageResult<WorkOrder>>.Ok(new PageResult<WorkOrder>(page, total));
            }

            RefAsync<int> totalRows = 0;
            var items = await _repo.Db.Queryable<WorkOrder>()
                .Where(w => w.IsDeleted == false)
                .WhereIF(!string.IsNullOrEmpty(orderNo), w => w.OrderNo.Contains(orderNo!))
                .WhereIF(query.Status.HasValue, w => w.Status == query.Status!.Value)
                .OrderByDescending(w => w.Id)
                .ToPageListAsync(pageIndex, pageSize, totalRows);
            return ApiResult<PageResult<WorkOrder>>.Ok(new PageResult<WorkOrder>(items, totalRows));
        }
        catch (Exception ex)
        {
            ErrorLog.Write(ex, "查询工单");
            return ApiResult<PageResult<WorkOrder>>.Fail("查询工单失败，详细信息已写入日志。");
        }
    }

    public async Task<ApiResult<WorkOrder>> GetAsync(long id)
    {
        try
        {
            if (AppHost.DemoMode)
            {
                lock (DemoSync)
                {
                    var found = DemoOrders.FirstOrDefault(w => w.Id == id && !w.IsDeleted);
                    return found == null
                        ? ApiResult<WorkOrder>.Fail("工单不存在或已删除。")
                        : ApiResult<WorkOrder>.Ok(Clone(found));
                }
            }

            var entity = await _repo.GetByIdAsync(id);
            if (entity == null || entity.IsDeleted)
                return ApiResult<WorkOrder>.Fail("工单不存在或已删除。");
            return ApiResult<WorkOrder>.Ok(entity);
        }
        catch (Exception ex)
        {
            ErrorLog.Write(ex, "读取工单");
            return ApiResult<WorkOrder>.Fail("读取工单失败，详细信息已写入日志。");
        }
    }

    public async Task<ApiResult> SaveAsync(WorkOrder model)
    {
        try
        {
            var error = Validate(model);
            if (error != null)
                return ApiResult.Fail(error);

            model.OrderNo = model.OrderNo.Trim();
            model.ProductCode = model.ProductCode.Trim();
            model.ProductName = model.ProductName.Trim();
            model.Remark = string.IsNullOrWhiteSpace(model.Remark) ? null : model.Remark.Trim();

            if (AppHost.DemoMode)
                return SaveDemo(model);

            var duplicated = await _repo.Db.Queryable<WorkOrder>()
                .Where(w => w.IsDeleted == false && w.OrderNo == model.OrderNo && w.Id != model.Id)
                .AnyAsync();
            if (duplicated)
                return ApiResult.Fail("工单号已存在。");

            if (model.Id == 0)
            {
                model.CreateTime = DateTime.Now;
                model.CreateUserId = AppSession.UserId == 0 ? null : AppSession.UserId;
                model.IsDeleted = false;
                await _repo.InsertAsync(model);
            }
            else
            {
                var existing = await _repo.GetByIdAsync(model.Id);
                if (existing == null || existing.IsDeleted)
                    return ApiResult.Fail("工单不存在或已删除。");

                model.CreateTime = existing.CreateTime;
                model.CreateUserId = existing.CreateUserId;
                model.UpdateUserId = AppSession.UserId == 0 ? null : AppSession.UserId;
                model.IsDeleted = false;
                await _repo.UpdateAsync(model);
            }

            return ApiResult.Ok("保存成功。");
        }
        catch (Exception ex)
        {
            ErrorLog.Write(ex, "保存工单");
            return ApiResult.Fail("保存工单失败，详细信息已写入日志。");
        }
    }

    public async Task<ApiResult> DeleteAsync(long id)
    {
        try
        {
            if (id <= 0)
                return ApiResult.Fail("请选择要删除的工单。");

            if (AppHost.DemoMode)
            {
                lock (DemoSync)
                {
                    var found = DemoOrders.FirstOrDefault(w => w.Id == id && !w.IsDeleted);
                    if (found == null)
                        return ApiResult.Fail("工单不存在或已删除。");
                    found.IsDeleted = true;
                    found.UpdateTime = DateTime.Now;
                    found.UpdateUserId = AppSession.UserId == 0 ? null : AppSession.UserId;
                }

                return ApiResult.Ok("删除成功。");
            }

            var rows = await _repo.SoftDeleteAsync(id, AppSession.UserId == 0 ? null : AppSession.UserId);
            return rows > 0
                ? ApiResult.Ok("删除成功。")
                : ApiResult.Fail("工单不存在或已删除。");
        }
        catch (Exception ex)
        {
            ErrorLog.Write(ex, "删除工单");
            return ApiResult.Fail("删除工单失败，详细信息已写入日志。");
        }
    }

    private static ApiResult SaveDemo(WorkOrder model)
    {
        lock (DemoSync)
        {
            var duplicated = DemoOrders.Any(w =>
                !w.IsDeleted
                && w.Id != model.Id
                && string.Equals(w.OrderNo, model.OrderNo, StringComparison.OrdinalIgnoreCase));
            if (duplicated)
                return ApiResult.Fail("工单号已存在。");

            if (model.Id == 0)
            {
                model.Id = ++_demoId;
                model.CreateTime = DateTime.Now;
                model.CreateUserId = AppSession.UserId == 0 ? null : AppSession.UserId;
                model.IsDeleted = false;
                DemoOrders.Add(Clone(model));
            }
            else
            {
                var index = DemoOrders.FindIndex(w => w.Id == model.Id && !w.IsDeleted);
                if (index < 0)
                    return ApiResult.Fail("工单不存在或已删除。");
                model.CreateTime = DemoOrders[index].CreateTime;
                model.CreateUserId = DemoOrders[index].CreateUserId;
                model.UpdateTime = DateTime.Now;
                model.UpdateUserId = AppSession.UserId == 0 ? null : AppSession.UserId;
                model.IsDeleted = false;
                DemoOrders[index] = Clone(model);
            }
        }

        return ApiResult.Ok("保存成功。");
    }

    public static string? Validate(WorkOrder model)
    {
        if (string.IsNullOrWhiteSpace(model.OrderNo))
            return "工单号不能为空。";
        if (model.OrderNo.Trim().Length > 32)
            return "工单号不能超过 32 个字符。";
        if (string.IsNullOrWhiteSpace(model.ProductCode))
            return "产品编码不能为空。";
        if (model.ProductCode.Trim().Length > 64)
            return "产品编码不能超过 64 个字符。";
        if (string.IsNullOrWhiteSpace(model.ProductName))
            return "产品名称不能为空。";
        if (model.ProductName.Trim().Length > 128)
            return "产品名称不能超过 128 个字符。";
        if (model.PlanQty <= 0)
            return "计划数量必须大于 0。";
        if (model.Status is not (WorkOrderStatus.Draft or WorkOrderStatus.Released or WorkOrderStatus.Closed))
            return "请选择工单状态。";
        if (model.PlanStart.HasValue && model.PlanEnd.HasValue && model.PlanEnd.Value.Date < model.PlanStart.Value.Date)
            return "计划结束不能早于计划开始。";
        if (model.Remark?.Trim().Length > 200)
            return "备注不能超过 200 个字符。";
        return null;
    }

    private static WorkOrder Clone(WorkOrder source) => new()
    {
        Id = source.Id,
        OrderNo = source.OrderNo,
        ProductCode = source.ProductCode,
        ProductName = source.ProductName,
        PlanQty = source.PlanQty,
        Status = source.Status,
        PlanStart = source.PlanStart,
        PlanEnd = source.PlanEnd,
        Remark = source.Remark,
        CreateTime = source.CreateTime,
        CreateUserId = source.CreateUserId,
        UpdateTime = source.UpdateTime,
        UpdateUserId = source.UpdateUserId,
        IsDeleted = source.IsDeleted
    };
}
