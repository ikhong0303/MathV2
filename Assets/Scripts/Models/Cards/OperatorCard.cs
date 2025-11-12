using System;
using UnityEngine;

namespace MathHighLow.Models
{
    /// <summary>
    /// [학습 포인트] enum과 switch 문
    /// 
    /// 사칙연산 기호를 나타내는 카드입니다.
    /// +, -, ×, ÷ 네 가지 연산자를 지원합니다.
    /// 

    /// </summary>
    [System.Serializable]
    public class OperatorCard : Card
    {
        /// <summary>
        /// 연산자 종류
        /// </summary>
        public enum OperatorType
        {
            Add,        // +
            Subtract,   // -
            Multiply,   // ×
            Divide      // ÷
        }

        /// <summary>
        /// 이 카드의 연산자 타입
        /// </summary>
        public OperatorType Operator { get; set; }

        /// <summary>
        /// 생성자
        /// </summary>
        public OperatorCard(OperatorType op)
        {
            Operator = op;
        }

        /// <summary>
        /// 연산자 기호를 문자열로 반환
        /// </summary>
        public override string GetDisplayText()
        {
            return Operator switch
            {
                OperatorType.Add => "+",
                OperatorType.Subtract => "-",
                OperatorType.Multiply => "×",
                OperatorType.Divide => "÷",
                _ => "?"
            };
        }

        /// <summary>
        /// 카드 타입 반환
        /// </summary>
        public override string GetCardType()
        {
            return "Operator";
        }

        /// <summary>
        /// 카드 복제
        /// </summary>
        public override Card Clone()
        {
            return new OperatorCard(Operator);
        }

        /// <summary>
        /// 실제 계산을 수행합니다.
        /// 
        /// [학습 포인트] 메서드의 역할
        /// 데이터(카드)가 자신의 동작(계산)을 가지고 있습니다.
        /// </summary>
        public float Calculate(float left, float right)
        {
            return Operator switch
            {
                OperatorType.Add => left + right,
                OperatorType.Subtract => left - right,
                OperatorType.Multiply => left * right,
                OperatorType.Divide => right != 0 ? left / right : float.PositiveInfinity,
                _ => 0
            };
        }

        /// <summary>
        /// 연산자 우선순위를 반환합니다.
        /// × ÷ = 2 (높은 우선순위)
        /// + - = 1 (낮은 우선순위)
        /// </summary>
        public int GetPriority()
        {
            return Operator switch
            {
                OperatorType.Multiply => 2,
                OperatorType.Divide => 2,
                OperatorType.Add => 1,
                OperatorType.Subtract => 1,
                _ => 0
            };
        }

        /// <summary>
        /// 디버깅용 문자열
        /// </summary>
        public override string ToString()
        {
            return $"OperatorCard({GetDisplayText()})";
        }
    }
}
