using Online_Blackjack_Server.Packets;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Timers;



/** TODO:
 * 9. Since cards are sorted by value, it is easy to tell what the hidden card may be for the dealer (Keep as hidden feature)
 * 10. Add a Quick Play Feature instead of having to choose specifically which one
 * 11. Add android action bar functionality.
 * 12. Clean up & Refactor code
 */
namespace Online_Blackjack_Server
{
    // Each game of Blackjack is a session
    // A session will hold the players, handle turns, bets, etc..

    // Starts off by giving everyone two decks, and the dealer gets 2 as well (1 hidden)
    // Anyone at the start who gets a 21 will automaticall get blackjack and win the round
    // Goes through to each person asking for hit or stand (Default : Stand if they take too long)
    // Then simulate the dealer turn
    // Dealer stops hitting at 17 or busts if he goes over 21 
    class Session
    {
        public Blackjack blackjackGame;
        ConcurrentDictionary<int,Client> players; // So if players leave, this will be thread-safe
        public int gameId { get; set; }

        public static int MONEY_FOR_EACH_PLAYER = 100;
        public static int PLACE_BET_TIMER = 16000; // 15 seconds (+1 second grace) are given to players to place a bet otherwise they get kicked from the game
        public static int RESPOND_TIMER = 31000; // 30 (+1 second grace) seconds are given to players to respond with choices
        public static int DELAY_BEFORE_GAME_START = 6000; // 5 (+1 second grace) seconds are given to players before the game starts
        public static int DELAY_BEFORE_GAME_END = 11000; // 10 (+1 second grace) seconds are given to players before the game ends and starts again
        public static int DELAY_TIME_FOR_DEALER = 6000; // 5 (+1 second grace) seconds are given to players before the dealer makes his move

        private static System.Timers.Timer timer;

        bool newGame = true;

        // Solo session -> 4 players
        public Session(params Client[] clients)
        {
            blackjackGame = new Blackjack();
            players = new ConcurrentDictionary<int, Client>();

            int i = 1;
            foreach (Client cl in clients)
            {
                cl.player.playerId = i;
                players.TryAdd(i, cl);
                i++;
            }
        }

        /** Game loop: How it works
         * SetUp(); is called from DelayBeforeGameStart() after a certain duration.
         * -> Requests Bets & waits for players to respond with a bet
         * -> WaitForPlayersToPlaceBet(); Is called
         *      -> CheckIfPlayersBet(); is called after a certain duration (PLACE_BET_TIMER); (Good)
         *          -> StartDealingCards(); Is called  
         *              -> WaitForPlayersToRespond() Is called
         *              -> FinishTakingResponses() is Called after a certain duration (RESPOND_TIMER)
         *                  -> DelayBeforeDealerMove(); is called
         *                      -> DealersTurnAndHandleWinners() is called from DelayBeforeDealerMove(); after a certain duration.
         *                          -> ResetForNextRound() is called
         *                              -> DelayBeforeGameEND() called
         *                                  -> StartGame() Is called if players.count > 0 from DelayBeforeGameEND()
         */
        public void StartGame()
        {
            DelayBeforeGameStart();
        }

        // Initialization of the game
        private void SetUp(object sender, ElapsedEventArgs e)
        {
            if (!newGame)
            {
                SendDealerHand(); 
            }

            // Initialize Deck if not intialized already
            // Add more cards to the deck if less than 32.
            if (blackjackGame.MoreCardsNeeded())
            {
                blackjackGame.Start();
            }
            GiveStartingMoneyToPlayers(newGame);
            newGame = false;
            SendIndividualPlayerData();
            SendAllPlayerInfo();
            RequestBets();

            // Give players 15 seconds to place bets...
            WaitForPlayersToPlaceBet();
        }

        private void StartDealingCards()
        {
            // Deal Cards
            blackjackGame.DealCards(players);

            // Check for any blackjacks at the beginning
            blackjackGame.CheckPlayerHandsAtStart(players);
            SendIndividualPlayerData();
            SendAllPlayerInfo();

            blackjackGame.SetUpDealer();
            SendDealerHand();

            RequestPlayerResponse(); // Players can do multiple responses (Multiple Hits) until they end their turn or bust...
            WaitForPlayersToRespond();
        }

        private void DealersTurnAndHandleWinners(object sender, ElapsedEventArgs e)
        {
            blackjackGame.SimulateDealerTurn();
            SendDealerHand();

            HandleWinners(blackjackGame.CheckWhoWon(players));
            SendIndividualPlayerData();
            SendAllPlayerInfo();
            ResetForNextRound();
        }

        private void ResetForNextRound()
        {
            RemoveBrokePlayers();
            ResetPlayersHands();
            ResetDealer();

            // Check if there are still players in the session, otherwise end it
            if (players.IsEmpty || NumberOfSpectaters() == players.Count)
            {
                EndSession();
                return;
            }
            DelayBeforeGameEnd();
        }

        private void ResetPlayersHands()
        {
            foreach (Client client in players.Values)
            {
                client.player.ResetHand();
                client.player.betPlaced = false;
                client.player.isBust = false;
                client.player.isMyTurn = false;
                client.player.blackjack = false;
                client.player.totalScore = 0;
            }
        }
        private void ResetDealer()
        {
            blackjackGame.dealer.ResetHand();
            blackjackGame.dealer.totalScore = 0;
            blackjackGame.dealer.isBust = false;
            //SendDealerHand();
        }

        private int NumberOfSpectaters()
        {
            int num = 0;
            foreach (Client client in players.Values)
            {
                if (client.player.spectateMode)
                {
                    num++;
                }
            }
            return num;
        }

        private void DelayBeforeDealerMove()
        {
            timer = new System.Timers.Timer(DELAY_TIME_FOR_DEALER);

            timer.Elapsed += new ElapsedEventHandler(DealersTurnAndHandleWinners);
            timer.AutoReset = false; // Only call this method once, then stop the timer
            SendClientDelayTimes(DELAY_TIME_FOR_DEALER / 1000, Packet.DELAY_TIME_FOR_DEALER);
            timer.Start();
        }

        private void DelayBeforeGameStart()
        {
            timer = new System.Timers.Timer(DELAY_BEFORE_GAME_START);

            timer.Elapsed += new ElapsedEventHandler(SetUp);
            timer.AutoReset = false; // Only call this method once, then stop the timer
            SendClientDelayTimes(DELAY_BEFORE_GAME_START/1000, Packet.DELAY_TIME_BEFORE_GAME);
            timer.Start();
        }

        private void DelayBeforeGameEnd()
        {
            timer = new System.Timers.Timer(DELAY_BEFORE_GAME_END);

            timer.Elapsed += new ElapsedEventHandler(SetUp);
            timer.AutoReset = false; // Only call this method once, then stop the timer
            SendClientDelayTimes(DELAY_BEFORE_GAME_END/1000, Packet.DELAY_TIME_AFTER_GAME);
            timer.Start();
        }
        private void SendClientDelayTimes(int time, Packet type)
        {
            foreach (Client client in players.Values)
            {
                client.SendDelayTime(time, type);
            }
        }

        private void WaitForPlayersToPlaceBet()
        {
            timer = new System.Timers.Timer(PLACE_BET_TIMER);

            timer.Elapsed += new ElapsedEventHandler(CheckIfPlayersBet);
            timer.AutoReset = false; // Only call this method once, then stop the timer
            timer.Start();
        }

        // Called from a timer
        private void CheckIfPlayersBet(object sender, ElapsedEventArgs e)
        {
            bool atleastOnePlayerPlacedBet = false;
            foreach (Client client in players.Values)
            {
                if (client.player.betPlaced && !client.player.spectateMode)
                {
                    atleastOnePlayerPlacedBet = true;

                }
                else if (!client.player.spectateMode)
                {
                    TurnPlayerToSpectator(client);
                }
            }

            // If no one placed a bet, then end the session
            if (!atleastOnePlayerPlacedBet)
            {
                EndSession();
                return;
            }

            // TODO: RESEND *ALL* PLAYERS INFO TO EVERYONE
            // Start dealing cards to players
            SendIndividualPlayerData();
            StartDealingCards();
        }

        private void WaitForPlayersToRespond()
        {
            timer = new System.Timers.Timer(RESPOND_TIMER);

            timer.Elapsed += new ElapsedEventHandler(FinishTakingResponses);
            timer.AutoReset = false;
            timer.Start();
        }

        // If a player doesn't send any response (hit/stand), they will automatically stand
        private void FinishTakingResponses(object sender, ElapsedEventArgs e)
        {
            foreach (Client client in players.Values)
            {
                client.player.isMyTurn = false;
                client.SendFinishTakingResponsesPacket();
            }

            DelayBeforeDealerMove();
        }

        private void SendIndividualPlayerData()
        {
            foreach (Client client in players.Values)
            {
                client.player.totalScore = client.player.GetTotalScore();
                client.SendPlayerInfoPacket();
            }
        }

        public void SendAllPlayerInfo()
        {
            List<Player> otherPlayers = new List<Player>();
            foreach (Client client in players.Values)
            {
                foreach (Client other in players.Values)
                {
                    if (client.player.playerId != other.player.playerId)
                    {
                        otherPlayers.Add(other.player);
                    }
                }
                client.SendAllPlayerInfo(otherPlayers);
                otherPlayers.Clear();
            }
        }


        // Sends the dealer's info to each client
        private void SendDealerHand()
        {
            foreach (Client client in players.Values)
            {
                blackjackGame.dealer.totalScore = blackjackGame.dealer.GetTotalHiddenScore();
                client.SendDealerInfoPacket(blackjackGame.dealer);
            }
        }

        // Winners are given their money * 2
        // Losers's bet are set to 0
        private void HandleWinners(ConcurrentDictionary<int, Client> winners)
        {
            foreach (Client client in winners.Values)
            {
                if (client.player.spectateMode)
                {
                    continue;
                }

                client.player.money += client.player.currentBet * 2;
                client.player.currentBet = 0;

                client.SendWonAgainstDealerPacket();
            }
        }

        private void RequestBets()
        {
            foreach (Client client in players.Values)
            {
                client.RequestBet(PLACE_BET_TIMER/1000);
            }
        }

        public void PlaceBet(Client client, int bet)
        {
            if (client.player.spectateMode)
                return;

            if (bet <= client.player.money && bet > 0)
            {
                client.player.money -= bet;
                client.player.currentBet = bet;
                client.player.betPlaced = true;

                client.SendValidBetPacket();
            } else
            {
                client.SendInvalidBetPacket();
            }
        }

        private void RequestPlayerResponse()
        {
            foreach (Client client in players.Values)
            {
                if (!client.player.spectateMode)
                {
                    client.player.isMyTurn = true;
                }

                client.AskForResponse(RESPOND_TIMER / 1000);
                if (client.player.blackjack)
                {
                    client.SendBlackjackPacket();
                    client.player.isMyTurn = false;
                } else if (client.player.isBust)
                {
                    client.SendBustPacket();
                    client.player.isMyTurn = false;
                }
            }
        }

        // Players start with $100 each NEW game session
        private void GiveStartingMoneyToPlayers(bool newGame)
        {
            if (!newGame)
            {
                return;
            }

            foreach (Client client in players.Values)
            {
                if (!client.player.spectateMode)
                {
                    client.player.money = MONEY_FOR_EACH_PLAYER;
                }
            }
        }

        private void RemoveBrokePlayers()
        {
            foreach (Client client in players.Values)
            {
                if (client.player.money == 0 && !client.player.spectateMode)
                {
                    TurnPlayerToSpectator(client); 
                }
            }
        }

        private void EndSession()
        {
            Console.WriteLine($"Ending game session {gameId}");
        }

        private void ResetPlayerData(Client client)
        {
            client.player.isBust = false;
            client.player.isMyTurn = false;
            client.player.totalScore = 0;
            client.player.currentBet = 0;
            client.player.money = 0;
            client.player.blackjack = false;
            client.player.ResetHand();
        }

        public void TurnPlayerToSpectator(Client client)
        {
            Console.WriteLine($"{client.player.username} Has turned to a spectator");
            ResetPlayerData(client);
            client.player.spectateMode = true;
            client.RemovedFromGameSession();
            client.SendPlayerInfoPacket();
            SendAllPlayerInfo();
        }

        private void SendPlayerRemovedPacket(Client clientToRemove)
        {
            foreach (Client client in players.Values)
            {
                if (client != clientToRemove)
                {
                    client.SendRemoveFromClientPacket(clientToRemove.player.playerId);
                }
            }
        }

        public void RemoveFromSession(Client client)
        {
            ResetPlayerData(client);
            SendPlayerRemovedPacket(client);
            client.currentSession = null;
            client.player.spectateMode = false;
            players.TryRemove(client.player.playerId, out _);
            SendAllPlayerInfo();
        }
    }
}
