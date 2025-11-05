using System.Collections.Generic;
using System.Linq;

namespace MathHighLow.Models
{
    /// <summary>
    /// ✅ 수정: OperatorCard 리스트 추가
    /// 
    /// 새로운 구조에서는 연산자도 카드로 받으므로
    /// 손패에 OperatorCard 리스트가 필요합니다.
    /// </summary>
    [System.Serializable]
    public class Hand
    {
        /// <summary>
        /// 보유한 숫자 카드 목록
        /// </summary>
        public List<NumberCard> NumberCards { get; private set; }

        /// <summary>
        /// ✅ 추가: 보유한 연산자 카드 목록
        /// </summary>
        public List<OperatorCard> OperatorCards { get; private set; }

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
            OperatorCards = new List<OperatorCard>();  // ✅ 추가
            SpecialCards = new List<SpecialCard>();
            DisabledOperators = new List<OperatorCard.OperatorType>();
        }

        /// <summary>
        /// 손패를 비웁니다 (라운드 시작 시 사용)
        /// </summary>
        public void Clear()
        {
            NumberCards.Clear();
            OperatorCards.Clear();  // ✅ 추가
            SpecialCards.Clear();
            DisabledOperators.Clear();
        }

        /// <summary>
        /// ✅ 수정: OperatorCard도 처리하도록 확장
        /// </summary>
        public void AddCard(Card card)
        {
            if (card is NumberCard number)
            {
                NumberCards.Add(number);
            }
            else if (card is OperatorCard operatorCard)  // ✅ 추가
            {
                OperatorCards.Add(operatorCard);
            }
            else if (card is SpecialCard special)
            {
                SpecialCards.Add(special);
            }
        }

        /// <summary>
        /// ✅ 수정: OperatorCard도 제거 가능
        /// </summary>
        public bool RemoveCard(Card card)
        {
            if (card is NumberCard number)
            {
                return NumberCards.Remove(number);
            }
            else if (card is OperatorCard operatorCard)  // ✅ 추가
            {
                return OperatorCards.Remove(operatorCard);
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
        /// ✅ 수정: 연산자 카드도 포함
        /// </summary>
        public List<OperatorCard.OperatorType> GetAvailableOperators()
        {
            // 손패에 있는 연산자 카드의 타입 반환
            return OperatorCards
                .Where(op => IsOperatorEnabled(op.Operator))
                .Select(op => op.Operator)
                .Distinct()
                .ToList();
        }

        /// <summary>
        /// ✅ 수정: 연산자 카드도 포함하여 총 카드 수 계산
        /// </summary>
        public int GetTotalCardCount()
        {
            return NumberCards.Count + OperatorCards.Count + SpecialCards.Count;
        }

        /// <summary>
        /// 손패가 비어있는지 확인합니다.
        /// </summary>
        public bool IsEmpty()
        {
            return NumberCards.Count == 0 && OperatorCards.Count == 0 && SpecialCards.Count == 0;
        }

        /// <summary>
        /// ✅ 수정: 연산자 카드 정보도 포함
        /// </summary>
        public override string ToString()
        {
            return $"Hand: {NumberCards.Count} numbers, {OperatorCards.Count} operators, {SpecialCards.Count} specials";
        }
    }
}