using GameServer.DataClasses;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace GameServer.Business
{
    class AddPlayersToGameRoom
    {
        private readonly int _tamanoCola;

        public Queue<ServerClient> QueueToPlay { get; set; }
    
        public AddPlayersToGameRoom()
        {
            QueueToPlay = new Queue<ServerClient>();
            _tamanoCola = 2;
        }

        public async Task AddToGame(List<ServerClient> clients, List<List<ServerClient>> clientsInGame)
        {
            List<ServerClient> _players = new List<ServerClient>();
            string _msg = "";
            for (int i = 0; i < _tamanoCola; i++)
            {
                _players.Add(QueueToPlay.Dequeue());
                _msg = _players[i].clientName + "|";
                _players[i].isMainPlayer = (i == 0);
                _players[i].SetisInGame(true); // ya esta en el juego 
                clients.Remove(_players[i]); //quitando los clientes del juego principal para que esten solo en el juego
                await BroadCastAsync("SWHO|" + _msg + ((_players[i].isMainPlayer) ? 1 : 0).ToString(), _players[i]);
            }
            CheckForEmptySpace(clientsInGame, _players);
        }

        private void CheckForEmptySpace(List<List<ServerClient>> listIngGame, List<ServerClient> players)
        {
            try
            {
                for (int i = 0; i < listIngGame.Count; i++)
                {
                    if (listIngGame[i].Count == 0)
                    {
                        foreach (ServerClient sc in players)
                            listIngGame[i].Add(sc);
                        return;
                    }
                }
                listIngGame.Add(players);
            }
            catch (Exception e)
            {
                Console.WriteLine("Message: " + e.Message);
                Console.WriteLine("Stack: " + e.StackTrace);
            }           
        }
        //overload para mandar info a 1 solo cliente
        private async Task BroadCastAsync(string data, ServerClient c)
        {
            StreamWriter streamWriter = new StreamWriter(c.tcp.GetStream());
            await streamWriter.WriteLineAsync(data);
            await streamWriter.FlushAsync();
        }
    }
}
