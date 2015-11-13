using System;
using System.Configuration;
using System.Web.Mvc;
using ElmahR.Models;

namespace ElmahR.Controllers
{
    public class CleanupController : Controller
    {
        [AllowAnonymous]
        public ActionResult Index()
        {
            var daysToKeep = ConfigurationManager.AppSettings["DaysToKeepRecords"];
            int days = 0 - Convert.ToInt32(daysToKeep);
            using (var db = new ElmahContext())
            {
                var sql = $"DELETE FROM [ErrorLogEntries] WHERE ReceivedAt < DATEADD(d, {days}, getdate())";
                int result = db.Database.ExecuteSqlCommand(sql);
                var message = $"Deleted old error records: {result}";
                TempData["notifySuccess"] = message;
                return Content(message);
            }
        }
    }
}