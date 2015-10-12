using System.Web.Mvc;
using System.Web.Security;
using ElmahR.Models;

namespace ElmahR.Controllers
{
    public class AdminController : Controller
    {
        [AllowAnonymous]
        public ViewResult LogOn()
        {
            return View();
        }

        [HttpPost][AllowAnonymous]
        public ActionResult LogOn(CredentialsViewModel model, string returnUrl)
        {
            if (ModelState.IsValid)
            {
                if (FormsAuthentication.Authenticate(model.UserName, model.Password))
                {
                    FormsAuthentication.SetAuthCookie(model.UserName, false);
                    return Redirect(returnUrl ?? Url.Action("Index", "Admin"));
                }
                else
                {
                    ModelState.AddModelError("", "Incorrect username or password");
                }
            }

            return View();
        }

        [Authorize]
        public ViewResult Index()
        {
            
            return View();
        }
        public string Hash(string id = "")
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