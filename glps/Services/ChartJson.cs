using Newtonsoft.Json;

namespace glps.Services
{
    /// <summary>
    /// Serialises chart data for embedding in a &lt;script&gt; block via Html.Raw.
    /// HTML-sensitive characters are escaped so database values cannot break out of the script.
    /// </summary>
    public static class ChartJson
    {
        private static readonly JsonSerializerSettings Settings = new JsonSerializerSettings
        {
            StringEscapeHandling = StringEscapeHandling.EscapeHtml
        };

        public static string Serialize(object value)
        {
            return JsonConvert.SerializeObject(value, Settings);
        }
    }
}
