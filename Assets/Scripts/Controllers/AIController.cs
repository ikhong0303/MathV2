using UnityEngine;
using MathHighLow.Models;
using System.Collections.Generic;
using System.Linq;

namespace MathHighLow.Controllers
{
    /// <summary>
    /// [학습 포인트] AI 완전 탐색 알고리즘
    /// 
    /// 모든 가능한 수식 조합을 시도하여 최적해를 찾습니다.
    /// 원본 AiSolver.cs를 리팩토링 버전에 맞게 통합했습니다.
    /// 
    /// 알고리즘:
    /// 1. 숫자 순열 생성 (Backtracking)
    /// 2. 제곱근 분배 (Recursion)
    /// 3. 연산자 배치 (Constraint Satisfaction)
    /// 4. 전체 평가 및 최적해 선택
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
        /// 
        /// [학습 포인트] 백트래킹 알고리즘
        /// 가능한 모든 숫자 배치를 시도합니다.
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
        /// 
        /// [학습 포인트] 재귀적 조합 생성
        /// 각 숫자에 √를 몇 개 적용할지 결정합니다.
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
        /// 
        /// [학습 포인트] 제약 조건 만족 문제
        /// × 카드 개수를 맞추면서 연산자를 배치합니다.
        /// </summary>
        private void EnumerateOperators(List<int> numbers, List<int> sqrtCounts)
        {
            AssignOperators(numbers, sqrtCounts, new List<OperatorCard.OperatorType>(), 0, 0);
        }

        /// <summary>
        /// 연산자를 재귀적으로 배치합니다.
        /// </summary>
        private void AssignOperators(List<int> numbers, List<int> sqrtCounts,
            List<OperatorCard.OperatorType> operators, int index, int multiplyUsed)
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
                AssignOperators(numbers, sqrtCounts, operators, index + 1, multiplyUsed + 1);
                operators.RemoveAt(operators.Count - 1);
            }

            // 기본 연산자 배치 시도
            foreach (var op in availableOperators)
            {
                operators.Add(op);
                AssignOperators(numbers, sqrtCounts, operators, index + 1, multiplyUsed);
                operators.RemoveAt(operators.Count - 1);
            }
        }

        /// <summary>
        /// 4단계: 수식 구성 및 평가
        /// 
        /// [학습 포인트] 최적화 탐색
        /// 각 조합을 평가하여 목표값에 가장 가까운 것을 저장합니다.
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

            // 목표값과의 거리 계산 (Unity Mathf 사용)
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