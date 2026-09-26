using System;
using System.Collections.Generic;
using System.Data.Entity.Validation;
using System.IO;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using glps.DAL;
using glps.Infrastructure;
using glps.Models;
using glps.Services;

namespace glps.Controllers
{
    public class HomeController : Controller
    {
        private readonly glpsContext db = new glpsContext();

        public ActionResult Index()
        {
            return View();
        }

        [AllowAnonymous]
        public ActionResult UserLogin(string returnUrl)
        {
            if (Session[SessionAuthorizeAttribute.UserIdKey] != null)
            {
                return RedirectToLocal(returnUrl);
            }

            ViewBag.ReturnUrl = returnUrl;
            return View();
        }

        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public ActionResult UserLogin(string EmailID, string Password, string returnUrl)
        {
            ViewBag.ReturnUrl = returnUrl;

            if (string.IsNullOrWhiteSpace(EmailID) || string.IsNullOrEmpty(Password))
            {
                ViewBag.Error = "Please enter your email and password.";
                return View();
            }

            var email = EmailID.Trim();
            var user = db.users.FirstOrDefault(u => u.EmailID == email);
            if (user == null || !PasswordHasher.Verify(Password, user.Password))
            {
                ViewBag.Error = "Invalid email or password.";
                return View();
            }

            // Upgrade accounts that still have a plaintext password.
            if (!PasswordHasher.IsHashed(user.Password))
            {
                user.Password = PasswordHasher.Hash(Password);
                db.SaveChanges();
            }

            Session.Clear();
            Session[SessionAuthorizeAttribute.UserIdKey] = user.ID;
            Session[SessionAuthorizeAttribute.EmailKey] = user.EmailID;
            return RedirectToLocal(returnUrl);
        }

        [AllowAnonymous]
        public ActionResult Logout()
        {
            Session.Clear();
            Session.Abandon();
            return RedirectToAction("UserLogin");
        }

        public ActionResult About()
        {
            ViewBag.Message = "Your application description page.";

            return View();
        }

        public ActionResult Contact()
        {
            ViewBag.Message = "Your contact page.";

            return View();
        }

        public ActionResult UploadData()
        {
            ViewBag.PassengerCount = db.bchn_Datas.Count();
            return View();
        }

        public ActionResult DownloadTemplate()
        {
            return File(BchnExcelImporter.CreateTemplate(), BchnExcelImporter.ContentType, "bchn_data_template.xlsx");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public JsonResult UploadExcel(HttpPostedFileBase FileUpload)
        {
            if (FileUpload == null || FileUpload.ContentLength == 0)
            {
                return UploadResult(false, 0, "Please choose an Excel file.");
            }

            var extension = Path.GetExtension(FileUpload.FileName ?? "").ToLowerInvariant();
            if (extension != ".xlsx")
            {
                return UploadResult(false, 0, extension == ".xls"
                    ? "Legacy .xls files are not supported. Please save the file as .xlsx and try again."
                    : "Only Excel (.xlsx) files are allowed.");
            }

            var parsed = BchnExcelImporter.Read(FileUpload.InputStream);
            var errors = parsed.Errors;

            var passports = parsed.Rows.Select(r => r.Passport_number).ToList();
            var existing = db.bchn_Datas
                .Where(b => passports.Contains(b.Passport_number))
                .Select(b => b.Passport_number)
                .ToList();
            errors.AddRange(existing.Select(p => "Passport number " + p + " already exists."));

            if (errors.Count > 0)
            {
                return UploadResult(false, 0, errors.ToArray());
            }

            try
            {
                db.bchn_Datas.AddRange(parsed.Rows);
                db.SaveChanges();
            }
            catch (DbEntityValidationException ex)
            {
                var messages = ex.EntityValidationErrors
                    .SelectMany(e => e.ValidationErrors)
                    .Select(e => e.PropertyName + ": " + e.ErrorMessage)
                    .ToArray();
                return UploadResult(false, 0, messages);
            }

            return UploadResult(true, parsed.Rows.Count);
        }

        public ActionResult ReportData()
        {
            var analytics = new RiskAnalytics(db.bchn_Datas.AsNoTracking().ToList(), null, null, null, null);
            var dataPoints = analytics.ByRiskType();
            ViewBag.HasData = dataPoints.Count > 0;
            ViewBag.DataPoints = ChartJson.Serialize(dataPoints);
            return View();
        }

        private JsonResult UploadResult(bool success, int imported, params string[] errors)
        {
            return Json(new { success, imported, errors });
        }

        private ActionResult RedirectToLocal(string returnUrl)
        {
            if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
            {
                return Redirect(returnUrl);
            }
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
