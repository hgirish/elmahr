namespace ElmahR.SampleSource.MVC.Controllers
{
    #region Imports

    using System;
    using System.Web;
    using System.Web.Mvc;

    #endregion

    public class HomeController : Controller
    {
        public ActionResult Index()
        {
            var exceptions = new Action[]
            {
                () => { throw new ApplicationException(); }, 
                () => { throw new ArgumentException(); }, 
                () => { throw new ArgumentNullException(); }, 
                () => { throw new InvalidCastException(); }, 
                () => { throw new NullReferenceException(); }, 
                () => { throw new AccessViolationException(); }, 
                () => { throw new HttpException(); }, 
            };

            var r = new Random(DateTime.Now.Millisecond);
            exceptions[r.Next(exceptions.Length)]();

            return null;
        }
    }
}
