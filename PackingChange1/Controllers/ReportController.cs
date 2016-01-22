using PackingChange1.Models;
using System.Web.Mvc;

namespace PackingChange1.Controllers
{
    public class ReportController : Controller
    {
        private PackingChangeEntities dbPC = new PackingChangeEntities();
        private TNC_ADMINEntities dbTNC = new TNC_ADMINEntities();

        public ActionResult Graph()
        {
            return View();
        }
    }
}