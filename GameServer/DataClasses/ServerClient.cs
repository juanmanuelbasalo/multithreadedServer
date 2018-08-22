using System.Net.Sockets;

namespace GameServer.DataClasses
{
    public class ServerClient
    {
        public string clientName;
        public TcpClient tcp; //el socket del que se conecta el cliente (un socket tiene puerto e ip)
        public bool isMainPlayer;
        public string sprites;
        public int points;
        private bool _isInGame;

        public ServerClient(TcpClient tcp)
        {
            this.tcp = tcp;
        }

        public void SetisInGame(bool _isInGame)
        {
            this._isInGame = _isInGame;
        }
        public bool GetisInGame()
        {
            return _isInGame;
        }
    }
}
