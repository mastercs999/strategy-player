using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace Common
{
    [DesignerCategory("Code")]
    public class AdvancedWebClient : WebClient
    {
        private readonly int Timeout;

        public CookieContainer CookieContainer { get; private set; }




        public AdvancedWebClient() : this(60 * 1000)
        {
        }
        public AdvancedWebClient(int timeout) : base()
        {
            Timeout = timeout;
            CookieContainer = new CookieContainer();
        }




        protected override WebRequest GetWebRequest(Uri address)
        {
            HttpWebRequest request = base.GetWebRequest(address) as HttpWebRequest;
            request.Timeout = Timeout;
            request.CookieContainer = CookieContainer;

            return request;
        }
    }
}
