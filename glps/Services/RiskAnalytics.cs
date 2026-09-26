using System;
using System.Collections.Generic;
using System.Linq;
using glps.Models;

namespace glps.Services
{
    /// <summary>
    /// Builds the analytics chart data from the database tables.
    /// Passengers (bchn_data) are linked to flights through the manifest (appin_data) by
    /// passport number; flights (asset_details) give the airports and terminal, and the
    /// airline is found from the flight number prefix (airline_details.letter_code).
    /// </summary>
    public class RiskAnalytics
    {
        private readonly List<bchn_data> _passengers;
        private readonly List<appin_data> _manifest;
        private readonly Dictionary<string, asset_details> _flights;
        private readonly Dictionary<string, airport_data> _airports;
        private readonly List<airline_details> _airlines;
        private readonly Dictionary<string, double> _riskByPassport;

        public RiskAnalytics(
            IEnumerable<bchn_data> passengers,
            IEnumerable<appin_data> manifest,
            IEnumerable<asset_details> flights,
            IEnumerable<airport_data> airports,
            IEnumerable<airline_details> airlines)
        {
            _passengers = (passengers ?? Enumerable.Empty<bchn_data>()).ToList();
            _manifest = (manifest ?? Enumerable.Empty<appin_data>()).ToList();
            _flights = FirstByKey(flights, f => Key(f.Flight));
            _airports = FirstByKey(airports, a => Key(a.IATA_code));
            _airlines = (airlines ?? Enumerable.Empty<airline_details>())
                .Where(a => !string.IsNullOrWhiteSpace(a.letter_code))
                .OrderByDescending(a => a.letter_code.Trim().Length)
                .ToList();

            _riskByPassport = new Dictionary<string, double>();
            foreach (var passenger in _passengers)
            {
                var key = Key(passenger.Passport_number);
                if (key == null) continue;
                double existing;
                var score = RiskScore.Overall(passenger);
                if (!_riskByPassport.TryGetValue(key, out existing) || score > existing)
                {
                    _riskByPassport[key] = score;
                }
            }
        }

        /// <summary>Average score per threat type across all screened passengers.</summary>
        public List<DataPoint> ByRiskType()
        {
            var points = new List<DataPoint>();
            if (_passengers.Count == 0) return points;

            var scores = _passengers.Select(RiskScore.ForPassenger).ToList();
            for (var i = 0; i < RiskScore.Categories.Length; i++)
            {
                var index = i;
                var flagged = scores.Count(s => s[index] >= RiskScore.HighRiskThreshold);
                points.Add(new DataPoint(RiskScore.Categories[i], Round(scores.Average(s => s[index])), flagged));
            }
            return points;
        }

        /// <summary>Average passenger risk grouped by departure airport.</summary>
        public List<DataPoint> ByAirport()
        {
            return Group(entry =>
            {
                var flight = FindFlight(entry.Flight);
                return flight == null ? null : AirportName(flight.Departure);
            });
        }

        /// <summary>Average passenger risk grouped by arrival airport and terminal.</summary>
        public List<DataPoint> ByTerminal()
        {
            return Group(entry =>
            {
                var flight = FindFlight(entry.Flight);
                if (flight == null) return null;
                var airport = AirportName(flight.Arrival) ?? "Unknown airport";
                return string.IsNullOrWhiteSpace(flight.Terminal)
                    ? airport
                    : airport + " - " + TerminalLabel(flight.Terminal);
            });
        }

        /// <summary>Average passenger risk grouped by airline.</summary>
        public List<DataPoint> ByAirline()
        {
            return Group(entry => AirlineName(entry.Flight));
        }

        public DashboardSummary Summary()
        {
            var overall = _passengers.Select(p => new { Passenger = p, Risk = RiskScore.Overall(p) }).ToList();
            return new DashboardSummary
            {
                PassengersScreened = _passengers.Count,
                ManifestEntries = _manifest.Count,
                Flights = _flights.Count,
                HighRiskPassengers = overall.Count(p => p.Risk >= RiskScore.HighRiskThreshold),
                AverageRisk = overall.Count == 0 ? 0 : Round(overall.Average(p => p.Risk)),
                TopRisks = overall
                    .Where(p => p.Risk > 0)
                    .OrderByDescending(p => p.Risk)
                    .Take(10)
                    .Select(p => new PassengerRisk { Passenger = p.Passenger, Risk = p.Risk })
                    .ToList()
            };
        }

        private List<DataPoint> Group(Func<appin_data, string> labelFor)
        {
            var groups = new Dictionary<string, List<double>>();
            foreach (var entry in _manifest)
            {
                double risk;
                var passport = Key(entry.Passport_Number);
                if (passport == null || !_riskByPassport.TryGetValue(passport, out risk)) continue;

                var label = labelFor(entry);
                if (string.IsNullOrWhiteSpace(label)) continue;

                List<double> scores;
                if (!groups.TryGetValue(label, out scores))
                {
                    scores = new List<double>();
                    groups[label] = scores;
                }
                scores.Add(risk);
            }

            return groups
                .Select(g => new DataPoint(g.Key, Round(g.Value.Average()), g.Value.Count))
                .OrderByDescending(p => p.Y)
                .ThenBy(p => p.Label)
                .ToList();
        }

        private asset_details FindFlight(string flightNumber)
        {
            var key = Key(flightNumber);
            asset_details flight;
            return key != null && _flights.TryGetValue(key, out flight) ? flight : null;
        }

        private string AirportName(string code)
        {
            if (string.IsNullOrWhiteSpace(code)) return null;
            airport_data airport;
            if (_airports.TryGetValue(Key(code), out airport) && !string.IsNullOrWhiteSpace(airport.Long_Name))
            {
                return airport.Long_Name.Trim();
            }
            return code.Trim();
        }

        private string AirlineName(string flightNumber)
        {
            var key = Key(flightNumber);
            if (key == null) return null;

            var airline = _airlines.FirstOrDefault(a => key.StartsWith(Key(a.letter_code), StringComparison.Ordinal));
            if (airline != null && !string.IsNullOrWhiteSpace(airline.Company_name))
            {
                return airline.Company_name.Trim();
            }

            var prefix = new string(key.TakeWhile(char.IsLetter).ToArray());
            return prefix.Length > 0 ? prefix + " (unknown airline)" : "Unknown airline";
        }

        private static string TerminalLabel(string terminal)
        {
            var value = terminal.Trim();
            return value.All(char.IsDigit) ? "Terminal " + value : value;
        }

        private static Dictionary<string, T> FirstByKey<T>(IEnumerable<T> items, Func<T, string> keyFor)
        {
            var result = new Dictionary<string, T>();
            foreach (var item in items ?? Enumerable.Empty<T>())
            {
                var key = keyFor(item);
                if (key != null && !result.ContainsKey(key)) result[key] = item;
            }
            return result;
        }

        /// <summary>Normalises codes for matching: trimmed, upper case, no spaces.</summary>
        internal static string Key(string value)
        {
            if (string.IsNullOrWhiteSpace(value)) return null;
            return value.Replace(" ", "").Trim().ToUpperInvariant();
        }

        private static double Round(double value)
        {
            return Math.Round(value, 1);
        }
    }

    public class DashboardSummary
    {
        public int PassengersScreened { get; set; }
        public int ManifestEntries { get; set; }
        public int Flights { get; set; }
        public int HighRiskPassengers { get; set; }
        public double AverageRisk { get; set; }
        public List<PassengerRisk> TopRisks { get; set; }
    }

    public class PassengerRisk
    {
        public bchn_data Passenger { get; set; }
        public double Risk { get; set; }
    }
}
