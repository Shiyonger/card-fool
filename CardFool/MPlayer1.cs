using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CardFool
{
    internal class MPlayer1
    {
        private string name = "Alex";
        private List<SCard> hand = new List<SCard>(); // карты на руке

        private enum LocalConsts
        {
            EarlyGameDeck = 13,
            MiddleGameDeck = 5,
            EarlyGameTrumps = 10,
            MiddleGameTrumps = 12,
            ALotOfTrumps = 3,
            DeckMax = 36,
            HandMax = 6
        }

        private int trumps = 0;
        private int enemyHand = (int)LocalConsts.HandMax;
        private int deck = (int)LocalConsts.DeckMax - (int)LocalConsts.HandMax;
        private bool defended = true;
        private bool first = true;

        // Возвращает имя игрока
        public string GetName()
        {
            return name;
        }

        // количество карт на руке
        public int GetCount()
        {
            return hand.Count;
        }

        // Добавляет новую карту в руку
        public void AddToHand(SCard card)
        {
            if (defended) deck--;
            else
            {
                enemyHand--;
                if (deck > 0 && enemyHand < (int)LocalConsts.HandMax)
                {
                    deck--;
                    enemyHand++;
                }
            }

            if (card.Suit == MTable.GetTrump().Suit) trumps++;
            for (int index = 0; index < hand.Count; index++)
                if ((card.Suit != MTable.GetTrump().Suit && card.Rank <= hand[index].Rank) ||
                    (card.Suit == MTable.GetTrump().Suit && hand[index].Suit == MTable.GetTrump().Suit &&
                     card.Rank <= hand[index].Rank) ||
                    (card.Suit != MTable.GetTrump().Suit && hand[index].Suit == MTable.GetTrump().Suit))
                {
                    hand.Insert(index, card);
                    return;
                }

            hand.Add(card);
        }

        // Сделать ход (первый)
        public List<SCard> LayCards()
        {
            if (first)
            {
                SortHand();
                first = false;
            }
            defended = true;
            if (hand.Count == 1)
            {
                SCard t = hand[0];
                hand.RemoveAt(0);
                return new List<SCard>() { t };
            }

            List<SCard> lay = new List<SCard>();
            int index = 0;

            if (trumps < (int)LocalConsts.ALotOfTrumps && deck > (int)LocalConsts.MiddleGameDeck)
            {
                index = hand.Count - trumps - 1;
                lay.Add(hand[Math.Max(index--, 0)]);
                while (enemyHand - lay.Count > 0 && index >= 0 && hand[index].Rank == lay[0].Rank)
                    lay.Add(hand[index--]);
                hand.RemoveRange(Math.Max(index + 1, 0), lay.Count);
                return lay;
            }

            index = GetLayRange(out int groupLength);
            if (hand[index].Suit == MTable.GetTrump().Suit) trumps--;
            lay.AddRange(hand.Slice(index, Math.Min(groupLength, enemyHand)));
            hand.RemoveRange(index, Math.Min(groupLength, enemyHand));
            return lay;
        }

        private int GetLayRange(out int groupLength)
        {
            int minGroups = Int32.MaxValue, maxGroups = 1, indexMax = 0, indexMin = 0, i = 0;
            while (i < hand.Count - trumps - 1)
            {
                int c = 1;
                while (i < hand.Count - trumps - 1 && hand[i].Rank == hand[i + 1].Rank)
                {
                    c++;
                    i++;
                }

                i++;
                if (c < minGroups && c <= enemyHand)
                {
                    minGroups = c;
                    indexMin = i - c;
                }

                if (c >= maxGroups && c <= enemyHand)
                {
                    maxGroups = c;
                    indexMax = i - c;
                }
            }

            if (deck <= (int)LocalConsts.MiddleGameDeck)
            {
                groupLength = maxGroups;
                return indexMax;
            }

            groupLength = minGroups;
            if (minGroups == Int32.MaxValue) groupLength = 1;
            return indexMin;
        }

        // Отбиться.
        // На вход подается набор карт на столе, часть из них могут быть уже покрыты
        public bool Defend(List<SCardPair> table)
        {
            if (first)
            {
                SortHand();
                first = false;
            }
            defended = true;
            if (!CheckCanDefend(table, out int countCards))
            {
                defended = false;
                enemyHand += 2 * (table.Count - countCards);
                return false;
            }

            for (int i = 0; i < table.Count; i++)
            {
                int j = 0;
                while (!table[i].Beaten && j < hand.Count)
                {
                    if (CheckCardSetUp(table[i].Down, hand[j]))
                    {
                        SCardPair t = table[i];
                        t.SetUp(hand[j], MTable.GetTrump().Suit);
                        table[i] = t;
                        hand.RemoveAt(j);
                    }

                    j++;
                }
            }

            enemyHand -= table.Count;
            while (deck > 0 && enemyHand < (int)LocalConsts.HandMax)
            {
                enemyHand++;
                deck--;
            }

            return true;
        }

        private bool CheckCanDefend(List<SCardPair> table, out int countCards)
        {
            HashSet<SCard> used = new HashSet<SCard>();
            countCards = 0;
            bool ans = true;
            for (int i = 0; i < table.Count; i++)
            {
                if (!table[i].Beaten)
                {
                    Console.WriteLine(table[i].Down.Rank + " " + table[i].Down.Suit);
                    countCards++;
                }

                int j = 0;
                bool beatable = false;
                while (!table[i].Beaten && j < hand.Count)
                {
                    if (CheckCardSetUp(table[i].Down, hand[j]) && !used.Contains(hand[j]))
                    {
                        beatable = true;
                        used.Add(hand[j]);
                        break;
                    }

                    j++;
                }

                if (!beatable) ans = false;
            }

            return ans;
        }

        private bool CheckCardSetUp(SCard down, SCard up)
        {
            if (deck > (int)LocalConsts.EarlyGameDeck &&
                up.Suit == MTable.GetTrump().Suit && up.Rank > (int)LocalConsts.EarlyGameTrumps) return false;
            if (deck > (int)LocalConsts.MiddleGameDeck &&
                up.Suit == MTable.GetTrump().Suit && up.Rank > (int)LocalConsts.MiddleGameTrumps) return false;
            if (up.Suit == down.Suit)
            {
                if (up.Rank > down.Rank) return true;
                return false;
            }
            if (up.Suit == MTable.GetTrump().Suit) return true;
            return false;
        }

        // Подбросить карты
        // На вход подаются карты на столе
        public bool AddCards(List<SCardPair> table)
        {
            if (enemyHand == 0) return false;
            HashSet<int> ranks = new HashSet<int>();
            bool defeated = false;
            int numberOfCards = table.Count;
            for (int i = 0; i < table.Count; i++)
            {
                ranks.Add(table[i].Down.Rank);
                if (!table[i].Beaten) defeated = true;
                else
                {
                    ranks.Add(table[i].Up.Rank);
                    numberOfCards++;
                }
            }

            bool ans = AddToTable(table, ranks);
            if (defeated) enemyHand += table.Count;
            if (!ans)
            {
                if (!defeated) enemyHand -= (numberOfCards - table.Count);
                int e = Math.Max(0, 6 - enemyHand);
                int m = Math.Max(0, 6 - hand.Count);
                if (deck > m)
                {
                    enemyHand += Math.Min(deck - m, e);
                    deck -= Math.Min(deck - m, e);
                }
            }

            return ans;
        }

        private bool AddToTable(List<SCardPair> table, HashSet<int> ranks)
        {
            bool ans = false;
            List<List<SCardPair>> groups = new List<List<SCardPair>>();
            for (int i = 0; i < 15; i++)
                groups.Add(new List<SCardPair>());
            for (int i = 0; i < hand.Count - trumps; i++)
                groups[hand[i].Rank].Add(new SCardPair(hand[i]));

            for (int i = 6; i < groups.Count; i++)
            {
                if (ranks.Contains(i) && enemyHand - table.Count >= groups[i].Count && table.Count+groups[i].Count <= 6)
                {
                    for (int j = 0; j < groups[i].Count; j++)
                    {
                        table.Add(groups[i][j]);
                        hand.Remove(groups[i][j].Down);
                        ans = true;
                    }
                }
            }

            if (deck <= (int)LocalConsts.MiddleGameDeck)
                for (int i = 0; i < hand.Count; i++)
                    if (hand[i].Suit == MTable.GetTrump().Suit && ranks.Contains(hand[i].Rank) &&
                        enemyHand - table.Count > 0 && table.Count < 6)
                    {
                        table.Add(new SCardPair(hand[i]));
                        hand.RemoveAt(i);
                        ans = true;
                        trumps--;
                    }

            return ans;
        }

        private void SortHand()
        {
            trumps = 0;
            for (int i = 0; i < hand.Count; i++)
            {
                if (hand[i].Suit == MTable.GetTrump().Suit) trumps++;
            }
            SCard trump = MTable.GetTrump();
            SCard tempCard;
            for (int i = 1; i < hand.Count; i++)
            {
                if (hand[i].Suit == trump.Suit && hand[i - 1].Suit != trump.Suit)
                    continue;
                int tempInd = i;
                for (int j = i - 1; j >= 0; j--)
                {
                    if (hand[tempInd].Suit != trump.Suit && hand[tempInd].Rank < hand[j].Rank)
                    {
                        tempCard = hand[tempInd];
                        hand[tempInd--] = hand[j];
                        hand[j] = tempCard;
                    }
                    else if (hand[tempInd].Suit == trump.Suit && hand[j].Suit == trump.Suit
                                                              && hand[j].Rank > hand[tempInd].Rank)
                    {
                        tempCard = hand[tempInd];
                        hand[tempInd--] = hand[j];
                        hand[j] = tempCard;
                    }
                    else if (hand[tempInd].Suit != trump.Suit && hand[j].Suit == trump.Suit)
                    {
                        tempCard = hand[tempInd];
                        hand[tempInd--] = hand[j];
                        hand[j] = tempCard;
                    }
                    else
                        break;
                }
            }
        }

        // Вывести в консоль карты на руке
        public void ShowHand()
        {
            Console.WriteLine("Hand " + name);
            foreach (SCard card in hand)
            {
                MTable.ShowCard(card);
                Console.Write(MTable.Separator);
            }

            Console.WriteLine();
        }
    }
}