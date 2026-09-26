using System.Web;
using System.Web.Mvc;
using glps.Infrastructure;

namespace glps
{
    public class FilterConfig
    {
        public static void RegisterGlobalFilters(GlobalFilterCollection filters)
        {
            filters.Add(new HandleErrorAttribute());
            // Every page requires login unless the action is marked [AllowAnonymous].
            filters.Add(new SessionAuthorizeAttribute());
        }
    }
}
