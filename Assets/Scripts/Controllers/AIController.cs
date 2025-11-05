using UnityEngine;
using MathHighLow.Models;
using System.Collections.Generic;
using System.Linq;

namespace MathHighLow.Controllers
{
    /// <summary>
    /// ✅ 수정: 연산자 카드를 한 번씩만 사용하도록 개선
    /// 
    /// 이제 연산자도 카드로 받기 때문에:
    /// - +, -, ÷ 각각 1장씩만 사용 가능
    /// - × 카드는 여러 장 가능 (특수 카드)
    /// </summary>
    public class AIController : MonoBehaviour
    {
        private Hand currentHand;
        private int targetNumber;
        private Expression bestExpression;
        private float bestDistance;
        private List<OperatorCard.OperatorType> availableOperators;

        public void Initialize()
        {
            // 초기화
        }

        public void PlayTurn(Hand hand, int target)
        {
            bestExpression = FindBestExpression(hand, target);

            // ✅ 디버그: AI가 선택한 수식 출력
            if (bestExpression != null)
            {
                Debug.Log($"[AI] 선택한 수식: {bestExpression.ToDisplayString()}");
            }
        }

        public Expression GetExpression()
        {
            return bestExpression ?? new Expression();
        }

        /// <summary>
        /// 최적 수식을 찾습니다 (완전 탐색)
        /// </summary>
        private Expression FindBestExpression(Hand hand, int target)
        {
            // 초기화
            currentHand = hand;
            targetNumber = target;
            bestExpression = new Expression();
            bestDistance = float.PositiveInfinity;
            availableOperators = hand.GetAvailableOperators();

            // ✅ 디버그: 사용 가능한 연산자 출력
            Debug.Log($"[AI] 사용 가능한 연산자: {string.Join(", ", availableOperators)}");

            // 카드가 없으면 빈 수식 반환
            if (hand.NumberCards.Count == 0)
                return new Expression();

            // 곱하기와 기본 연산자 검증
            int multiplyCount = hand.GetMultiplyCount();
            int remainingSlots = hand.NumberCards.Count - 1 - multiplyCount;

            if (remainingSlots < 0 || (remainingSlots > 0 && availableOperators.Count == 0))
                return new Expression();

            // 완전 탐색 시작
            var numbers = hand.NumberCards.Select(c => c.Value).ToList();
            PermuteNumbers(numbers, new List<int>(), new Dictionary<int, int>());

            return bestExpression.Clone();
        }

        /// <summary>
        /// 1단계: 숫자 순열 생성
        /// </summary>
        private void PermuteNumbers(List<int> remaining, List<int> current, Dictionary<int, int> used)
        {
            // 모든 숫자를 배치했으면 다음 단계
            if (current.Count == currentHand.NumberCards.Count)
            {
                DistributeSquareRoots(current, 0, new List<int>());
                return;
            }

            // 각 숫자별로 사용 가능한 개수 세기
            var numberCounts = new Dictionary<int, int>();
            foreach (var num in remaining)
            {
                if (!numberCounts.ContainsKey(num))
                    numberCounts[num] = 0;
                numberCounts[num]++;
            }

            // 각 고유 숫자 시도
            foreach (var kvp in numberCounts)
            {
                int number = kvp.Key;

                // 이 숫자를 사용할 수 있는지 확인
                int usedCount = used.ContainsKey(number) ? used[number] : 0;
                if (usedCount >= kvp.Value)
                    continue;

                // 백트래킹
                current.Add(number);
                used[number] = usedCount + 1;

                PermuteNumbers(remaining, current, used);

                current.RemoveAt(current.Count - 1);
                used[number] = usedCount;
            }
        }

        /// <summary>
        /// 2단계: 제곱근 분배
        /// </summary>
        private void DistributeSquareRoots(List<int> numbers, int index, List<int> sqrtCounts)
        {
            // 모든 숫자에 √ 분배 완료
            if (index == numbers.Count)
            {
                // √ 총 개수 확인
                int totalSqrt = sqrtCounts.Sum();
                if (totalSqrt == currentHand.GetSquareRootCount())
                {
                    EnumerateOperators(numbers, sqrtCounts);
                }
                return;
            }

            // 현재 숫자에 √를 0개~남은개수만큼 시도
            int remaining = currentHand.GetSquareRootCount() - sqrtCounts.Sum();
            for (int count = 0; count <= remaining; count++)
            {
                sqrtCounts.Add(count);
                DistributeSquareRoots(numbers, index + 1, sqrtCounts);
                sqrtCounts.RemoveAt(sqrtCounts.Count - 1);
            }
        }

        /// <summary>
        /// 3단계: 연산자 배치
        /// </summary>
        private void EnumerateOperators(List<int> numbers, List<int> sqrtCounts)
        {
            // ✅ 수정: 사용 가능한 연산자 리스트 전달
            AssignOperators(numbers, sqrtCounts, new List<OperatorCard.OperatorType>(),
                           new List<OperatorCard.OperatorType>(availableOperators), 0, 0);
        }

        /// <summary>
        /// ✅ 완전히 수정: 연산자를 한 번씩만 사용하도록 재귀 배치
        /// </summary>
        private void AssignOperators(List<int> numbers, List<int> sqrtCounts,
            List<OperatorCard.OperatorType> operators,
            List<OperatorCard.OperatorType> remainingOperators, // ✅ 추가: 남은 연산자 추적
            int index, int multiplyUsed)
        {
            int totalSlots = numbers.Count - 1;
            int multiplyNeeded = currentHand.GetMultiplyCount();
            int slotsRemaining = totalSlots - index;
            int multiplyRemaining = multiplyNeeded - multiplyUsed;

            // 가지치기: 남은 슬롯에 × 를 다 못 채우면 중단
            if (multiplyRemaining > slotsRemaining)
                return;

            // 모든 슬롯 채움
            if (index == totalSlots)
            {
                if (multiplyUsed == multiplyNeeded)
                {
                    BuildAndEvaluate(numbers, sqrtCounts, operators);
                }
                return;
            }

            // × 배치 시도 (아직 필요하면)
            if (multiplyUsed < multiplyNeeded)
            {
                operators.Add(OperatorCard.OperatorType.Multiply);
                AssignOperators(numbers, sqrtCounts, operators, remainingOperators,
                               index + 1, multiplyUsed + 1);
                operators.RemoveAt(operators.Count - 1);
            }

            // ✅ 수정: 기본 연산자 배치 (한 번씩만 사용)
            for (int i = 0; i < remainingOperators.Count; i++)
            {
                var op = remainingOperators[i];

                // 이 연산자를 사용
                operators.Add(op);

                // ✅ 핵심: 이 연산자를 제거한 새 리스트 생성
                var newRemaining = new List<OperatorCard.OperatorType>(remainingOperators);
                newRemaining.RemoveAt(i);

                // 재귀 호출
                AssignOperators(numbers, sqrtCounts, operators, newRemaining,
                               index + 1, multiplyUsed);

                // 백트래킹
                operators.RemoveAt(operators.Count - 1);
            }
        }

        /// <summary>
        /// 4단계: 수식 구성 및 평가
        /// </summary>
        private void BuildAndEvaluate(List<int> numbers, List<int> sqrtCounts,
            List<OperatorCard.OperatorType> operators)
        {
            // Expression 객체 구성
            Expression expr = new Expression();

            for (int i = 0; i < numbers.Count; i++)
            {
                // √ 적용 여부
                bool hasRoot = sqrtCounts[i] > 0;
                expr.AddNumber(numbers[i], hasRoot);

                // 연산자 추가 (마지막이 아니면)
                if (i < operators.Count)
                {
                    expr.AddOperator(operators[i]);
                }
            }

            // 검증
            var validation = ExpressionValidator.Validate(expr, currentHand);
            if (!validation.IsValid)
                return;

            // 계산
            var evaluation = MathHighLow.Models.ExpressionEvaluator.Evaluate(expr);
            if (!evaluation.Success)
                return;

            // 목표값과의 거리 계산
            float distance = Mathf.Abs(evaluation.Value - targetNumber);

            // 더 좋은 결과면 저장
            if (distance < bestDistance)
            {
                bestDistance = distance;
                bestExpression = expr.Clone();
            }
        }
    }
}