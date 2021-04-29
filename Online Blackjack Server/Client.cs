using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Online_Blackjack_Server.Packets;
using System;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace Online_Blackjack_Server
{
    class Client
    {
        public TcpClient tcpClient { get; set; }
        public int uniqueID { get; set; }

        private const int MAX_BYTES = 4096;
        private byte[] receivedBytes = new byte[MAX_BYTES];
        private byte[] sendBytes = new byte[MAX_BYTES];

        public Player player { get; set; }
        public Session currentSession { get; set; }
        Matchmaking currentQueueType; // holds the enum for the current queue if the player is in one
        Packet lastPacketSent;

        public long lastTimePinged { get; set; }
        public int numOfRetries { get; set; } // Counts number of times that server tries to ping the client, but the client doesn't respond


        public Client(TcpClient tcpClient, int uniqueID)
        {
            lastPacketSent = Packet.PING;
            this.player = new Player("");
            this.tcpClient = tcpClient;
            this.uniqueID = uniqueID;

            SetupClient();
        }

        private void SetupClient()
        {
            this.tcpClient.ReceiveBufferSize = MAX_BYTES;
            this.tcpClient.GetStream().BeginRead(receivedBytes, 0, tcpClient.Available, new AsyncCallback(ProcessReceivedData), this.tcpClient.GetStream()); // Starts to wait for any client messages

            // Send the client the current version
            Ping();
            SendVersionInfoPacket();
        }

        // Mighr be a problem here..
        // Client will read when the message has not been finished sending
        private void SendMessage(string message)
        {
            message = message + "~";
            sendBytes = Encoding.UTF8.GetBytes(message);
            //this.tcpClient.GetStream().BeginWrite(sendBytes, 0, sendBytes.Length, new AsyncCallback(this.tcpClient.GetStream().EndWrite), null);
            this.tcpClient.GetStream().Write(sendBytes, 0, sendBytes.Length);
            this.tcpClient.GetStream().Flush();
        }

        private void ProcessReceivedData(IAsyncResult result)
        {
            NetworkStream stream = (NetworkStream)result.AsyncState;

            int byteCount = -1;
            StringBuilder messageData = new StringBuilder();

            try
            {
                byteCount = stream.EndRead(result);

                // Use Decoder class to convert from bytes to UTF8
                // in case a character spans two buffers.
                Decoder decoder = Encoding.UTF8.GetDecoder();
                char[] chars = new char[decoder.GetCharCount(receivedBytes, 0, byteCount)];
                decoder.GetChars(receivedBytes, 0, byteCount, chars, 0);

                messageData.Append(chars);
                string[] allMsgData = messageData.ToString().Split("~", StringSplitOptions.RemoveEmptyEntries);

                foreach (string data in allMsgData)
                {
                    Console.WriteLine($"Message recieved from client {uniqueID}: {data}");
                    HandleData(data);
                }
                lastTimePinged = GetCurrentMilli();

                this.tcpClient.GetStream().BeginRead(receivedBytes, 0, tcpClient.Available, new AsyncCallback(ProcessReceivedData), stream);
            }
            catch (Exception e)
            {

            }

        }

        public static long GetCurrentMilli()
        {
            return DateTime.Now.Ticks / 10000;
        }

        private void HandleData(string data)
        {
            JObject jsonObj = JObject.Parse(data);

            ushort packetType = (ushort)jsonObj.GetValue("Packet");
            switch (packetType)
            {
                case (ushort)Packet.PING:
                    break;
                case (ushort)Packet.REGISTER_USERNAME:
                    this.player.username = JsonConvert.DeserializeObject<RegisterUsernamePacket>(data).username;
                    SendUsernameSuccessPacket(); // Tells the client that username is good.
                    break;
                case (ushort)Packet.JOIN_SOLO_SESSION:
                    SendSessionStartedPacket(Packet.SESSION_STARTED_SOLO);
                    MatchmakingService.StartSoloSession(this);
                    break;
                case (ushort)Packet.JOIN_TWO_PLAYER_QUEUE:
                    MatchmakingService.EnqueuePlayer(this, Matchmaking.TWO_PLAYERS);
                    currentQueueType = Matchmaking.TWO_PLAYERS;
                    break;
                case (ushort)Packet.JOIN_THREE_PLAYER_QUEUE:
                    MatchmakingService.EnqueuePlayer(this, Matchmaking.THREE_PLAYERS);
                    currentQueueType = Matchmaking.THREE_PLAYERS;
                    break;
                case (ushort)Packet.JOIN_FOUR_PLAYER_QUEUE:
                    MatchmakingService.EnqueuePlayer(this, Matchmaking.FOUR_PLAYERS);
                    currentQueueType = Matchmaking.FOUR_PLAYERS;
                    break;
                case (ushort)Packet.REQUEST_PLAYER_INFO:
                    SendPlayerInfoPacket();
                    break;
                case (ushort)Packet.LEAVE_SESSION:
                    if (currentSession != null)
                    {
                        currentSession.RemoveFromSession(this);
                    }
                    break;
                case (ushort)Packet.REQUEST_LOBBY_INFO:
                    SendLobbyInfo();
                    SendPlayerInfoPacket();
                    break;
                case (ushort)Packet.LEAVE_MATCHMAKING:
                    MatchmakingService.LeaveQueue(currentQueueType, this);
                    currentQueueType = Matchmaking.NONE;
                    break;
                case (ushort)Packet.PLACE_BET:
                    if (currentSession != null)
                    {
                        PlaceBetPacket placeBetPacket = JsonConvert.DeserializeObject<PlaceBetPacket>(jsonObj.ToString());
                        currentSession.PlaceBet(this, placeBetPacket.betAmt);
                    }
                    break;
                case (ushort)Packet.HIT:
                    if (currentSession != null && !player.spectateMode)
                    {
                        currentSession.blackjackGame.Hit(this);
                        SendPlayerInfoPacket();
                        currentSession.SendAllPlayerInfo(); // Sends info to all other players wheb a player hits.
                    }
                    break;
                case (ushort)Packet.STAND:
                    if (currentSession != null && !player.spectateMode)
                    {
                        currentSession.blackjackGame.Stand(this);
                    }
                    break;
                case (ushort)Packet.RESEND_LAST_PACKET:
                    ResendLastPacket();
                    break;
                default:
                    Console.WriteLine("Invalid packet received from client");
                    break;
            }
        }

        public void SendRemoveFromClientPacket(int id)
        {
            JObject jsonObj = new JObject();
            jsonObj.Add("Packet", (int)Packet.REMOVE_PLAYER_FROM_CLIENT);
            jsonObj.Add("playerId", id);
            var t = Task.Run(() => SendMessage(jsonObj.ToString()));
        }

        public void SendWonAgainstDealerPacket()
        {
            JObject jsonObj = new JObject();
            jsonObj.Add("Packet", (int)Packet.BEAT_THE_DEALER);
            lastPacketSent = Packet.TIE_WITH_DEALER;
            var t = Task.Run(() => SendMessage(jsonObj.ToString()));
        }

        public void SendTieAgainstDealerPacket()
        {
            JObject jsonObj = new JObject();
            jsonObj.Add("Packet", (int)Packet.TIE_WITH_DEALER);
            lastPacketSent = Packet.TIE_WITH_DEALER;
            var t = Task.Run(() => SendMessage(jsonObj.ToString()));
        }

        public void SendLostAgainstDealerPacket()
        {
            JObject jsonObj = new JObject();
            jsonObj.Add("Packet", (int)Packet.LOST_AGAINST_DEALER);
            lastPacketSent = Packet.LOST_AGAINST_DEALER;
            var t = Task.Run(() => SendMessage(jsonObj.ToString()));
        }

        public void SendNoMoreResponsesPacket()
        {
            JObject jsonObj = new JObject();
            jsonObj.Add("Packet", (int)Packet.NO_MORE_RESPONSES);
            lastPacketSent = Packet.NO_MORE_RESPONSES;
            var t = Task.Run(() => SendMessage(jsonObj.ToString()));
        }

        public void SendFinishTakingResponsesPacket()
        {
            JObject jsonObj = new JObject();
            jsonObj.Add("Packet", (int)Packet.FINISH_TAKING_RESPONSES);
            lastPacketSent = Packet.FINISH_TAKING_RESPONSES;
            var t = Task.Run(() => SendMessage(jsonObj.ToString()));
        }

        public void SendSessionStartedPacket(Packet type)
        {
            JObject jsonObj = new JObject();
            jsonObj.Add("Packet", (int)type);
            lastPacketSent = type;
            var t = Task.Run(() => SendMessage(jsonObj.ToString()));
        }

        public void SendLobbyInfo()
        {
            LobbyInfoPacket lobbyInfoPacket = new LobbyInfoPacket();
            lobbyInfoPacket.playersConnected = Server.GetTotalConnectedPlayers();

            JObject jsonObj = JObject.Parse(JsonConvert.SerializeObject(lobbyInfoPacket));
            jsonObj.Add("Packet", (int)Packet.LOBBY_INFO);
            lastPacketSent = Packet.LOBBY_INFO;
            var t = Task.Run(() => SendMessage(jsonObj.ToString()));
        }

        private void SendVersionInfoPacket()
        {
            JObject jsonObj = new JObject();
            jsonObj.Add("Packet", (int)Packet.VERSION_INFO);
            jsonObj.Add("version", Server.versionId);
            lastPacketSent = Packet.VERSION_INFO;
            var t = Task.Run(() => SendMessage(jsonObj.ToString()));
        }

        private void SendUsernameSuccessPacket()
        {
            JObject jObject = new JObject();
            jObject.Add("Packet", (int)Packet.USERNAME_REGISTERED);
            lastPacketSent = Packet.USERNAME_REGISTERED;
            var t = Task.Run(() => SendMessage(jObject.ToString()));
        }

        public void SendDealerInfoPacket(Dealer dealer)
        {
            string dealerJson = JsonConvert.SerializeObject(dealer);
            JObject jObject = JObject.Parse(dealerJson);
            jObject.Add("Packet", (int)Packet.DEALER_INFO);
            lastPacketSent = Packet.DEALER_INFO;
            var t = Task.Run(() => SendMessage(jObject.ToString()));
        }

        public void SendPlayerInfoPacket()
        {
            string playerJson = JsonConvert.SerializeObject(this.player);
            JObject jObject = JObject.Parse(playerJson);
            jObject.Add("Packet", (int)Packet.PLAYER_INFO);
            lastPacketSent = Packet.PLAYER_INFO;
            var t = Task.Run(() => SendMessage(jObject.ToString()));
        }

        public void SendBustPacket()
        {
            JObject jObject = new JObject();
            jObject.Add("Packet", (int)Packet.BUST);
            lastPacketSent = Packet.BUST;
            var t = Task.Run(() => SendMessage(jObject.ToString()));
        }

        public void SendBlackjackPacket()
        {
            JObject jObject = new JObject();
            jObject.Add("Packet", (int)Packet.BLACKJACK);
            lastPacketSent = Packet.BLACKJACK;
            var t = Task.Run(() => SendMessage(jObject.ToString()));
        }

        public void SendValidBetPacket()
        {
            JObject jObject = new JObject();
            jObject.Add("Packet", (int)Packet.VALID_BET);
            lastPacketSent = Packet.VALID_BET;
            var t = Task.Run(() => SendMessage(jObject.ToString()));
        }

        public void SendInvalidBetPacket()
        {
            JObject jObject = new JObject();
            jObject.Add("Packet", (int)Packet.INVALID_BET);
            lastPacketSent = Packet.INVALID_BET;
            var t = Task.Run(() => SendMessage(jObject.ToString()));
        }

        // Asks the client if they want to Hit or Stand
        public void AskForResponse(int time)
        {
            TimeLeftPacket timeLeftPacket = new TimeLeftPacket();
            timeLeftPacket.timeLeft = time;
            string timeJson = JsonConvert.SerializeObject(timeLeftPacket);
            JObject jObject = JObject.Parse(timeJson);
            jObject.Add("Packet", (int)Packet.REQUEST_RESPONSE);
            lastPacketSent = Packet.REQUEST_RESPONSE;
            var t = Task.Run(() => SendMessage(jObject.ToString()));
        }

        // Removes the client from the game session
        public void RemovedFromGameSession()
        {
            JObject jObject = new JObject();
            jObject.Add("Packet", (int)Packet.REMOVE_FROM_GAME_SESSION);
            lastPacketSent = Packet.REMOVE_FROM_GAME_SESSION;
            var t = Task.Run(() => SendMessage(jObject.ToString()));
        }

        // Requests a bet from the player
        public void RequestBet(int time)
        {
            TimeLeftPacket timeLeftPacket = new TimeLeftPacket();
            timeLeftPacket.timeLeft = time;
            string timeJson = JsonConvert.SerializeObject(timeLeftPacket);
            JObject jObject = JObject.Parse(timeJson);
            jObject.Add("Packet", (int)Packet.REQUEST_BET);
            lastPacketSent = Packet.REQUEST_BET;
            var t = Task.Run(() => SendMessage(jObject.ToString()));
        }

        public void SendDelayTime(int time, Packet type)
        {
            TimeLeftPacket timeLeftPacket = new TimeLeftPacket();
            timeLeftPacket.timeLeft = time;
            string timeJson = JsonConvert.SerializeObject(timeLeftPacket);
            JObject jObject = JObject.Parse(timeJson);
            jObject.Add("Packet", (int)type);
            lastPacketSent = type;
            var t = Task.Run(() => SendMessage(jObject.ToString()));
        }

        public void SendAllPlayerInfo(List<Player> otherPlayers)
        {
            foreach (Player player in otherPlayers)
            {
                string otherPlayersJson = JsonConvert.SerializeObject(player);
                JObject jObject = JObject.Parse(otherPlayersJson);
                jObject.Add("Packet", (int)Packet.OTHER_PLAYER_INFO);
                lastPacketSent = Packet.OTHER_PLAYER_INFO;
                var t = Task.Run(() => SendMessage(jObject.ToString()));
            }
        }

        // Pings the client
        public void Ping()
        {
            JObject jObject = new JObject();
            jObject.Add("Packet", (int)Packet.PING);
            lastPacketSent = Packet.PING;
            var t = Task.Run(() => SendMessage(jObject.ToString()));
        }

        private void ResendLastPacket()
        {
            if (lastPacketSent == Packet.PLAYER_INFO)
            {
                SendPlayerInfoPacket();
            }
            else if (lastPacketSent == Packet.OTHER_PLAYER_INFO)
            {
                if (currentSession != null)
                {
                    currentSession.SendAllPlayerInfo();
                }
                SendPlayerInfoPacket();
            }
            else if (lastPacketSent == Packet.LOBBY_INFO)
            {
                SendLobbyInfo();
            }
            else if (lastPacketSent == Packet.BUST)
            {
                SendBustPacket();
            }
            else if (lastPacketSent == Packet.BLACKJACK)
            {
                SendBlackjackPacket();
            }
            else if (lastPacketSent == Packet.BEAT_THE_DEALER)
            {
                SendWonAgainstDealerPacket();
            }
            else if (lastPacketSent == Packet.TIE_WITH_DEALER)
            {
                SendTieAgainstDealerPacket();
            }
            else if (lastPacketSent == Packet.LOST_AGAINST_DEALER)
            {
                SendLostAgainstDealerPacket();
            }
            else if (lastPacketSent == Packet.DELAY_TIME_BEFORE_GAME)
            {
                SendDelayTime(Session.DELAY_BEFORE_GAME_START / 1000, Packet.DELAY_TIME_BEFORE_GAME);
            }
            else if (lastPacketSent == Packet.DELAY_TIME_AFTER_GAME)
            {
                SendDelayTime(Session.DELAY_BEFORE_GAME_END / 1000, Packet.DELAY_TIME_AFTER_GAME);
            }
            else if (lastPacketSent == Packet.DELAY_TIME_FOR_DEALER)
            {
                SendDelayTime(Session.DELAY_TIME_FOR_DEALER / 1000, Packet.DELAY_TIME_FOR_DEALER);
            }
            else if (lastPacketSent == Packet.REQUEST_BET)
            {
                RequestBet(Session.PLACE_BET_TIMER);
            }
            else if (lastPacketSent == Packet.REQUEST_RESPONSE)
            {
                AskForResponse(Session.RESPOND_TIMER / 1000);
            }
            else if (lastPacketSent == Packet.FINISH_TAKING_RESPONSES)
            {
                SendFinishTakingResponsesPacket();
            }
            else if (lastPacketSent == Packet.NO_MORE_RESPONSES)
            {
                SendNoMoreResponsesPacket();
            }
        }
    }
}