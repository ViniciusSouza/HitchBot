using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Http;
using System.Web.Routing;
using System.Web.SessionState;
using TinderLibrary;

namespace tinderbot
{
    public class WebApiApplication : System.Web.HttpApplication
    {

        public Dictionary<string, TinderSession> TinderSessions
        {
            get
            {
                if (_tinderSessions == null)
                    _tinderSessions = new Dictionary<string, TinderSession>();
                return _tinderSessions;
            }
            set { _tinderSessions = value; }
        }

        public Dictionary<string, List<string>> Conversations
        {
            get
            {
                if (_conversations == null)
                    _conversations = new Dictionary<string, List<string>>();
                return _conversations;
            }
            set { _conversations = value; }
        }

        private Dictionary<string, TinderSession> _tinderSessions;

        private Dictionary<string, List<string>> _conversations;



        protected void Application_Start()
        {
            //TinderSessions = new Dictionary<string, TinderSession>();
            GlobalConfiguration.Configure(WebApiConfig.Register);
            
        }

        protected void Application_PostAuthorizeRequest()
        {
            HttpContext.Current.SetSessionStateBehavior(SessionStateBehavior.Required);
        }



    }
}
