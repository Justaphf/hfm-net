using System.ComponentModel;

namespace HFM.Core;
public static class EnumExtensions
{
    private static readonly object _cacheSyncObject = new();

    private static readonly Dictionary<Enum, string> _descriptionCache = new();

    public static string Description(this Enum value)
    {
        if (value == null) throw new ArgumentNullException(nameof(value));

        if (!_descriptionCache.ContainsKey(value))
        {
            lock (_cacheSyncObject)
            {
                if (!_descriptionCache.ContainsKey(value))
                {
                    string valueAsString = value.ToString();
                    var fi = value.GetType().GetField(valueAsString);
                    if (fi != null)
                    {
                        var da = (DescriptionAttribute[])fi.GetCustomAttributes(typeof(DescriptionAttribute), false);
                        valueAsString = da.Length > 0 ? da[0].Description : valueAsString;
                    }

                    _descriptionCache.Add(value, valueAsString);
                }
            }
        }
        return _descriptionCache[value];
    }
}
