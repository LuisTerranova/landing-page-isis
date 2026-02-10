namespace landing_page_isis.core;

public record PaginatedResponse<T>(IEnumerable<T> Items, int TotalItems, int CurrentPage, int PageSize)
{
  public int TotalPages => (int)Math.Ceiling((double)TotalItems / PageSize);
}
