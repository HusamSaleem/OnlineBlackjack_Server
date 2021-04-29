using System;

namespace Online_Blackjack_Server
{
    class Card : IComparable
    {
        public bool hidden { get; set; }
        public int value { get; set; }
        public string cardId { get; set; }

        public bool isAce { get; set; }// Default value = 11, otherwise its a 1;  T: 11 F: 1

        public Card(string cardId, int value, bool hidden)
        {
            this.cardId = cardId;
            this.value = value;
            this.hidden = hidden;
            this.isAce = false;
        }

        public int CompareTo(object obj)
        {
            Card card = (Card)obj;
            return this.value.CompareTo(card.value);
        }
    }
}
