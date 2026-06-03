namespace NewGest.Application.DTOs.Shared;

public record PagedResult<T>(IReadOnlyList<T> Items, int Total, int Page, int PageSize);
