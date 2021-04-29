using Newtonsoft.Json;
using System.Collections.Generic;

namespace Online_Blackjack_Server
{
    class Player
    {
        public string username { get; set; }
        public int playerId { get; set; }
        public List<Card> currentHand { get; set; }
        public bool isMyTurn { get; set; }
        public bool endTurn { get; set; }
        public bool isBust { get; set; }
        public int currentBet { get; set; }
        public int money { get; set; }
        public bool blackjack { get; set; }
        public bool betPlaced { get; set; }
        public int totalScore { get; set; }
        public bool spectateMode { get; set; }

        [JsonIgnore]
        const int MAX_VAL = 21;

        public Player(string username)
        {
            this.username = username;
            currentHand = new List<Card>();
            isMyTurn = false;
            isBust = false;
            currentBet = 0;
            money = 0;
            blackjack = false;
        }

        public void ResetHand()
        {
            currentHand.Clear();
        }

        // Uses what Ace value is best automatically
        public int GetTotalScore()
        {
            int score = 0;
            currentHand.Sort(); // Want Ace at the end
            foreach (Card c in currentHand)
            {
                if (c.isAce)
                {
                    if (score + c.value > MAX_VAL)
                    {
                        score += 1;
                        continue;
                    }
                }

                score += c.value;
            }

            return score;
        }

    }
}
