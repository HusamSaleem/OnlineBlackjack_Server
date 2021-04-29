using Newtonsoft.Json;
using System.Collections.Generic;

namespace Online_Blackjack_Server
{
    class Dealer
    {
        public List<Card> currentHand { get; set; }
        public bool isBust { get; set; }
        public int totalScore { get; set; }

        [JsonIgnore]
        const int MAX_VAL = 21;
        public Dealer()
        {
            currentHand = new List<Card>();
            isBust = false;
        }

        public void ResetHand()
        {
            currentHand.Clear();
        }

        public void UnhideCards()
        {
            foreach (Card card in currentHand)
            {
                card.hidden = false;
            }
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

        // Uses what Ace value is best automatically
        // Doesn't include the hidden value cards
        public int GetTotalHiddenScore()
        {
            int score = 0;
            currentHand.Sort(); // Want Ace at the end
            foreach (Card c in currentHand)
            {
                if (c.hidden)
                {
                    continue;
                }

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
