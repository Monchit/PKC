using System.Web;
using System.Web.Mvc;

namespace PackingChange1.Controllers
{
    public class Chk_Authen : ActionFilterAttribute
    {
        public override void OnActionExecuting(ActionExecutingContext filterContext)
        {
            if (HttpContext.Current.Session["PCO_Auth"] == null)
            {
                string loginpath = "~/Home/Index";
                if (HttpContext.Current.Request.Url != null)
                {
                    HttpContext.Current.Session["Redirect"] = HttpContext.Current.Request.Url;
                }
                filterContext.Result = new RedirectResult(loginpath);
            }
        }
    }
}