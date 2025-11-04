using UnityEngine;

namespace MathHighLow.Models
{
    /// <summary>
    /// [학습 포인트] 추상 클래스와 다형성
    /// 
    /// 모든 카드의 공통 기능을 정의합니다.
    /// 각 카드 종류는 이 클래스를 상속받아 구현합니다.
    /// 
    /// 왜 추상 클래스를 사용할까요?
    /// - 공통 속성/메서드를 한 곳에서 관리
    /// - 카드 타입별 다른 동작 구현 가능
    /// - 새로운 카드 추가가 쉬움
    /// </summary>
    [System.Serializable]
    public abstract class Card
    {
        /// <summary>
        /// 카드가 UI에 표시할 텍스트를 반환합니다.
        /// 각 카드 타입에서 override하여 구현합니다.
        /// </summary>
        public abstract string GetDisplayText();

        /// <summary>
        /// 카드의 종류를 문자열로 반환합니다.
        /// 디버깅이나 저장/로드 시 유용합니다.
        /// </summary>
        public abstract string GetCardType();

        /// <summary>
        /// 카드를 복제합니다.
        /// 덱에서 카드를 뽑을 때 원본을 보호하기 위해 사용합니다.
        /// </summary>
        public abstract Card Clone();

        /// <summary>
        /// 두 카드가 같은 종류인지 비교합니다.
        /// </summary>
        public virtual bool IsSameType(Card other)
        {
            return other != null && GetCardType() == other.GetCardType();
        }
    }
}
