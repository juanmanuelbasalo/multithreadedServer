using System;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Net;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using GameServer.DataClasses;
using GameServer.Helpers;

namespace GameServer.Business
{
    public class Server
    {
        private readonly int port = 7777; //el puerto al que se van a conectar los clientes

        private List<ServerClient> _clients; // los clientes que se conectan al servidor
        private AddPlayersToGameRoom _addPlayersToGameRoom; // hace la funcion de una cola
        private List<List<ServerClient>> _clientsInGame; // los clientes actualmente  jugando
        private readonly TcpListener _server; // el socket por donde el servidor va a escuchar a los clientes
        private readonly SwitchContext _switchMessagesReceived;
        private bool _serverStarted; // si el servidor esta activo
        private readonly object _lock; // un lock para agregar y eliminar
        private ManualResetEvent _manualReset = new ManualResetEvent(false); // es como un lock, para evitar que varios hilos traten de acceder a una variable

        //constructor para inicializar
        public Server()
        {
            _clients = new List<ServerClient>();
            _addPlayersToGameRoom = new AddPlayersToGameRoom();
            _clientsInGame = new List<List<ServerClient>>();
            _server = new TcpListener(IPAddress.Any, port);
            _lock = new object();
            _switchMessagesReceived = new SwitchContext(_clientsInGame);
        }
        //funcion para iniciar el servidor
        public void Init()
        {
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine(" Tratando de iniciar el servidor en el puerto: " + port);
            Console.ResetColor();
            Console.WriteLine(" ------------------------------------------------------------------ ".PadRight(Console.WindowWidth - 1));

            try
            {
                _server.Start();
                StartListening();
                _serverStarted = true;

                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine(" El servidor se inicio con exito en el puerto: " + port);
                Console.ResetColor();

                Task.Run(MainRoomAsync);
                Task.Run(PlayingRoomAsync);
            }
            catch (Exception e)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("Error en el socket: " + e.Message);
                Console.ResetColor();
            }
        }
        
        // funcion para empezar a escuchar a los clientes
        private void StartListening()
        {
            _server.BeginAcceptTcpClient(new AsyncCallback(AcceptTcpClient), _server);
        }
        // cuando ya se acepto al cliente
        private void AcceptTcpClient(IAsyncResult ar)
        {
            _manualReset.Reset();
            TcpListener listener = (TcpListener)ar.AsyncState;

            ServerClient sc = new ServerClient(listener.EndAcceptTcpClient(ar));
            sc.SetisInGame(false); // no esta dentro del juego, esta en el menu principal
            _manualReset.Set();
            _clients.Add(sc);

            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine(" Se unio un nuevo cliente al servidor: " + sc.clientName);
            Console.ResetColor();

            StartListening();
        }
        //revisar si esta conectado el cliente
        private bool IsConnected(TcpClient c)
        {
            try
            {
                if (c != null && c.Client != null && c.Client.Connected)
                {
                    if (c.Client.Poll(0, SelectMode.SelectRead))
                        return !(c.Client.Receive(new byte[1], SocketFlags.Peek) == 0);

                    return true;
                }
                else
                    return false;
            }
            catch
            {
                return false;
            }
        }

        //overload para mandar info a 1 solo cliente
        private async Task BroadCastAsync(string data, ServerClient c)
        {
            StreamWriter streamWriter = new StreamWriter(c.tcp.GetStream());           
            await streamWriter.WriteLineAsync(data);
            await streamWriter.FlushAsync();                                   
        }

        //overload para poder recibir informacion del cliente y mandartela a ti mismo (menu principal)
        private async Task OnIncomingDataAsync(ServerClient c, string data)
        {
            string[] aData = data.Split('|');

            switch (aData[0])
            {
                case "CMATCH":
                    if (aData[1].Contains("true"))
                    {
                        lock(_lock)
                            _addPlayersToGameRoom.QueueToPlay.Enqueue(c);
                        
                        if (_addPlayersToGameRoom.QueueToPlay.Count == 2)
                        {
                            await Task.Run(() => RoomThreadAsync());
                        }
                        else
                            await BroadCastAsync("SMATCH|false", c);
                    }
                    else if (aData[1].Contains("false"))
                    {
                        lock(_lock)
                             _addPlayersToGameRoom.QueueToPlay.Dequeue();

                        await BroadCastAsync("SMATCH|true", c);
                    }
                    break;

                case "CQUIT":
                    if (_addPlayersToGameRoom.QueueToPlay.Contains(c))
                    {
                        lock(_lock)
                             _addPlayersToGameRoom.QueueToPlay.Dequeue();
                    }
                        
                    if (_clients.Contains(c))
                    {
                        lock (_lock)
                        {
                            _clients.Remove(c);
                        }                      
                    }                                              
                    break;
            }
        }

        //leer y enviar datos en el menu principal
        private async Task ReadAsync(ServerClient clients)
        {
            if (clients?.tcp == null)
                return;
             
            _manualReset.WaitOne();
            if (!clients.GetisInGame())
            {
                NetworkStream networkStream = clients.tcp.GetStream();

                if (networkStream.DataAvailable)
                {
                    StreamReader streamReader = new StreamReader(networkStream, true);                            
                    string data = await streamReader.ReadLineAsync();
                   
                     if (data != null)
                         await OnIncomingDataAsync(clients, data);   
                }                     
            }  
        }

        //mismo metodo que prueba pero para 2 clientes (dentro del juego)
        private async Task ReadAsync(ServerClient client ,List<ServerClient> clients)
        {
            try
            {
                if (client?.tcp == null)
                    return;
               
                NetworkStream networkStream = client.tcp.GetStream();

                if (networkStream.DataAvailable)
                {
                     StreamReader streamReader = new StreamReader(networkStream, true);

                     string data = await streamReader.ReadLineAsync();

                     if (data != null)
                     {
                         await _switchMessagesReceived.DoProcessDataAsync(data, client, clients);
                     }
                }
                
            }
            catch (Exception e)
            {
                Console.WriteLine("Message: " + e.Message);
                Console.WriteLine("Stack: " + e.StackTrace);
            }           
        }

        //Metodo Async para enviar y recibir datos de los clientes en el cuarto principal
        private async Task MainRoomAsync()
        {
            while (true)
            {
                if (!_serverStarted)
                    return;
                try
                {
                    List<Task> ioTasks = new List<Task>();
                    for (int i = 0; i < _clients.Count; i++)
                    {
                        ioTasks.Add(ReadAsync(_clients[i]));
                    }
                    await Task.WhenAll(ioTasks);
                }
                catch (Exception e)
                {
                    Console.WriteLine("Message: " + e.Message);
                    Console.WriteLine("Stack: " + e.StackTrace);
                }              
            }
        }
        //Metodo Async para enviar y recibir datos dentro del juego
        private async Task PlayingRoomAsync()
        {
            while (true)
            {
                if (!_serverStarted)
                    return;

                try
                {
                    List<Task> ioTasks = new List<Task>();
                    for (int i = 0; i < _clientsInGame.Count; i++)
                    {
                        for (int c = 0; c < _clientsInGame[i].Count; c++)
                        {
                             ioTasks.Add(ReadAsync(_clientsInGame[i][c], _clientsInGame[i]));
                        }
                    }
                    await Task.WhenAll(ioTasks);
                }
                catch (Exception ex)
                {
                    Console.WriteLine("Stack: " + ex.StackTrace);
                    Console.WriteLine("Message: " + ex.Message);
                }
            }
        }
        //el hilo que crea cada habitacion 
        private async Task RoomThreadAsync()
        {
            _manualReset.Reset();
            await _addPlayersToGameRoom.AddToGame(_clients, _clientsInGame);
            _manualReset.Set();
        }
       
        //cerrar el servidor
        public void CloseServerConexion()
        {
            if (!_serverStarted)
                return;

            _server.Stop();

        }
    }
}
