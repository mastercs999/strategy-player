using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Analyzer.Mocking.Gateway
{
    public interface IGateway
    {
        void Start(string version, string username, string password, bool liveAccount);
        void Stop();
    }
}
