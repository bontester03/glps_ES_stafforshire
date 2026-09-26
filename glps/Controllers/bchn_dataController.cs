using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Entity;
using System.Linq;
using System.Net;
using System.Web;
using System.Web.Mvc;
using glps.DAL;
using glps.Models;
using glps.Services;

namespace glps.Controllers
{
    public class bchn_dataController : Controller
    {
        private glpsContext db = new glpsContext();

        // GET: bchn_data
        public ActionResult Index()
        {
            return View(db.bchn_Datas.ToList());
        }

        // GET: bchn_data/Details/5
        public ActionResult Details(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            bchn_data bchn_data = db.bchn_Datas.Find(id);
            if (bchn_data == null)
            {
                return HttpNotFound();
            }

            var scores = RiskScore.ForPassenger(bchn_data);
            var dataPoints = RiskScore.Categories.Select((c, i) => new DataPoint(c, scores[i])).ToList();
            ViewBag.OverallRisk = RiskScore.Overall(bchn_data);
            ViewBag.DataPoints = ChartJson.Serialize(dataPoints);
            return View(bchn_data);
        }

        // GET: bchn_data/Create
        public ActionResult Create()
        {
            return View();
        }

        // POST: bchn_data/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to, for 
        // more details see https://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create([Bind(Include = "Id,Fore_Nane,Family_Name,Gender,DOB,Nationality,Passport_number,Terrorism,Narcotics,Smuggling,Illegal_Immigration,Revenue")] bchn_data bchn_data)
        {
            if (ModelState.IsValid)
            {
                db.bchn_Datas.Add(bchn_data);
                db.SaveChanges();
                return RedirectToAction("Index");
            }

            return View(bchn_data);
        }

        // GET: bchn_data/Edit/5
        public ActionResult Edit(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            bchn_data bchn_data = db.bchn_Datas.Find(id);
            if (bchn_data == null)
            {
                return HttpNotFound();
            }
            return View(bchn_data);
        }

        // POST: bchn_data/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to, for 
        // more details see https://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Edit([Bind(Include = "Id,Fore_Nane,Family_Name,Gender,DOB,Nationality,Passport_number,Terrorism,Narcotics,Smuggling,Illegal_Immigration,Revenue")] bchn_data bchn_data)
        {
            if (ModelState.IsValid)
            {
                db.Entry(bchn_data).State = EntityState.Modified;
                db.SaveChanges();
                return RedirectToAction("Index");
            }
            return View(bchn_data);
        }

        // GET: bchn_data/Delete/5
        public ActionResult Delete(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            bchn_data bchn_data = db.bchn_Datas.Find(id);
            if (bchn_data == null)
            {
                return HttpNotFound();
            }
            return View(bchn_data);
        }

        // POST: bchn_data/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public ActionResult DeleteConfirmed(int id)
        {
            bchn_data bchn_data = db.bchn_Datas.Find(id);
            db.bchn_Datas.Remove(bchn_data);
            db.SaveChanges();
            return RedirectToAction("Index");
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
