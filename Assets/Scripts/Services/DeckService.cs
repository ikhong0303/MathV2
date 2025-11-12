using System.Collections.Generic;
using UnityEngine;
using MathHighLow.Models;  // ✅ 추가됨!

namespace MathHighLow.Services
{
    /// 카드 덱을 관리하는 서비스입니다.
    /// 게임에서 사용할 카드를 생성하고 섞고 분배합니다.
    /// </summary>
    public class DeckService
    {
        private readonly GameConfig config;
        private readonly List<Card> slotDeck;
        private System.Random random;

        /// <summary>
        /// 생성자
        /// </summary>
        public DeckService(GameConfig config, int? seed = null)
        {
            this.config = config;
            this.slotDeck = new List<Card>();
            this.random = seed.HasValue ? new System.Random(seed.Value) : new System.Random();
        }

        /// <summary>
        /// 슬롯 덱을 새로 구성합니다.
        /// 
        /// [학습 포인트] 덱 구성 알고리즘
        /// 1. 숫자 카드 0~10 각 4장씩 추가
        /// 2. 특수 카드(×, √) 추가
        /// 3. 전체 셔플
        /// </summary>
        public void BuildSlotDeck()
        {
            slotDeck.Clear();

            // 숫자 카드 추가 (0~10, 각 4장)
            for (int number = 0; number <= 10; number++)
            {
                for (int copy = 0; copy < config.NumberCopiesPerValue; copy++)
                {
                    slotDeck.Add(new NumberCard(number));
                }
            }

            // × 특수 카드 추가
            for (int i = 0; i < config.MultiplyCardsPerRound; i++)
            {
                slotDeck.Add(new SpecialCard(SpecialCard.SpecialType.Multiply));
            }

            // √ 특수 카드 추가
            for (int i = 0; i < config.SquareRootCardsPerRound; i++)
            {
                slotDeck.Add(new SpecialCard(SpecialCard.SpecialType.SquareRoot));
            }

            // 셔플
            ShuffleDeck(slotDeck);
        }

        /// <summary>
        /// 슬롯 덱에서 한 장을 뽑습니다.
        /// 덱이 소진되면 자동으로 재구성합니다.
        /// </summary>
        public Card DrawSlotCard()
        {
            if (slotDeck.Count == 0)
            {
                BuildSlotDeck();
            }

            int lastIndex = slotDeck.Count - 1;
            Card card = slotDeck[lastIndex];
            slotDeck.RemoveAt(lastIndex);

            return card.Clone(); // 복제본 반환
        }

        /// <summary>
        /// 랜덤 숫자 카드를 생성합니다 (무한 생성 가능)
        /// AI용으로 주로 사용됩니다.
        /// </summary>
        public NumberCard DrawRandomNumberCard()
        {
            int value = random.Next(0, 11); // 0~10
            return new NumberCard(value);
        }

        /// <summary>
        /// 덱을 섞습니다.
        /// 
        /// [학습 포인트] Fisher-Yates 셔플 알고리즘
        /// 효율적이고 공정한 셔플 알고리즘입니다.
        /// </summary>
        private void ShuffleDeck(List<Card> deck)
        {
            for (int i = deck.Count - 1; i > 0; i--)
            {
                int randomIndex = random.Next(i + 1);

                // 교환
                Card temp = deck[i];
                deck[i] = deck[randomIndex];
                deck[randomIndex] = temp;
            }
        }

        /// <summary>
        /// 남은 카드 개수를 반환합니다.
        /// </summary>
        public int GetRemainingCardCount()
        {
            return slotDeck.Count;
        }
    }
}