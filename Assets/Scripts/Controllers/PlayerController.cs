using UnityEngine;
using System.Collections.Generic;
using MathHighLow.Models;
using MathHighLow.Services;
using System.Linq;

namespace MathHighLow.Controllers
{
    /// <summary>
    /// ✅ 새로운 구조: 모든 카드를 클릭으로 처리
    /// 
    /// - 숫자 카드 클릭: 수식에 숫자 추가
    /// - 연산자 카드 클릭: 수식에 연산자 추가
    /// - 특수 카드 (×, √): 자동으로 처리 (클릭 불가)
    /// 
    /// UI 버튼 없음! 오직 카드만 클릭!
    /// </summary>
    public class PlayerController : MonoBehaviour
    {
        // --- 현재 라운드 상태 ---
        private Hand currentHand;
        private Expression currentExpression;

        // --- 카드 사용량 추적 ---
        private Dictionary<Card, bool> usedCards; // 모든 카드 추적 (숫자 + 연산자)

        /// <summary>
        /// 컨트롤러 초기화
        /// </summary>
        public void Initialize()
        {
            currentExpression = new Expression();
            usedCards = new Dictionary<Card, bool>();
        }

        #region Unity 생명주기 및 이벤트 구독

        void OnEnable()
        {
            GameEvents.OnRoundStarted += HandleRoundStarted;
            GameEvents.OnResetClicked += HandleResetClicked;
            GameEvents.OnCardClicked += HandleCardClicked;
        }

        void OnDisable()
        {
            GameEvents.OnRoundStarted -= HandleRoundStarted;
            GameEvents.OnResetClicked -= HandleResetClicked;
            GameEvents.OnCardClicked -= HandleCardClicked;
        }

        #endregion

        #region 공개 메서드

        public void SetHand(Hand hand)
        {
            currentHand = hand;
            ResetExpressionState();
        }

        public Expression GetExpression()
        {
            return currentExpression.Clone();
        }

        #endregion

        #region 이벤트 핸들러

        private void HandleRoundStarted()
        {
            ResetExpressionState();
            GameEvents.InvokeExpressionUpdated("");
        }

        private void HandleResetClicked()
        {
            ResetExpressionState();
            GameEvents.InvokeExpressionUpdated("");
        }

        /// <summary>
        /// ✅ 완전히 새로운 카드 클릭 처리
        /// - 숫자 카드 클릭 → 수식에 숫자 추가
        /// - 연산자 카드 클릭 → 수식에 연산자 추가
        /// - 특수 카드 클릭 → 무시 (자동 처리됨)
        /// </summary>
        private void HandleCardClicked(Card card)
        {
            // 1. null 체크
            if (card == null)
            {
                Debug.LogWarning("[PlayerController] 클릭된 카드가 null입니다.");
                return;
            }

            // 2. currentHand null 체크
            if (currentHand == null)
            {
                Debug.LogWarning("[PlayerController] 손패가 설정되지 않았습니다.");
                return;
            }

            // 3. 이미 사용한 카드인지 확인
            if (usedCards.ContainsKey(card) && usedCards[card])
            {
                Debug.Log("[PlayerController] 이미 사용한 카드입니다.");
                return;
            }

            // 4. 카드 타입별 처리
            if (card is NumberCard numberCard)
            {
                HandleNumberCardClicked(numberCard);
            }
            else if (card is OperatorCard operatorCard)
            {
                HandleOperatorCardClicked(operatorCard);
            }
            else if (card is SpecialCard)
            {
                // 특수 카드는 자동으로 처리되므로 클릭 불가
                Debug.Log("[PlayerController] 특수 카드는 클릭할 수 없습니다.");
            }
        }

        /// <summary>
        /// ✅ 숫자 카드 클릭 처리
        /// </summary>
        private void HandleNumberCardClicked(NumberCard numberCard)
        {
            // 1. 수식이 숫자를 기대하는 상태가 아니면 무시
            if (!currentExpression.ExpectingNumber())
            {
                Debug.Log("[PlayerController] 지금은 연산자를 선택해야 합니다.");
                return;
            }

            // 2. 손패에 있는 카드인지 확인
            if (!currentHand.NumberCards.Contains(numberCard))
            {
                Debug.LogWarning("[PlayerController] 손패에 없는 숫자 카드입니다.");
                return;
            }

            // 3. 수식에 숫자 추가 (√는 자동으로 처리되므로 false)
            currentExpression.AddNumber(numberCard.Value, false);
            usedCards[numberCard] = true;

            // 4. UI 업데이트
            GameEvents.InvokeExpressionUpdated(currentExpression.ToDisplayString());

            Debug.Log($"[PlayerController] 숫자 추가: {numberCard.Value}");
        }

        /// <summary>
        /// ✅ 연산자 카드 클릭 처리 (새로운 기능!)
        /// </summary>
        private void HandleOperatorCardClicked(OperatorCard operatorCard)
        {
            // 1. 수식이 연산자를 기대하는 상태가 아니면 무시
            if (currentExpression.ExpectingNumber() || currentExpression.IsEmpty())
            {
                Debug.Log("[PlayerController] 지금은 숫자를 선택해야 합니다.");
                return;
            }

            // 2. 손패에 연산자 카드가 있는지 확인
            // (새 구조에서는 기본 연산자를 손패에 카드로 받음)
            // Hand에 OperatorCard 리스트가 필요함 → Hand.cs 수정 필요!

            // 임시: 기본 연산자는 항상 사용 가능하다고 가정
            bool canUseOperator = true;

            // × 연산자는 특별 처리 (특수 카드로 받아야 함)
            if (operatorCard.Operator == OperatorCard.OperatorType.Multiply)
            {
                // ×는 특수 카드에서만 사용 가능
                int multiplyCount = currentHand.GetMultiplyCount();
                if (multiplyCount == 0)
                {
                    Debug.Log("[PlayerController] × 카드가 손패에 없습니다.");
                    return;
                }
            }

            if (!canUseOperator)
            {
                Debug.Log("[PlayerController] 사용할 수 없는 연산자입니다.");
                return;
            }

            // 3. 수식에 연산자 추가
            currentExpression.AddOperator(operatorCard.Operator);
            usedCards[operatorCard] = true;

            // 4. UI 업데이트
            GameEvents.InvokeExpressionUpdated(currentExpression.ToDisplayString());

            Debug.Log($"[PlayerController] 연산자 추가: {operatorCard.GetDisplayText()}");
        }

        #endregion

        #region 내부 유틸리티

        private void ResetExpressionState()
        {
            currentExpression.Clear();
            usedCards.Clear();

            // 손패의 모든 카드를 "사용 안 함"으로 초기화
            if (currentHand != null)
            {
                // 숫자 카드
                foreach (var card in currentHand.NumberCards)
                {
                    usedCards[card] = false;
                }

                // 연산자 카드도 추가 (Hand.cs에 OperatorCards 리스트 필요)
                // 현재는 기본 연산자만 있으므로 생략
            }
        }

        #endregion
    }
}