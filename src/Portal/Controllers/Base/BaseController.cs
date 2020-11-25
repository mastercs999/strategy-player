using Common;
using Common.Extensions;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Web;
using System.Web.Mvc;
using System.Web.Security;
using System.Web.Mvc.Filters;

namespace Portal.Controllers.Base
{
    public partial class BaseController : Controller
    {
        protected string SourceFolder => Properties.Settings.Default.SourceFolder;
        protected string WorkingDirectory => Properties.Settings.Default.WorkingDirectory;

        private string Username => Properties.Settings.Default.Username;
        private string Password => Properties.Settings.Default.Password;

        public virtual ActionResult Menu()
        {
            return PartialView();
        }

        public virtual ActionResult SignOut()
        {
            FormsAuthentication.SignOut();

            return Redirect("https://www.google.com");
        }

        protected override void OnAuthentication(AuthenticationContext filterContext)
        {
            // Are we already logged in?
            HttpCookie authCookie = Request.Cookies[FormsAuthentication.FormsCookieName];
            if (authCookie != null && FormsAuthentication.Decrypt(authCookie.Value).Name == Username)
                return;

            string authorize = filterContext.HttpContext.Request.Headers["Authorization"];
            if (!String.IsNullOrEmpty(authorize))
            {
                // Decode given credentials
                string[] parts = Encoding.ASCII.GetString(Convert.FromBase64String(authorize.Substring(6))).Split(':');
                string username = parts[0];
                string password = parts[1];

                if (username == Username && password == Password)
                {
                    // Create ticket
                    FormsAuthenticationTicket ticket = new FormsAuthenticationTicket(1,
                    username,
                    DateTime.Now,
                    DateTime.Now.AddDays(30),
                    true,
                    username,
                    FormsAuthentication.FormsCookiePath);

                    // Create the cookie.
                    HttpCookie cookie = new HttpCookie(FormsAuthentication.FormsCookieName, FormsAuthentication.Encrypt(ticket));
                    cookie.Expires = ticket.Expiration;

                    // Save the cookie
                    Response.Cookies.Add(cookie);

                    return;
                }
            }

            // Authentication challenge
            FormsAuthentication.SignOut();
            filterContext.HttpContext.Response.AddHeader("WWW-Authenticate", "Basic realm=\"Secured\"");
            filterContext.Result = new HttpUnauthorizedResult();
        }

        protected override void OnException(ExceptionContext filterContext)
        {
            Exception ex = filterContext.Exception;

            // Throw real exception
            if (ex is ThreadInterruptedException)
                ThreadMessage.ThrownException.Rethrow();

            if (filterContext.HttpContext.Request.IsAjaxRequest())
            {
                filterContext.ExceptionHandled = true;
                filterContext.HttpContext.Response.StatusCode = (int)HttpStatusCode.InternalServerError;
                filterContext.Result = new JsonResult()
                {
                    Data = new
                    {
                        Message = ex.Message,
                        Exception = ex.ToString()
                    }
                };
            }
        }
    }
}