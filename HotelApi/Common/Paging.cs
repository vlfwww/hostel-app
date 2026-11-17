namespace HotelApi.Common;

public static class Paging
{
    public static (int Page, int PageSize) Normalize(int? page, int? pageSize, int defaultPageSize, int maxPageSize)
    {
        var normalizedPage = page.GetValueOrDefault(1);
        if (normalizedPage < 1)
        {
            normalizedPage = 1;
        }

        var normalizedSize = pageSize.GetValueOrDefault(defaultPageSize);
        if (normalizedSize < 1)
        {
            normalizedSize = defaultPageSize;
        }

        if (normalizedSize > maxPageSize)
        {
            normalizedSize = maxPageSize;
        }

        return (normalizedPage, normalizedSize);
    }
}
