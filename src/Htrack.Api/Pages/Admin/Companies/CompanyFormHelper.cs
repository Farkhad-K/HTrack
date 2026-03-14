namespace HTrack.Api.Pages.Admin.Companies;

internal static class CompanyFormHelper
{
    internal static List<long> ParseManagerIds(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw)) return [];
        return raw.Split(',', StringSplitOptions.RemoveEmptyEntries)
                  .Select(s => s.Trim())
                  .Where(s => long.TryParse(s, out _))
                  .Select(long.Parse)
                  .ToList();
    }
}
