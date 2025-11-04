using System.Collections.Generic;
using UnityEngine;

namespace MathHighLow.Models
{
    /// <summary>
    /// [학습 포인트] 스택을 이용한 수식 계산
    /// 
    /// 수식을 계산하여 결과값을 반환합니다.
    /// 연산자 우선순위를 고려합니다 (×÷ 먼저, +- 나중에)
    /// 
    /// 계산 과정:
    /// 1. 제곱근 먼저 적용 (√4 = 2)
    /// 2. 연산자 우선순위에 따라 계산
    ///    - 스택 사용: 우선순위 높은 연산자부터 처리
    /// 
    /// 실습 과제:
    /// 1. 괄호 지원 추가
    /// 2. 계산 과정을 단계별로 보여주는 GetSteps() 메서드 추가
    /// </summary>
    public static class ExpressionEvaluator
    {
        /// <summary>
        /// 계산 결과를 담는 클래스
        /// </summary>
        public class EvaluationResult
        {
            public bool Success { get; set; }
            public float Value { get; set; }
            public string ErrorMessage { get; set; }

            public EvaluationResult()
            {
                Success = true;
                Value = 0;
                ErrorMessage = "";
            }
        }

        /// <summary>
        /// 수식을 계산합니다.
        /// 
        /// [학습 포인트] 두 패스 알고리즘
        /// 1패스: 제곱근 적용
        /// 2패스: 연산자 우선순위 계산
        /// </summary>
        public static EvaluationResult Evaluate(Expression expression)
        {
            EvaluationResult result = new EvaluationResult();

            if (expression.IsEmpty())
            {
                result.Success = false;
                result.ErrorMessage = "빈 수식입니다.";
                return result;
            }

            // 1패스: 제곱근 적용
            List<float> processedNumbers = ApplySquareRoots(expression, result);
            if (!result.Success)
                return result;

            // 2패스: 연산자 우선순위 계산
            float finalValue = CalculateWithPriority(processedNumbers, expression.Operators, result);
            if (!result.Success)
                return result;

            result.Value = finalValue;
            return result;
        }

        /// <summary>
        /// 1패스: 모든 숫자에 제곱근을 적용합니다.
        /// </summary>
        private static List<float> ApplySquareRoots(Expression expression, EvaluationResult result)
        {
            List<float> processed = new List<float>();

            for (int i = 0; i < expression.Numbers.Count; i++)
            {
                float number = expression.Numbers[i];

                if (expression.HasSquareRoot[i])
                {
                    if (number < 0)
                    {
                        result.Success = false;
                        result.ErrorMessage = "음수에 제곱근을 적용할 수 없습니다.";
                        return processed;
                    }
                    number = Mathf.Sqrt(number);
                }

                processed.Add(number);
            }

            return processed;
        }

        /// <summary>
        /// 2패스: 연산자 우선순위를 고려하여 계산합니다.
        /// 
        /// [학습 포인트] 스택 기반 계산
        /// 
        /// 알고리즘:
        /// 1. 숫자와 연산자를 순서대로 처리
        /// 2. 현재 연산자보다 스택의 연산자 우선순위가 높으면 먼저 계산
        /// 3. 모든 항목 처리 후 스택에 남은 연산 수행
        /// </summary>
        private static float CalculateWithPriority(List<float> numbers, List<OperatorCard.OperatorType> operators, EvaluationResult result)
        {
            // 스택 초기화
            Stack<float> numberStack = new Stack<float>();
            Stack<OperatorCard.OperatorType> operatorStack = new Stack<OperatorCard.OperatorType>();

            // 첫 숫자 추가
            numberStack.Push(numbers[0]);

            // 연산자와 숫자를 순서대로 처리
            for (int i = 0; i < operators.Count; i++)
            {
                OperatorCard.OperatorType currentOp = operators[i];
                float nextNumber = numbers[i + 1];

                // 스택에 있는 더 높은 우선순위 연산자들을 먼저 계산
                while (operatorStack.Count > 0 && GetPriority(operatorStack.Peek()) >= GetPriority(currentOp))
                {
                    if (!ExecuteOperation(numberStack, operatorStack, result))
                        return 0;
                }

                // 현재 연산자와 숫자 추가
                operatorStack.Push(currentOp);
                numberStack.Push(nextNumber);
            }

            // 남은 연산자들 모두 계산
            while (operatorStack.Count > 0)
            {
                if (!ExecuteOperation(numberStack, operatorStack, result))
                    return 0;
            }

            return numberStack.Pop();
        }

        /// <summary>
        /// 스택에서 연산 하나를 수행합니다.
        /// </summary>
        private static bool ExecuteOperation(Stack<float> numberStack, Stack<OperatorCard.OperatorType> operatorStack, EvaluationResult result)
        {
            if (numberStack.Count < 2)
            {
                result.Success = false;
                result.ErrorMessage = "계산 중 오류가 발생했습니다.";
                return false;
            }

            // 스택은 LIFO이므로 순서 주의
            float right = numberStack.Pop();
            float left = numberStack.Pop();
            OperatorCard.OperatorType op = operatorStack.Pop();

            // 계산
            float opResult = Calculate(left, op, right, result);
            if (!result.Success)
                return false;

            // 결과를 다시 스택에 추가
            numberStack.Push(opResult);
            return true;
        }

        /// <summary>
        /// 두 숫자를 연산자로 계산합니다.
        /// </summary>
        private static float Calculate(float left, OperatorCard.OperatorType op, float right, EvaluationResult result)
        {
            switch (op)
            {
                case OperatorCard.OperatorType.Add:
                    return left + right;

                case OperatorCard.OperatorType.Subtract:
                    return left - right;

                case OperatorCard.OperatorType.Multiply:
                    return left * right;

                case OperatorCard.OperatorType.Divide:
                    if (Mathf.Approximately(right, 0))
                    {
                        result.Success = false;
                        result.ErrorMessage = "0으로 나눌 수 없습니다.";
                        return 0;
                    }
                    return left / right;

                default:
                    result.Success = false;
                    result.ErrorMessage = "알 수 없는 연산자입니다.";
                    return 0;
            }
        }

        /// <summary>
        /// 연산자 우선순위를 반환합니다.
        /// </summary>
        private static int GetPriority(OperatorCard.OperatorType op)
        {
            switch (op)
            {
                case OperatorCard.OperatorType.Multiply:
                case OperatorCard.OperatorType.Divide:
                    return 2; // 높은 우선순위

                case OperatorCard.OperatorType.Add:
                case OperatorCard.OperatorType.Subtract:
                    return 1; // 낮은 우선순위

                default:
                    return 0;
            }
        }

        /// <summary>
        /// 간단한 계산 (왼쪽에서 오른쪽으로)
        /// 연산자 우선순위를 무시하고 순차적으로 계산합니다.
        /// 교육용으로 먼저 가르칠 수 있는 간단한 버전입니다.
        /// </summary>
        public static EvaluationResult EvaluateSimple(Expression expression)
        {
            EvaluationResult result = new EvaluationResult();

            if (expression.IsEmpty())
            {
                result.Success = false;
                result.ErrorMessage = "빈 수식입니다.";
                return result;
            }

            // 제곱근 적용
            List<float> processedNumbers = ApplySquareRoots(expression, result);
            if (!result.Success)
                return result;

            // 왼쪽부터 순서대로 계산
            float value = processedNumbers[0];
            for (int i = 0; i < expression.Operators.Count; i++)
            {
                float nextNumber = processedNumbers[i + 1];
                value = Calculate(value, expression.Operators[i], nextNumber, result);
                if (!result.Success)
                    return result;
            }

            result.Value = value;
            return result;
        }
    }
}
