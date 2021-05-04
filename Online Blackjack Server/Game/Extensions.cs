using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;

namespace Online_Blackjack_Server.Game
{
    public static class Extensions
    {
        static IEnumerable<string> Suits()
        {
            yield return "Spades";
            yield return "Hearts";
            yield return "Clubs";
            yield return "Diamonds";
        }

        static IEnumerable<string> Ranks()
        {
            yield return "Two";
            yield return "Three";
            yield return "Four";
            yield return "Five";
            yield return "Six";
            yield return "Seven";
            yield return "Eight";
            yield return "Nine";
            yield return "Ten";
            yield return "Jack";
            yield return "Queen";
            yield return "King";
            yield return "Ace";
        }

        static IEnumerable<int> Values()
        {
            yield return 2;
            yield return 3;
            yield return 4;
            yield return 5;
            yield return 6;
            yield return 7;
            yield return 8;
            yield return 9;
            yield return 10;
            yield return 10;
            yield return 10;
            yield return 10;
            yield return 11;
        }

        public static IList<T> GenerateDeck<T>(this IList<T> deck)
        {
            return (IList<T>)Suits().SelectMany(suit => Ranks().Zip(Values()).Select(rank => new Card(rank.First + " of " + suit, rank.Second, false))).ToList();
        }

        public static IList<T> Shuffle<T>(this IList<T> deck)
        {
            int n = deck.Count;
            Random rng = new Random();
            while (n > 1)
            {
                n--;
                int k = rng.Next(n + 1);
                T temp = deck[k];
                deck[k] = deck[n];
                deck[n] = temp;
            }

            return deck;
        }
    }
}
