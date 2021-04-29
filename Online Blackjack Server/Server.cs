using System;
using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;

namespace Online_Blackjack_Server
{
    class Server
    {
        TcpListener listener;
        private static ConcurrentDictionary<int, Client> activeClients; // Keeping it thread safe

        public static string versionId = "1.0A";

        int currentId = 0; // Every connected client increases this by 1. POTENTIAL ERROR: When number of connected clients reach the maximum integer limit (2 billion something)

        public async Task Start()
        {
            activeClients = new ConcurrentDictionary<int, Client>();
            listener = new TcpListener(IPAddress.Any, 6479);
            listener.Start();

            ClientPingHandler clientPingHandler = new ClientPingHandler();
            clientPingHandler.Start();

            Console.WriteLine("Server started!");
            Console.WriteLine($"Listening on port {6479}");

            while (true)
            {
                TcpClient client = await listener.AcceptTcpClientAsync();
                OnClientConnect(client);
            }
        }

        // Makes sure that the client has a certificate
        private void OnClientConnect(TcpClient client)
        {
            Console.WriteLine("Client Connected!");
            activeClients.TryAdd(currentId, new Client(client, currentId));
            currentId++;
        }

        public static int GetTotalConnectedPlayers()
        {
            return activeClients.Count;
        }

        public static ConcurrentDictionary<int, Client> GetActiveClients()
        {
            return activeClients;
        }

        public static void RemoveClient(int id)
        {
            activeClients.TryRemove(id, out _);
        }
    }
}
