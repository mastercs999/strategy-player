using Common.Loggers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Analyzer.Mocking.Gateway
{
    public class RealtimeGateway : IGateway
    {
        private GatewayController.Gateway Gateway;




        public RealtimeGateway()
        {
            Gateway = new GatewayController.Gateway();
        }
        public RealtimeGateway(ILogger logger)
        {
            Gateway = new GatewayController.Gateway(logger);
        }




        public void Start(string version, string username, string password, bool liveAccount)
        {
            Gateway.Start(version, username, password, liveAccount);
        }

        public void Stop()
        {
            Gateway.Stop();
        }
    }
}
