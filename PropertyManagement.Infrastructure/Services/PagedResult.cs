namespace PropertyManagement.Infrastructure.Services;

public record PagedResult<T>(List<T> Items, int TotalCount, int Page, int PageSize);
