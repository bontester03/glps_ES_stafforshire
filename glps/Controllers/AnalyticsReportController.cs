using System.Collections.Generic;
using System.Linq;
using System.Web.Mvc;
using glps.DAL;
using glps.Models;
using glps.Services;

namespace glps.Controllers
{
    public class AnalyticsReportController : Controller
    {
        private readonly glpsContext db = new glpsContext();

        // GET: AnalyticsReport
        public ActionResult Index()
        {
            return View(LoadAnalytics().Summary());
        }

        public ActionResult passenger()
        {
            return ChartView(LoadAnalytics().ByRiskType());
        }

        public ActionResult Airport()
        {
            return ChartView(LoadAnalytics().ByAirport());
        }

        public ActionResult Terminal()
        {
            return ChartView(LoadAnalytics().ByTerminal());
        }

        public ActionResult Flight()
        {
            return ChartView(LoadAnalytics().ByAirline());
        }

        private ActionResult ChartView(List<DataPoint> dataPoints)
        {
            ViewBag.HasData = dataPoints.Count > 0;
            ViewBag.DataPoints = ChartJson.Serialize(dataPoints);
            return View();
        }

        private RiskAnalytics LoadAnalytics()
        {
            return new RiskAnalytics(
                db.bchn_Datas.AsNoTracking().ToList(),
                db.appin_Datas.AsNoTracking().ToList(),
                db.asset_Details.AsNoTracking().ToList(),
                db.airport_Datas.AsNoTracking().ToList(),
                db.airline_Details.AsNoTracking().ToList());
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                db.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}
