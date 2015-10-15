using System.Web.Mvc;
using ElmahR.Models;

namespace ElmahR.Controllers
{
    public class HashController : Controller
    {
        [AllowAnonymous]
        public string Index(string id = "")
        {
            if (!string.IsNullOrWhiteSpace(id))
            {
                var hash = HashGenerator.GetHashString(id);
                ViewBag.Message = hash;
                return hash;
            }
            return string.Empty;
        }
    }
}