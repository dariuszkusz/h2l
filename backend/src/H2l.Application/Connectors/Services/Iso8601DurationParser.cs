using System.Xml;

namespace H2l.Application.Connectors.Services;

public static class Iso8601DurationParser
{
    public static bool TryParse(string? isoDuration, out TimeSpan duration)
    {
        duration = default;
        if (string.IsNullOrWhiteSpace(isoDuration))
        {
            return false;
        }

        try
        {
            var parsed = XmlConvert.ToTimeSpan(isoDuration.Trim());
            if (parsed <= TimeSpan.Zero)
            {
                return false;
            }

            duration = parsed;
            return true;
        }
        catch (FormatException)
        {
            return false;
        }
        catch (OverflowException)
        {
            return false;
        }
    }
}
