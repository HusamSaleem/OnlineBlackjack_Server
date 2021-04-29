
using DotNetty.Common.Utilities;
using Online_Blackjack_Server.Packets;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace Online_Blackjack_Server
{
    abstract class MatchmakingService
    {
        static LinkedList<Client> twoPlayerQueue = new LinkedList<Client>();
        static LinkedList<Client> threePlayerQueue = new LinkedList<Client>();
        static LinkedList<Client> fourPlayerQueue = new LinkedList<Client>();


        public static void StartSoloSession(Client client)
        {
            SessionHandler.NewSession(client);
        }

        public static void EnqueuePlayer(Client client, Matchmaking code)
        {
            switch (code)
            {
                case Matchmaking.TWO_PLAYERS:
                    twoPlayerQueue.AddLast(client);
                    break;
                case Matchmaking.THREE_PLAYERS:
                    threePlayerQueue.AddLast(client);
                    break;
                case Matchmaking.FOUR_PLAYERS:
                    fourPlayerQueue.AddLast(client);
                    break;
            }

            TryStartingGames();
        }

        private static void TryStartingGames()
        {
            while (twoPlayerQueue.Count > 0 && twoPlayerQueue.Count % 2 == 0)
            {
                Client client1 = twoPlayerQueue.First();
                twoPlayerQueue.RemoveFirst();
                Client client2 = twoPlayerQueue.First();
                twoPlayerQueue.RemoveFirst();

                // Tell clients that the session started
                client1.SendSessionStartedPacket(Packet.SESSION_STARTED_TWO);
                client2.SendSessionStartedPacket(Packet.SESSION_STARTED_TWO);

                // Start a session
                SessionHandler.NewSession(client1, client2);
            }

            while (threePlayerQueue.Count > 0 && threePlayerQueue.Count % 3 == 0)
            {
                Client client1 = threePlayerQueue.First();
                threePlayerQueue.RemoveFirst();
                Client client2 = threePlayerQueue.First();
                threePlayerQueue.RemoveFirst();
                Client client3 = threePlayerQueue.First();
                threePlayerQueue.RemoveFirst();

                // Tell clients that the session started
                client1.SendSessionStartedPacket(Packet.SESSION_STARTED_THREE);
                client2.SendSessionStartedPacket(Packet.SESSION_STARTED_THREE);
                client3.SendSessionStartedPacket(Packet.SESSION_STARTED_THREE);

                // Start a session
                SessionHandler.NewSession(client1, client2, client3);
            }

            while (fourPlayerQueue.Count > 0 && fourPlayerQueue.Count % 4 == 0)
            {
                Client client1 = fourPlayerQueue.First();
                fourPlayerQueue.RemoveFirst();
                Client client2 = fourPlayerQueue.First();
                fourPlayerQueue.RemoveFirst();
                Client client3 = fourPlayerQueue.First();
                fourPlayerQueue.RemoveFirst();
                Client client4 = fourPlayerQueue.First();
                fourPlayerQueue.RemoveFirst();

                // Tell clients that the session started
                client1.SendSessionStartedPacket(Packet.SESSION_STARTED_FOUR);
                client2.SendSessionStartedPacket(Packet.SESSION_STARTED_FOUR);
                client3.SendSessionStartedPacket(Packet.SESSION_STARTED_FOUR);
                client4.SendSessionStartedPacket(Packet.SESSION_STARTED_FOUR);

                // Start a session
                SessionHandler.NewSession(client1, client2, client3, client4);
            }
        }

        public static void LeaveQueue(Matchmaking type, Client client)
        {
            if (type == Matchmaking.TWO_PLAYERS)
            {
                twoPlayerQueue.Remove(client);
            } else if (type == Matchmaking.THREE_PLAYERS)
            {
                threePlayerQueue.Remove(client);
            } else if (type == Matchmaking.FOUR_PLAYERS)
            {
                fourPlayerQueue.Remove(client);
            }
        }
    }
}
