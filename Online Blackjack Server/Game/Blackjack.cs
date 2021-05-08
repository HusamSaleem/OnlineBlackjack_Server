using Online_Blackjack_Server.Game;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;

namespace Online_Blackjack_Server
{
    // The actual Blackjack Game implementation goes here
    // Normally played with 52 card deck or *312 in a casino*
    // Will use a 312 card deck in this case...
    class Blackjack
    {
        const int NUM_OF_DECKS = 4;
        const int NUM_OF_TIMES_TO_SHUFFLE = 3; // Shuffles the deck 3 times, just so there is more randomness
        const int BLACKJACK_MAX = 21;
        const int DEALER_GOAL = 17; // Dealer tries to hit until the cards are at 17

        List<Card> deck; // Contains all cards and shuffled for the game

        public Dealer dealer;

        public Blackjack()
        {
            deck = new List<Card>();
        }

        public void Start()
        {
            CreateDeck();
        }

        // Generates and shuffles the deck
        private void CreateDeck()
        {
            for (int i = 0; i < NUM_OF_DECKS; i++)
            {
                deck.AddRange(deck.GenerateDeck().ToList());
            }

            // Add a unique id and make sure ace cards are marked as ace cards
            Random rng = new Random();
            deck.ForEach(card =>
            {
                if (card.cardId.Contains("Ace"))
                {
                    card.isAce = true;
                }

                card.uniqueId = rng.Next(-9999, 9999);
            });

            for (int i = 0; i < NUM_OF_TIMES_TO_SHUFFLE; i++)
            {
                deck.Shuffle();
            }
        }


        // Gets and deletes the card from the deck
        private Card GetCard()
        {
            Card card = deck[deck.Count - 1];
            deck.RemoveAt(deck.Count - 1);
            return card;
        }

        // Dealer AI
        // Each player will start with two cards
        public void DealCards(ConcurrentDictionary<int, Client> players)
        {
            foreach (Client client in players.Values)
            {
                if (!client.player.spectateMode)
                {
                    client.player.currentHand.Add(GetCard());
                    client.player.currentHand.Add(GetCard());
                }
            }
        }

        // One of the cards of the dealer is hidden until the end
        // BUG: If two cards are the same, then hiding one will hide both...
        public void SetUpDealer()
        {
            dealer = new Dealer();

            Card card1 = GetCard();
            Card card2 = GetCard();

            card2.hidden = true;
            dealer.currentHand.Add(card1);
            dealer.currentHand.Add(card2);
        }

        public void Hit(Client client)
        {
            if (HandleHitCases(client))
            {
                return;
            }

            if (client.player.isMyTurn)
            {
                client.player.currentHand.Add(GetCard());
                client.player.totalScore = client.player.GetTotalScore();
                client.player.isBust = client.player.totalScore > BLACKJACK_MAX;
                client.player.blackjack = client.player.GetTotalScore() == BLACKJACK_MAX;

                HandleHitCases(client);
            }
        }

        // Ends the turn
        public void Stand(Client client)
        {
            client.player.isMyTurn = false;
        }

        // Sends a bust or blackjack packet to player 
        private bool HandleHitCases(Client client)
        {
            if (client.player.blackjack)
            {
                client.SendBlackjackPacket();
                return true;
            }
            else if (client.player.isBust)
            {
                client.SendBustPacket();
                return true;
            }

            return false;
        }

        public void SimulateDealerTurn()
        {
            dealer.UnhideCards();
            while (dealer.GetTotalScore() < DEALER_GOAL)
            {
                dealer.currentHand.Add(GetCard());
                dealer.totalScore = dealer.GetTotalScore();
                dealer.isBust = dealer.totalScore > BLACKJACK_MAX;
            }
        }

        // Returns a list of players who won
        // Also handles the players who lost, and removes their bets.
        public ConcurrentDictionary<int, Client> CheckWhoWon(ConcurrentDictionary<int, Client> players)
        {
            ConcurrentDictionary<int, Client> winners = new ConcurrentDictionary<int, Client>();

            foreach (Client client in players.Values)
            {
                if (client.player.spectateMode)
                {
                    continue;
                }

                if (client.player.isBust)
                {
                    client.player.currentBet = 0;
                    client.SendLostAgainstDealerPacket();
                    continue;
                }

                // Case 0: Dealer is bust, player isnt bust
                if (dealer.isBust)
                {
                    winners.TryAdd(client.player.playerId, client);
                    continue;
                }

                // Case 1: Tie
                // Money is given back to player
                if (client.player.GetTotalScore() == dealer.GetTotalScore())
                {
                    client.player.money += client.player.currentBet;
                    client.player.currentBet = 0;
                    client.SendTieAgainstDealerPacket();
                    continue;
                }

                // Case 2: Player score > dealer score
                if (client.player.GetTotalScore() > dealer.GetTotalScore())
                {
                    winners.TryAdd(client.player.playerId, client);
                    continue;
                }

                // Case 3: Player Score < Dealer score
                if (client.player.GetTotalScore() < dealer.GetTotalScore())
                {
                    client.player.currentBet = 0;
                    client.SendLostAgainstDealerPacket();
                    continue;
                }
            }

            return winners;
        }

        // When there are only 32 cards left, return true;
        public bool MoreCardsNeeded()
        {
            return this.deck.Count <= 32;
        }

        // Checks the players hands at the start of the game
        // An Ace + Face = blackjack winner
        public void CheckPlayerHandsAtStart(ConcurrentDictionary<int, Client> players)
        {
            foreach (Client client in players.Values)
            {
                if (client.player.GetTotalScore() == BLACKJACK_MAX)
                {
                    client.player.blackjack = true;
                }
            }
        }
    }
}
