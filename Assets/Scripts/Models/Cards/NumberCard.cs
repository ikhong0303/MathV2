using UnityEngine;

namespace MathHighLow.Models
{
    /// <summary>
    /// [학습 포인트] 클래스 상속
    /// 
    /// 0~10 사이의 숫자를 나타내는 카드입니다.
    /// 
    /// </summary>
    [System.Serializable]
    public class NumberCard : Card
    {
        /// <summary>
        /// 카드의 숫자 값 (0~10)
        /// </summary>
        public int Value { get; set; }

        /// <summary>
        /// 생성자
        /// </summary>
        public NumberCard(int value)
        {
            Value = Mathf.Clamp(value, 0, 10);
        }

        /// <summary>
        /// UI에 표시할 숫자를 문자열로 반환
        /// </summary>
        public override string GetDisplayText()
        {
            return Value.ToString();
        }

        /// <summary>
        /// 카드 타입 반환
        /// </summary>
        public override string GetCardType()
        {
            return "Number";
        }

        /// <summary>
        /// 카드 복제
        /// </summary>
        public override Card Clone()
        {
            return new NumberCard(Value);
        }

        /// <summary>
        /// 같은 숫자인지 비교
        /// </summary>
        public bool HasSameValue(NumberCard other)
        {
            return other != null && Value == other.Value;
        }

        /// <summary>
        /// 디버깅용 문자열
        /// </summary>
        public override string ToString()
        {
            return $"NumberCard({Value})";
        }
    }
}
