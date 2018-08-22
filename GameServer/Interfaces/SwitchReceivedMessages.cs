using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using GameServer.DataClasses;
using System.Threading;
using System;

namespace GameServer.Interfaces
{
    //adentro del cuarto del juego
    public abstract class SwitchReceivedMessages
    {
        public abstract Task OnIncommingDataAsync(string[] Adata, ServerClient c, List<ServerClient> listPlayers);
        protected virtual async Task BroadCastAsync(string data, List<ServerClient> sc)
        {
            foreach (ServerClient c in sc)
            {
                StreamWriter streamWriter = new StreamWriter(c.tcp.GetStream());
                await streamWriter.WriteLineAsync(data);
                await streamWriter.FlushAsync();
            }
        }
        protected virtual async Task BroadCastAsync(string data, ServerClient c)
        {
            StreamWriter streamWriter = new StreamWriter(c.tcp.GetStream());
            await streamWriter.WriteLineAsync(data);
            await streamWriter.FlushAsync();
        }
    }

    public sealed class ReceiveCWHO : SwitchReceivedMessages
    {
        public override async Task OnIncommingDataAsync(string[] Adata, ServerClient c, List<ServerClient> listPlayers)
        {
            c.clientName = Adata[1];
            await BroadCastAsync("SCNN|" + c.clientName, listPlayers);
        }
    }
    public sealed class ReceiveCLOR : SwitchReceivedMessages
    {
        public override async Task OnIncommingDataAsync(string[] Adata, ServerClient c, List<ServerClient> listPlayers)
        {
            List<string> msg = new List<string>();
            for (int i = 0; i < 72; i++)
            {
                msg.Add(Adata[i + 1]);
            }
            c.sprites = string.Join("|", msg.ToArray());
            await BroadCastAsync("SLOR|" + c.sprites, listPlayers[listPlayers.Count - 1]);
        }
    }
    public sealed class ReceiveCLORC : SwitchReceivedMessages
    {

        public override async Task OnIncommingDataAsync(string[] Adata, ServerClient c, List<ServerClient> listPlayers)
        {
            List<string> msg = new List<string>();
            for (int i = 1; i < Adata.Length; i++)
            {
                msg.Add(Adata[i]);
            }
            c.sprites = string.Join("|", msg.ToArray());
            await BroadCastAsync("SLORC|" + c.sprites, listPlayers);
        }
    }
    public sealed class ReceiveCPOINT : SwitchReceivedMessages
    {
        public override async Task OnIncommingDataAsync(string[] Adata, ServerClient c, List<ServerClient> listPlayers)
        {
            c.points = int.Parse(Adata[2]);
            await BroadCastAsync("SPOINT|" + Adata[1] + "|" + Adata[2], listPlayers);
        }
    }
    public sealed class ReceiveCGRID : SwitchReceivedMessages
    {
        public override async Task OnIncommingDataAsync(string[] Adata, ServerClient c, List<ServerClient> listPlayers)
        {
            await BroadCastAsync("SGRID|" + Adata[1] + "|" + Adata[2], listPlayers);
        }
    }
    public sealed class ReceiveCWIN : SwitchReceivedMessages
    {
        public override async Task OnIncommingDataAsync(string[] Adata, ServerClient c, List<ServerClient> listPlayers)
        {
            await BroadCastAsync("SWIN|" + Adata[1], listPlayers);           
        }
    }
    public sealed class ReceiveCQUIT : SwitchReceivedMessages
    {
        private List<List<ServerClient>> ClientsInGame;
        private readonly object _lock = new object();

        public ReceiveCQUIT(List<List<ServerClient>> ClientsInGame)
        {
            this.ClientsInGame = ClientsInGame;
        }
        public override async Task OnIncommingDataAsync(string[] Adata, ServerClient c, List<ServerClient> listPlayers)
        {
            if (ClientsInGame.Contains(listPlayers))
            {
                try
                {
                    int indexOfList = ClientsInGame.IndexOf(listPlayers);
                    if (ClientsInGame[indexOfList].Contains(c))
                    {
                        lock (_lock)
                        {
                            ClientsInGame[indexOfList].Remove(c);
                        }                        
                    }                       

                    Console.WriteLine("Count: " + ClientsInGame.Count);
                                     
                }
                catch (Exception e)
                {
                    Console.WriteLine("Exception StackTrace:" + e.StackTrace);
                }
                
            }
        }
    }
}
