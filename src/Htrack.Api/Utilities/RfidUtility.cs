using HTrack.Api.Exceptions;

namespace HTrack.Api.Utilities;

public static class RfidUtility
{
    public static string Normalize(string? value)
    {
        var normalized = value?.Trim().Replace(" ", string.Empty).ToUpperInvariant();

        if (string.IsNullOrWhiteSpace(normalized))
            throw new InvalidEmployeeRfidException();

        return normalized;
    }
}
