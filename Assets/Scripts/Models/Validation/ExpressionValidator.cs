using System.Collections.Generic;
using UnityEngine;

namespace MathHighLow.Models
{
    /// <summary>
    /// [학습 포인트] 검증 로직 분리
    /// 
    /// 플레이어가 만든 수식이 게임 규칙에 맞는지 검증합니다.
    /// 
    /// 검증 규칙:
    /// 1. 모든 숫자 카드 사용
    /// 2. 모든 특수 카드 사용
    /// 3. 비활성화된 연산자 미사용
    /// 4. 수식 형식이 올바른지 (숫자-연산자-숫자...)
    /// 
    /// 실습 과제:
    /// 1. 수식 길이 제한 검증 추가
    /// 2. 특정 숫자 범위 제한 추가
    /// </summary>
    public static class ExpressionValidator
    {
        /// <summary>
        /// 검증 결과를 담는 클래스
        /// </summary>
        public class ValidationResult
        {
            public bool IsValid { get; set; }
            public string ErrorMessage { get; set; }
            public List<string> Warnings { get; set; }

            public ValidationResult()
            {
                IsValid = true;
                ErrorMessage = "";
                Warnings = new List<string>();
            }
        }

        /// <summary>
        /// 수식을 검증합니다.
        /// 
        /// [학습 포인트] 단계별 검증
        /// 여러 규칙을 순서대로 검증하고, 실패하면 즉시 중단합니다.
        /// </summary>
        private const string GeneralFailureMessage = "수식을 완성하지 못했습니다.";

        public static ValidationResult Validate(Expression expression, Hand hand)
        {
            ValidationResult result = new ValidationResult();

            // 1단계: 수식이 비어있지 않은지
            if (!ValidateNotEmpty(expression, result))
                return result;

            // 2단계: 수식이 완성되었는지 (숫자-연산자-숫자... 형태)
            if (!ValidateComplete(expression, result))
                return result;

            // 3단계: 숫자 카드 사용량 검증
            if (!ValidateNumberUsage(expression, hand, result))
                return result;

            // 4단계: 특수 카드(√) 사용량 검증
            if (!ValidateSquareRootUsage(expression, hand, result))
                return result;

            // 5단계: 곱하기(×) 카드 사용량 검증
            if (!ValidateMultiplyUsage(expression, hand, result))
                return result;

            // 6단계: 비활성화된 연산자 사용 여부
            if (!ValidateDisabledOperators(expression, hand, result))
                return result;

            return result;
        }

        /// <summary>
        /// 1단계: 수식이 비어있지 않은지 검증
        /// </summary>
        private static bool ValidateNotEmpty(Expression expression, ValidationResult result)
        {
            if (expression.IsEmpty())
            {
                MarkInvalid(result, "수식이 비어있습니다.");
                return false;
            }
            return true;
        }

        /// <summary>
        /// 2단계: 수식이 완성되었는지 검증
        /// </summary>
        private static bool ValidateComplete(Expression expression, ValidationResult result)
        {
            if (!expression.IsComplete())
            {
                MarkInvalid(result, "수식이 완성되지 않았습니다.");
                return false;
            }
            return true;
        }

        /// <summary>
        /// 3단계: 숫자 카드 사용량 검증
        /// 
        /// [학습 포인트] Dictionary를 이용한 개수 세기
        /// </summary>
        private static bool ValidateNumberUsage(Expression expression, Hand hand, ValidationResult result)
        {
            // 손패의 숫자별 개수 세기
            Dictionary<int, int> availableNumbers = new Dictionary<int, int>();
            foreach (var card in hand.NumberCards)
            {
                if (!availableNumbers.ContainsKey(card.Value))
                    availableNumbers[card.Value] = 0;
                availableNumbers[card.Value]++;
            }

            // 수식에서 사용한 숫자별 개수 세기
            Dictionary<int, int> usedNumbers = new Dictionary<int, int>();
            foreach (var number in expression.Numbers)
            {
                int intNumber = Mathf.RoundToInt(number);
                if (!usedNumbers.ContainsKey(intNumber))
                    usedNumbers[intNumber] = 0;
                usedNumbers[intNumber]++;
            }

            // 비교
            foreach (var kvp in availableNumbers)
            {
                int number = kvp.Key;
                int available = kvp.Value;
                int used = usedNumbers.ContainsKey(number) ? usedNumbers[number] : 0;

                if (used < available)
                {
                    MarkInvalid(result, $"숫자 {number}을(를) {available - used}장 더 사용해야 합니다.");
                    return false;
                }
                else if (used > available)
                {
                    MarkInvalid(result, $"숫자 {number}을(를) {used - available}장 초과 사용했습니다.");
                    return false;
                }
            }

            return true;
        }

        /// <summary>
        /// 4단계: 제곱근(√) 사용량 검증
        /// </summary>
        private static bool ValidateSquareRootUsage(Expression expression, Hand hand, ValidationResult result)
        {
            int requiredCount = hand.GetSquareRootCount();
            int usedCount = 0;

            foreach (bool hasRoot in expression.HasSquareRoot)
            {
                if (hasRoot) usedCount++;
            }

            if (usedCount < requiredCount)
            {
                MarkInvalid(result, $"√를 {requiredCount - usedCount}개 더 사용해야 합니다.");
                return false;
            }
            else if (usedCount > requiredCount)
            {
                MarkInvalid(result, $"√를 {usedCount - requiredCount}개 초과 사용했습니다.");
                return false;
            }

            return true;
        }

        /// <summary>
        /// 5단계: 곱하기(×) 카드 사용량 검증
        /// </summary>
        private static bool ValidateMultiplyUsage(Expression expression, Hand hand, ValidationResult result)
        {
            int requiredCount = hand.GetMultiplyCount();
            int usedCount = 0;

            foreach (var op in expression.Operators)
            {
                if (op == OperatorCard.OperatorType.Multiply)
                    usedCount++;
            }

            if (usedCount < requiredCount)
            {
                MarkInvalid(result, $"×를 {requiredCount - usedCount}개 더 사용해야 합니다.");
                return false;
            }
            else if (usedCount > requiredCount)
            {
                MarkInvalid(result, $"×를 {usedCount - requiredCount}개 초과 사용했습니다.");
                return false;
            }

            return true;
        }

        /// <summary>
        /// 6단계: 비활성화된 연산자 사용 여부 검증
        /// </summary>
        private static bool ValidateDisabledOperators(Expression expression, Hand hand, ValidationResult result)
        {
            foreach (var op in expression.Operators)
            {
                if (!hand.IsOperatorEnabled(op) && op != OperatorCard.OperatorType.Multiply)
                {
                    MarkInvalid(result, $"비활성화된 연산자를 사용했습니다: {GetOperatorName(op)}");
                    return false;
                }
            }

            return true;
        }

        private static void MarkInvalid(ValidationResult result, string detailedMessage)
        {
            result.IsValid = false;
            result.ErrorMessage = GeneralFailureMessage;

            if (!string.IsNullOrEmpty(detailedMessage))
            {
                result.Warnings.Add(detailedMessage);
                Debug.LogWarning($"[ExpressionValidator] {detailedMessage}");
            }
        }

        /// <summary>
        /// 연산자 이름 반환
        /// </summary>
        private static string GetOperatorName(OperatorCard.OperatorType op)
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
    }
}

