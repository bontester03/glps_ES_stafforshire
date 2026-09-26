using System.Web;
using System.Web.Mvc;
using System.Web.Routing;

namespace glps.Infrastructure
{
    /// <summary>
    /// Requires a logged-in user (Session["User_ID"]) for every action it applies to.
    /// Registered globally in FilterConfig; use [AllowAnonymous] to opt an action out.
    /// </summary>
    public class SessionAuthorizeAttribute : AuthorizeAttribute
    {
        public const string UserIdKey = "User_ID";
        public const string EmailKey = "EmailID";

        protected override bool AuthorizeCore(HttpContextBase httpContext)
        {
            return httpContext.Session != null && httpContext.Session[UserIdKey] != null;
        }

        protected override void HandleUnauthorizedRequest(AuthorizationContext filterContext)
        {
            var request = filterContext.HttpContext.Request;
            if (request.IsAjaxRequest())
            {
                filterContext.Result = new HttpStatusCodeResult(401, "Login required");
                return;
            }

            filterContext.Result = new RedirectToRouteResult(new RouteValueDictionary
            {
                { "controller", "Home" },
                { "action", "UserLogin" },
                { "returnUrl", request.RawUrl }
            });
        }
    }
}
