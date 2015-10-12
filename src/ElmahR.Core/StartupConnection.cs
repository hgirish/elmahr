namespace ElmahR.Core
{
    #region Imports

    using System;
    using System.Linq;
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using Microsoft.AspNet.SignalR;

    #endregion

    // ElmahR persistent connection for startup

    public class StartupConnection : PersistentConnection 
    {
        private readonly IDictionary<string, Func<IConnection, IRequest, string, string, Task>> _senders =
            new Dictionary<string, Func<IConnection, IRequest, string, string, Task>>(StringComparer.OrdinalIgnoreCase)
            {
                {
                    "startup",
                    (c, r, cid, d) => c.Send(cid, new { Selector = d, Answer = Plugins.BuildPlugins().ToArray() }) 
                },
                {
                    "*",
                    (c, r, cid, d) => c.Send(cid, new { Selector = d, Answer = new KeyValuePair<string, KeyValuePair<string, string>>[0] }) 
                }
            };

        protected override Task OnReceived(IRequest request, string connectionId, string data)
        {
            var key = _senders.ContainsKey(data) 
                    ? data 
                    : "*";
            return _senders[key](Connection, request, connectionId, data);
        }
    }
}