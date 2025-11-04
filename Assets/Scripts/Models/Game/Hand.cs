using System.Collections.Generic;
using System.Linq;

namespace MathHighLow.Models
{
    /// <summary>
    /// [학습 포인트] 컬렉션 관리와 LINQ
    /// 
    /// 한 플레이어(또는 AI)의 손패를 관리합니다.
    /// 숫자 카드와 특수 카드를 따로 관리합니다.
    /// 
    /// 실습 과제:
    /// 1. 손패의 총 가치를 계산하는 GetTotalValue() 메서드 추가
    /// 2. 특정 숫자 카드를 찾는 FindNumberCard(int value) 메서드 추가
    /// </summary>
    [System.Serializable]
    public class Hand
    {
        /// <summary>
        /// 보유한 숫자 카드 목록
        /// </summary>
        public List<NumberCard> NumberCards { get; private set; }

        /// <summary>
        /// 보유한 특수 카드 목록
        /// </summary>
        public List<SpecialCard> SpecialCards { get; private set; }

        /// <summary>
        /// 비활성화된 연산자 목록
        /// × 카드 사용 시 선택한 연산자가 여기 추가됩니다.
        /// </summary>
        public List<OperatorCard.OperatorType> DisabledOperators { get; private set; }

        /// <summary>
        /// 생성자
        /// </summary>
        public Hand()
        {
            NumberCards = new List<NumberCard>();
            SpecialCards = new List<SpecialCard>();
            DisabledOperators = new List<OperatorCard.OperatorType>();
        }

        /// <summary>
        /// 손패를 비웁니다 (라운드 시작 시 사용)
        /// </summary>
        public void Clear()
        {
            NumberCards.Clear();
            SpecialCards.Clear();
            DisabledOperators.Clear();
        }

        /// <summary>
        /// 카드를 추가합니다.
        /// 
        /// [학습 포인트] 다형성 활용
        /// Card 타입을 받아서 실제 타입에 따라 다르게 처리합니다.
        /// </summary>
        public void AddCard(Card card)
        {
            if (card is NumberCard number)
            {
                NumberCards.Add(number);
            }
            else if (card is SpecialCard special)
            {
                SpecialCards.Add(special);
            }
        }

        /// <summary>
        /// 특정 카드를 제거합니다.
        /// </summary>
        public bool RemoveCard(Card card)
        {
            if (card is NumberCard number)
            {
                return NumberCards.Remove(number);
            }
            else if (card is SpecialCard special)
            {
                return SpecialCards.Remove(special);
            }
            return false;
        }

        /// <summary>
        /// 곱하기(×) 특수 카드 개수를 반환합니다.
        /// </summary>
        public int GetMultiplyCount()
        {
            return SpecialCards.Count(c => c.Type == SpecialCard.SpecialType.Multiply);
        }

        /// <summary>
        /// 제곱근(√) 특수 카드 개수를 반환합니다.
        /// </summary>
        public int GetSquareRootCount()
        {
            return SpecialCards.Count(c => c.Type == SpecialCard.SpecialType.SquareRoot);
        }

        /// <summary>
        /// 특정 연산자가 사용 가능한지 확인합니다.
        /// </summary>
        public bool IsOperatorEnabled(OperatorCard.OperatorType op)
        {
            return !DisabledOperators.Contains(op);
        }

        /// <summary>
        /// 연산자를 비활성화합니다.
        /// </summary>
        public void DisableOperator(OperatorCard.OperatorType op)
        {
            if (!DisabledOperators.Contains(op))
            {
                DisabledOperators.Add(op);
            }
        }

        /// <summary>
        /// 사용 가능한 기본 연산자 목록을 반환합니다.
        /// (Multiply는 특수 카드로만 사용 가능하므로 제외)
        /// </summary>
        public List<OperatorCard.OperatorType> GetAvailableOperators()
        {
            var baseOperators = new List<OperatorCard.OperatorType>
            {
                OperatorCard.OperatorType.Add,
                OperatorCard.OperatorType.Subtract,
                OperatorCard.OperatorType.Divide
            };

            return baseOperators.Where(op => IsOperatorEnabled(op)).ToList();
        }

        /// <summary>
        /// 손패의 총 카드 수를 반환합니다.
        /// </summary>
        public int GetTotalCardCount()
        {
            return NumberCards.Count + SpecialCards.Count;
        }

        /// <summary>
        /// 손패가 비어있는지 확인합니다.
        /// </summary>
        public bool IsEmpty()
        {
            return NumberCards.Count == 0 && SpecialCards.Count == 0;
        }

        /// <summary>
        /// 디버깅용 문자열
        /// </summary>
        public override string ToString()
        {
            return $"Hand: {NumberCards.Count} numbers, {SpecialCards.Count} specials";
        }
    }
}
