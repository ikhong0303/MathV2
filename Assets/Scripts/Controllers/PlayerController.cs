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
    /// - × 특수 카드: 클릭하여 다음 연산자를 곱하기로 전환
    /// - √ 특수 카드: 클릭하여 다음 숫자에 제곱근 적용
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
        private bool isSubmitAvailable;
        private bool isSquareRootPending;
        private SpecialCard pendingSquareRootCard;

        /// <summary>
        /// 컨트롤러 초기화
        /// </summary>
        public void Initialize()
        {
            currentExpression = new Expression();
            usedCards = new Dictionary<Card, bool>();
            isSubmitAvailable = false;
            isSquareRootPending = false;
            pendingSquareRootCard = null;
        }

        #region Unity 생명주기 및 이벤트 구독

        void OnEnable()
        {
            GameEvents.OnRoundStarted += HandleRoundStarted;
            GameEvents.OnResetClicked += HandleResetClicked;
            GameEvents.OnCardClicked += HandleCardClicked;
            GameEvents.OnSubmitAvailabilityChanged += HandleSubmitAvailabilityChanged;
        }

        void OnDisable()
        {
            GameEvents.OnRoundStarted -= HandleRoundStarted;
            GameEvents.OnResetClicked -= HandleResetClicked;
            GameEvents.OnCardClicked -= HandleCardClicked;
            GameEvents.OnSubmitAvailabilityChanged -= HandleSubmitAvailabilityChanged;
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

        private void HandleSubmitAvailabilityChanged(bool canSubmit)
        {
            isSubmitAvailable = canSubmit;

            ShowCompletionStatus();
        }

        /// <summary>
        /// ✅ 완전히 새로운 카드 클릭 처리
        /// - 숫자 카드 클릭 → 수식에 숫자 추가
        /// - 연산자 카드 클릭 → 수식에 연산자 추가
        /// - 특수 카드 클릭 → ×, √ 카드 모두 수동 처리
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
            else if (card is SpecialCard specialCard)
            {
                HandleSpecialCardClicked(specialCard);
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
                GameEvents.InvokeStatusTextUpdated("지금은 연산자 카드를 눌러주세요.");
                return;
            }

            // 2. 손패에 있는 카드인지 확인
            if (!currentHand.NumberCards.Contains(numberCard))
            {
                Debug.LogWarning("[PlayerController] 손패에 없는 숫자 카드입니다.");
                return;
            }

            bool applySquareRoot = isSquareRootPending && pendingSquareRootCard != null;

            // 3. 수식에 숫자 추가 (필요 시 √ 적용)
            currentExpression.AddNumber(numberCard.Value, applySquareRoot);
            usedCards[numberCard] = true;
            GameEvents.InvokeCardConsumed(numberCard);

            if (applySquareRoot)
            {
                pendingSquareRootCard.Consume();
                usedCards[pendingSquareRootCard] = true;
                GameEvents.InvokeSpecialCardConsumed(pendingSquareRootCard);

                isSquareRootPending = false;
                pendingSquareRootCard = null;

                GameEvents.InvokeStatusTextUpdated("√ 카드가 적용되었습니다. 연산자를 선택하세요.");
            }

            // 4. UI 업데이트
            GameEvents.InvokeExpressionUpdated(currentExpression.ToDisplayString());

            bool hasUnusedNumbers = HasUnusedNumberCards();

            if (!hasUnusedNumbers && !currentExpression.ExpectingNumber())
            {
                ShowCompletionStatus();
            }
            else if (currentExpression.ExpectingNumber())
            {
                GameEvents.InvokeStatusTextUpdated("숫자 카드를 눌러주세요.");
            }
            else
            {
                GameEvents.InvokeStatusTextUpdated("연산자 카드를 눌러주세요.");
            }

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
                GameEvents.InvokeStatusTextUpdated("지금은 숫자 카드를 눌러주세요.");
                return;
            }

            if (!HasUnusedNumberCards())
            {
                Debug.Log("[PlayerController] 사용할 수 있는 숫자 카드가 없어 연산자를 추가할 수 없습니다.");
                GameEvents.InvokeStatusTextUpdated("남은 숫자가 없어 연산자를 더 선택할 수 없습니다. 제출을 준비해 주세요.");
                return;
            }

            // 2. 손패에 연산자 카드가 있는지 확인
            // (새 구조에서는 기본 연산자를 손패에 카드로 받음)
            // Hand에 OperatorCard 리스트가 필요함 → Hand.cs 수정 필요!

            // 임시: 기본 연산자는 항상 사용 가능하다고 가정
            if (!currentHand.OperatorCards.Contains(operatorCard))
            {
                Debug.LogWarning("[PlayerController] 손패에 없는 연산자 카드입니다.");
                return;
            }

            // 3. 수식에 연산자 추가
            OperatorCard.OperatorType operatorToAdd = operatorCard.Operator;
            currentExpression.AddOperator(operatorToAdd);
            usedCards[operatorCard] = true;
            GameEvents.InvokeCardConsumed(operatorCard);

            // 4. UI 업데이트
            GameEvents.InvokeExpressionUpdated(currentExpression.ToDisplayString());

            if (currentExpression.ExpectingNumber())
            {
                GameEvents.InvokeStatusTextUpdated("숫자 카드를 눌러주세요.");
            }

            Debug.Log($"[PlayerController] 연산자 추가: {OperatorToText(operatorToAdd)}");
        }

        #endregion

        #region 내부 유틸리티

        private void ResetExpressionState()
        {
            currentExpression.Clear();
            usedCards.Clear();
            isSubmitAvailable = false;
            isSquareRootPending = false;
            pendingSquareRootCard = null;

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
                foreach (var card in currentHand.OperatorCards)
                {
                    usedCards[card] = false;
                }

                foreach (var card in currentHand.SpecialCards)
                {
                    card.ResetUsage();
                    usedCards[card] = false;
                }
            }
        }

        private void HandleSpecialCardClicked(SpecialCard specialCard)
        {
            if (!currentHand.SpecialCards.Contains(specialCard))
            {
                Debug.LogWarning("[PlayerController] 손패에 없는 특수 카드입니다.");
                return;
            }

            if (specialCard.Type == SpecialCard.SpecialType.Multiply)
            {
                HandleMultiplyCardClicked(specialCard);
                return;
            }

            if (specialCard.Type == SpecialCard.SpecialType.SquareRoot)
            {
                HandleSquareRootCardClicked(specialCard);
                return;
            }

            Debug.Log("[PlayerController] 지원되지 않는 특수 카드입니다.");
        }

        private void HandleMultiplyCardClicked(SpecialCard multiplyCard)
        {
            if (currentExpression.IsEmpty() || currentExpression.ExpectingNumber())
            {
                Debug.Log("[PlayerController] 숫자를 먼저 선택해야 × 카드를 사용할 수 있습니다.");
                GameEvents.InvokeStatusTextUpdated("숫자를 배치한 후에 × 카드를 눌러주세요.");
                return;
            }

            if (!HasUnusedNumberCards())
            {
                Debug.Log("[PlayerController] 사용할 수 있는 숫자 카드가 없습니다.");
                GameEvents.InvokeStatusTextUpdated("남은 숫자가 없어 × 카드를 사용할 수 없습니다.");
                return;
            }

            currentExpression.AddOperator(OperatorCard.OperatorType.Multiply);
            multiplyCard.Consume();
            usedCards[multiplyCard] = true;
            GameEvents.InvokeSpecialCardConsumed(multiplyCard);

            GameEvents.InvokeExpressionUpdated(currentExpression.ToDisplayString());
            GameEvents.InvokeStatusTextUpdated("숫자 카드를 눌러주세요.");
            Debug.Log("[PlayerController] × 카드가 즉시 적용되었습니다.");
        }

        private void HandleSquareRootCardClicked(SpecialCard squareRootCard)
        {
            if (isSquareRootPending)
            {
                Debug.Log("[PlayerController] 이미 √ 카드가 대기 중입니다.");
                GameEvents.InvokeStatusTextUpdated("이미 준비된 √ 카드가 있습니다. 숫자를 선택하세요.");
                return;
            }

            if (!currentExpression.ExpectingNumber())
            {
                Debug.Log("[PlayerController] 지금은 √ 카드를 사용할 수 없습니다.");
                GameEvents.InvokeStatusTextUpdated("연산자를 선택한 후 숫자를 넣을 차례에 √ 카드를 눌러주세요.");
                return;
            }

            bool hasAvailableNumber = currentHand.NumberCards.Any(numberCard =>
            {
                if (!usedCards.ContainsKey(numberCard))
                {
                    usedCards[numberCard] = false;
                }

                return !usedCards[numberCard];
            });

            if (!hasAvailableNumber)
            {
                Debug.Log("[PlayerController] 사용할 수 있는 숫자 카드가 없습니다.");
                GameEvents.InvokeStatusTextUpdated("남은 숫자 카드가 없어 √ 카드를 사용할 수 없습니다.");
                return;
            }

            isSquareRootPending = true;
            pendingSquareRootCard = squareRootCard;

            GameEvents.InvokeStatusTextUpdated("다음에 선택하는 숫자에 √가 적용됩니다. 숫자를 골라주세요.");
            Debug.Log("[PlayerController] √ 카드가 준비되었습니다.");
        }

        private string OperatorToText(OperatorCard.OperatorType op)
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

        private bool HasUnusedNumberCards()
        {
            if (currentHand == null)
            {
                return false;
            }

            foreach (var card in currentHand.NumberCards)
            {
                if (!usedCards.TryGetValue(card, out bool used) || !used)
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// 30초 이전에도 특수 카드 사용을 안내해야 하는지 여부를 확인합니다.
        /// </summary>
        public bool NeedsSpecialCardUsageReminder()
        {
            if (currentHand == null || currentExpression == null)
            {
                return false;
            }

            if (currentExpression.IsEmpty())
            {
                return false;
            }

            bool expressionComplete = !HasUnusedNumberCards() && !currentExpression.ExpectingNumber();

            if (!expressionComplete)
            {
                return false;
            }

            return !HasUsedRequiredSpecialCards();
        }

        private void ShowCompletionStatus()
        {
            if (HasUnusedNumberCards() || currentExpression == null || currentExpression.ExpectingNumber())
            {
                return;
            }

            if (!HasUsedRequiredSpecialCards())
            {
                GameEvents.InvokeStatusTextUpdated("제출하려면 받은 √와 × 카드를 모두 사용해야 합니다.");
                return;
            }

            if (isSubmitAvailable)
            {
                GameEvents.InvokeStatusTextUpdated("수식을 완성했습니다. 제출 버튼으로 확인해 보세요.");
            }
            else
            {
                GameEvents.InvokeStatusTextUpdated("30초가 지나면 제출 버튼이 활성화됩니다.");
            }
        }

        /// <summary>
        /// 현재 수식이 제출 조건(모든 √, × 특수 카드 사용)을 만족하는지 확인합니다.
        /// </summary>
        public bool HasUsedRequiredSpecialCards()
        {
            if (currentHand == null || currentExpression == null)
            {
                return false;
            }

            if (currentExpression.IsEmpty())
            {
                return false;
            }

            foreach (var specialCard in currentHand.SpecialCards)
            {
                if ((specialCard.Type == SpecialCard.SpecialType.Multiply ||
                     specialCard.Type == SpecialCard.SpecialType.SquareRoot) &&
                    !specialCard.IsConsumed)
                {
                    return false;
                }
            }

            return true;
        }

        #endregion
    }
}
