using System.Web;
using System.Web.Mvc;

namespace PackingChange1.Controllers
{
    public class Chk_Authorize_SP : ActionFilterAttribute
    {
        public override void OnActionExecuting(ActionExecutingContext filterContext)
        {
            if (HttpContext.Current.Session["PCO_UTypeLv"] == null || HttpContext.Current.Session["PCO_UTypeLv"].ToString() != "5")
            {
                filterContext.Result = new RedirectResult("~/Home/Index");
            }
        }
    }
}