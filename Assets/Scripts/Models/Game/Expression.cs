using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace MathHighLow.Models
{
    /// <summary>
    /// [학습 포인트] 복합 데이터 구조
    /// 
    /// 플레이어가 만든 수식을 표현합니다.
    /// 예: √4 × 3 + 2
    /// 
    /// 구조:
    /// - Numbers: [4, 3, 2]
    /// - Operators: [×, +]
    /// - SquareRoots: [true, false, false] (각 숫자에 √가 있는지)
    /// 
    /// 실습 과제:
    /// 1. Reverse() 메서드로 수식을 거꾸로 뒤집기
    /// 2. ReplaceNumber(index, newValue) 메서드 추가
    /// </summary>
    [System.Serializable]
    public class Expression
    {
        /// <summary>
        /// 수식의 숫자들 (순서대로)
        /// </summary>
        public List<float> Numbers { get; private set; }

        /// <summary>
        /// 수식의 연산자들 (순서대로)
        /// 연산자 개수 = 숫자 개수 - 1
        /// </summary>
        public List<OperatorCard.OperatorType> Operators { get; private set; }

        /// <summary>
        /// 각 숫자에 제곱근이 적용되는지 여부
        /// [true, false, true] = √숫자1, 숫자2, √숫자3
        /// </summary>
        public List<bool> HasSquareRoot { get; private set; }

        /// <summary>
        /// 생성자
        /// </summary>
        public Expression()
        {
            Numbers = new List<float>();
            Operators = new List<OperatorCard.OperatorType>();
            HasSquareRoot = new List<bool>();
        }

        /// <summary>
        /// 숫자를 추가합니다.
        /// </summary>
        public void AddNumber(float number, bool withSquareRoot = false)
        {
            Numbers.Add(number);
            HasSquareRoot.Add(withSquareRoot);
        }

        /// <summary>
        /// 연산자를 추가합니다.
        /// </summary>
        public void AddOperator(OperatorCard.OperatorType op)
        {
            Operators.Add(op);
        }

        /// <summary>
        /// 마지막에 추가한 항목을 제거합니다 (Undo 기능)
        /// </summary>
        public void RemoveLast()
        {
            if (Operators.Count > 0 && Operators.Count == Numbers.Count - 1)
            {
                // 마지막이 연산자면 연산자 제거
                Operators.RemoveAt(Operators.Count - 1);
            }
            else if (Numbers.Count > 0)
            {
                // 마지막이 숫자면 숫자 제거
                Numbers.RemoveAt(Numbers.Count - 1);
                HasSquareRoot.RemoveAt(HasSquareRoot.Count - 1);
            }
        }

        /// <summary>
        /// 수식을 초기화합니다.
        /// </summary>
        public void Clear()
        {
            Numbers.Clear();
            Operators.Clear();
            HasSquareRoot.Clear();
        }

        /// <summary>
        /// 수식이 완성되었는지 확인합니다.
        /// 완성 조건: 연산자 개수 = 숫자 개수 - 1
        /// </summary>
        public bool IsComplete()
        {
            return Numbers.Count > 0 && Operators.Count == Numbers.Count - 1;
        }

        /// <summary>
        /// 수식이 비어있는지 확인합니다.
        /// </summary>
        public bool IsEmpty()
        {
            return Numbers.Count == 0;
        }

        /// <summary>
        /// 다음에 추가해야 할 것이 숫자인지 연산자인지 반환합니다.
        /// </summary>
        public bool ExpectingNumber()
        {
            return Numbers.Count == Operators.Count;
        }

        /// <summary>
        /// 수식을 "√4 × 3 + 2" 형식의 문자열로 변환합니다.
        /// 
        /// [학습 포인트] StringBuilder 사용
        /// 문자열을 반복적으로 연결할 때는 StringBuilder를 사용하는 것이 효율적입니다.
        /// </summary>
        public string ToDisplayString()
        {
            if (Numbers.Count == 0)
            {
                return "";
            }

            StringBuilder sb = new StringBuilder();

            for (int i = 0; i < Numbers.Count; i++)
            {
                // 제곱근 기호 추가
                if (HasSquareRoot[i])
                {
                    sb.Append("√");
                }

                // 숫자 추가
                sb.Append(Numbers[i].ToString("0.##"));

                // 연산자 추가 (마지막 숫자가 아니면)
                if (i < Operators.Count)
                {
                    sb.Append(" ");
                    sb.Append(GetOperatorSymbol(Operators[i]));
                    sb.Append(" ");
                }
            }

            return sb.ToString();
        }

        /// <summary>
        /// 연산자를 기호로 변환합니다.
        /// </summary>
        private string GetOperatorSymbol(OperatorCard.OperatorType op)
        {
            return op switch
            {
                OperatorCard.OperatorType.Add => "+",
                OperatorCard.OperatorType.Subtract => "-",
                OperatorCard.OperatorType.Multiply => "×",
                OperatorCard.OperatorType.Divide => "÷",
                _ => "?"
            };
        }

        /// <summary>
        /// 수식을 복제합니다.
        /// </summary>
        public Expression Clone()
        {
            Expression clone = new Expression();
            clone.Numbers.AddRange(Numbers);
            clone.Operators.AddRange(Operators);
            clone.HasSquareRoot.AddRange(HasSquareRoot);
            return clone;
        }

        /// <summary>
        /// 디버깅용 문자열
        /// </summary>
        public override string ToString()
        {
            return $"Expression: {ToDisplayString()}";
        }
    }
}
