using System.Linq;
using glps.Models;
using glps.Services;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace glps.Tests.Services
{
    [TestClass]
    public class RiskAnalyticsTest
    {
        [TestMethod]
        public void ParsesRiskValues()
        {
            Assert.AreEqual(35, RiskScore.Parse("35"));
            Assert.AreEqual(35, RiskScore.Parse(" 35% "));
            Assert.AreEqual(100, RiskScore.Parse("250"));
            Assert.AreEqual(100, RiskScore.Parse("Yes"));
            Assert.AreEqual(100, RiskScore.Parse("1"));
            Assert.AreEqual(0, RiskScore.Parse("No"));
            Assert.AreEqual(0, RiskScore.Parse(null));
            Assert.AreEqual(50, RiskScore.Parse("Medium"));
            Assert.AreEqual(100, RiskScore.Parse("Illegal_Immigration", "Illegal Immigration"));
            Assert.AreEqual(0, RiskScore.Parse("unknown text", "Terrorism"));
        }

        [TestMethod]
        public void OverallIsHighestCategory()
        {
            var passenger = new bchn_data { Terrorism = "10", Narcotics = "70%", Revenue = "No" };
            Assert.AreEqual(70, RiskScore.Overall(passenger));
        }

        [TestMethod]
        public void BuildsChartsFromJoinedTables()
        {
            var analytics = new RiskAnalytics(
                new[]
                {
                    new bchn_data { Id = 1, Fore_Nane = "A", Passport_number = "P1", Terrorism = "80" },
                    new bchn_data { Id = 2, Fore_Nane = "B", Passport_number = "P2", Smuggling = "20" },
                    new bchn_data { Id = 3, Fore_Nane = "C", Passport_number = "P3", Revenue = "Yes" }
                },
                new[]
                {
                    new appin_data { Flight = "BA 123", Passport_Number = "p1" },
                    new appin_data { Flight = "BA123", Passport_Number = "P2" },
                    new appin_data { Flight = "FR9", Passport_Number = "P3" },
                    new appin_data { Flight = "FR9", Passport_Number = "NOT-SCREENED" }
                },
                new[]
                {
                    new asset_details { Flight = "BA123", Departure = "JFK", Arrival = "LHR", Terminal = "5" },
                    new asset_details { Flight = "FR9", Departure = "DUB", Arrival = "MAN", Terminal = "T2" }
                },
                new[]
                {
                    new airport_data { IATA_code = "JFK", Long_Name = "John F Kennedy" },
                    new airport_data { IATA_code = "LHR", Long_Name = "Heathrow" }
                },
                new[]
                {
                    new airline_details { letter_code = "BA", Company_name = "British Airways" }
                });

            var byAirport = analytics.ByAirport();
            Assert.AreEqual(2, byAirport.Count);
            Assert.AreEqual("DUB", byAirport[0].Label);
            Assert.AreEqual(100, byAirport[0].Y);
            Assert.AreEqual("John F Kennedy", byAirport[1].Label);
            Assert.AreEqual(50, byAirport[1].Y);
            Assert.AreEqual(2, byAirport[1].Count);

            var byTerminal = analytics.ByTerminal().Select(p => p.Label).ToList();
            CollectionAssert.AreEquivalent(new[] { "Heathrow - Terminal 5", "MAN - T2" }, byTerminal);

            var byAirline = analytics.ByAirline().Select(p => p.Label).ToList();
            CollectionAssert.AreEquivalent(new[] { "British Airways", "FR (unknown airline)" }, byAirline);

            var byType = analytics.ByRiskType();
            Assert.AreEqual(RiskScore.Categories.Length, byType.Count);
            Assert.AreEqual(26.7, byType.Single(p => p.Label == "Terrorism").Y);
            Assert.AreEqual(1, byType.Single(p => p.Label == "Terrorism").Count);

            var summary = analytics.Summary();
            Assert.AreEqual(3, summary.PassengersScreened);
            Assert.AreEqual(2, summary.HighRiskPassengers);
            Assert.AreEqual("C", summary.TopRisks[0].Passenger.Fore_Nane);
        }

        [TestMethod]
        public void EmptyTablesGiveEmptyCharts()
        {
            var analytics = new RiskAnalytics(null, null, null, null, null);
            Assert.AreEqual(0, analytics.ByRiskType().Count);
            Assert.AreEqual(0, analytics.ByAirport().Count);
            Assert.AreEqual(0, analytics.Summary().AverageRisk);
        }
    }
}
