using System.Collections.Generic;
using System.Threading.Tasks;
using GameServer.Interfaces;
using GameServer.DataClasses;

namespace GameServer.Helpers
{
    public class SwitchContext
    {
        private Dictionary<string, SwitchReceivedMessages> _messages = new Dictionary<string, SwitchReceivedMessages>();

        public SwitchContext(List<List<ServerClient>> lsc)
        {
            _messages.Add("CWHO", new ReceiveCWHO());
            _messages.Add("CLOR", new ReceiveCLOR());
            _messages.Add("CLORC", new ReceiveCLORC());
            _messages.Add("CPOINT", new ReceiveCPOINT());
            _messages.Add("CGRID", new ReceiveCGRID());
            _messages.Add("CWIN", new ReceiveCWIN());
            _messages.Add("CQUIT", new ReceiveCQUIT(lsc));
        }

        public async Task DoProcessDataAsync(string data, ServerClient c, List<ServerClient> listPlayers)
        {
            await _messages[data.Split('|')[0]].OnIncommingDataAsync(data.Split('|'), c, listPlayers);
        }
    }
}
