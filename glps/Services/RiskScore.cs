using System;
using System.Globalization;
using glps.Models;

namespace glps.Services
{
    /// <summary>
    /// Converts the free-text risk columns on <see cref="bchn_data"/> into a 0-100 score.
    /// Accepts percentages ("35", "35%"), yes/no style flags ("Yes", "Y", "True", "1"),
    /// low/medium/high levels, or the category name itself (e.g. "Terrorism").
    /// </summary>
    public static class RiskScore
    {
        public const double HighRiskThreshold = 50;

        public static readonly string[] Categories =
        {
            "Terrorism", "Narcotics", "Smuggling", "Illegal Immigration", "Revenue"
        };

        public static double Parse(string value, string category = null)
        {
            if (string.IsNullOrWhiteSpace(value)) return 0;

            var text = value.Trim();
            var numeric = text.EndsWith("%") ? text.TrimEnd('%').Trim() : text;
            double number;
            if (double.TryParse(numeric, NumberStyles.Float, CultureInfo.InvariantCulture, out number))
            {
                // A bare "1" is treated as a yes flag, anything else as a percentage.
                if (number == 1 && !text.EndsWith("%")) return 100;
                return Math.Max(0, Math.Min(100, number));
            }

            var normalized = Normalize(text);
            switch (normalized)
            {
                case "yes":
                case "y":
                case "true":
                case "high":
                case "flagged":
                case "risk":
                    return 100;
                case "medium":
                case "med":
                    return 50;
                case "low":
                    return 25;
                case "no":
                case "n":
                case "false":
                case "none":
                case "nil":
                case "clear":
                    return 0;
            }

            if (category != null && normalized == Normalize(category)) return 100;
            return 0;
        }

        /// <summary>Scores for each category, in the order of <see cref="Categories"/>.</summary>
        public static double[] ForPassenger(bchn_data passenger)
        {
            return new[]
            {
                Parse(passenger.Terrorism, "Terrorism"),
                Parse(passenger.Narcotics, "Narcotics"),
                Parse(passenger.Smuggling, "Smuggling"),
                Parse(passenger.Illegal_Immigration, "Illegal Immigration"),
                Parse(passenger.Revenue, "Revenue")
            };
        }

        /// <summary>A passenger's overall risk is their highest category score.</summary>
        public static double Overall(bchn_data passenger)
        {
            var max = 0d;
            foreach (var score in ForPassenger(passenger))
            {
                if (score > max) max = score;
            }
            return max;
        }

        private static string Normalize(string value)
        {
            return value.Replace("_", "").Replace(" ", "").Replace("-", "").ToLowerInvariant();
        }
    }
}
