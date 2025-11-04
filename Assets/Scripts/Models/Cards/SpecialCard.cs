using System;
using UnityEngine;

namespace MathHighLow.Models
{
    /// <summary>
    /// [학습 포인트] 특별한 규칙을 가진 객체
    /// 
    /// 게임에 특별한 효과를 주는 카드입니다.
    /// - Multiply: 연산자 하나를 비활성화하고, × 연산자를 강제로 사용
    /// - SquareRoot: 숫자에 제곱근을 적용
    /// 
    /// 실습 과제:
    /// 1. Square (제곱) 타입을 추가해보세요
    /// 2. Negate (부호 반전) 타입을 추가해보세요
    /// </summary>
    [System.Serializable]
    public class SpecialCard : Card
    {
        /// <summary>
        /// 특수 카드 종류
        /// </summary>
        public enum SpecialType
        {
            Multiply,       // × 강제 사용 카드
            SquareRoot      // √ 제곱근 카드
        }

        /// <summary>
        /// 이 카드의 특수 효과 타입
        /// </summary>
        public SpecialType Type { get; set; }

        /// <summary>
        /// 생성자
        /// </summary>
        public SpecialCard(SpecialType type)
        {
            Type = type;
        }

        /// <summary>
        /// 특수 카드 기호를 문자열로 반환
        /// </summary>
        public override string GetDisplayText()
        {
            return Type switch
            {
                SpecialType.Multiply => "×",
                SpecialType.SquareRoot => "√",
                _ => "?"
            };
        }

        /// <summary>
        /// 카드 타입 반환
        /// </summary>
        public override string GetCardType()
        {
            return "Special";
        }

        /// <summary>
        /// 카드 복제
        /// </summary>
        public override Card Clone()
        {
            return new SpecialCard(Type);
        }

        /// <summary>
        /// 이 특수 카드가 숫자 앞에 붙는 단항 연산자인지 확인
        /// √는 단항 연산자 (√4 처럼 사용)
        /// ×는 이항 연산자 (2 × 3 처럼 사용)
        /// </summary>
        public bool IsUnaryOperator()
        {
            return Type == SpecialType.SquareRoot;
        }

        /// <summary>
        /// 제곱근 계산을 수행합니다.
        /// </summary>
        public float ApplySquareRoot(float value)
        {
            if (Type != SpecialType.SquareRoot)
            {
                Debug.LogWarning("이 카드는 제곱근 카드가 아닙니다.");
                return value;
            }

            if (value < 0)
            {
                Debug.LogError("음수에는 제곱근을 적용할 수 없습니다.");
                return float.NaN;
            }

            return Mathf.Sqrt(value);
        }

        /// <summary>
        /// 디버깅용 문자열
        /// </summary>
        public override string ToString()
        {
            return $"SpecialCard({GetDisplayText()})";
        }
    }
}
