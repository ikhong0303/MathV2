using UnityEngine;
using System.Collections.Generic;
using MathHighLow.Models;
using MathHighLow.Services;
using System.Linq;

namespace MathHighLow.Controllers
{
    /// <summary>
    /// [학습 포인트] 이벤트 구독 및 모델 업데이트
    /// 
    /// 플레이어의 입력을 받아(Events)
    /// 수식 데이터(Expression)를 관리(Update Model)합니다.
    /// UI(View)와 직접 통신하지 않고, GameEvents를 통해 상호작용합니다.
    /// </summary>
    public class PlayerController : MonoBehaviour
    {
        // --- 현재 라운드 상태 ---

        /// <summary>
        /// 이번 라운드에 받은 손패
        /// </summary>
        private Hand currentHand;

        /// <summary>
        /// 플레이어가 현재 조립 중인 수식
        /// </summary>
        private Expression currentExpression;

        // --- 카드 사용량 추적 ---

        /// <summary>
        /// 사용한 숫자 카드를 추적합니다. (중복 사용 방지)
        /// Key: 손패의 NumberCard 객체, Value: 사용 여부
        /// </summary>
        private Dictionary<NumberCard, bool> usedNumberCards;

        /// <summary>
        /// 사용한 √ 카드 개수
        /// </summary>
        private int usedSqrtCount;

        /// <summary>
        /// 사용한 × 카드 개수
        /// </summary>
        private int usedMultiplyCount;

        /// <summary>
        /// √ 버튼을 방금 눌렀는지 여부 (다음 숫자에 적용)
        /// </summary>
        private bool nextNumberHasSqrt;

        /// <summary>
        /// 컨트롤러 초기화 (GameController가 호출)
        /// </summary>
        public void Initialize()
        {
            currentExpression = new Expression();
            usedNumberCards = new Dictionary<NumberCard, bool>();
        }

        #region Unity 생명주기 및 이벤트 구독

        void OnEnable()
        {
            // [학습 포인트] 이벤트 구독
            // UI(View)나 다른 시스템에서 발행하는 이벤트를 수신합니다.

            // 라운드 시작/리셋
            GameEvents.OnRoundStarted += HandleRoundStarted;
            GameEvents.OnResetClicked += HandleResetClicked;

            // 카드/연산자 입력
            GameEvents.OnCardClicked += HandleCardClicked;
            GameEvents.OnOperatorSelected += HandleOperatorSelected;
            GameEvents.OnSquareRootClicked += HandleSquareRootClicked;
        }

        void OnDisable()
        {
            // [학습 포인트] 이벤트 구독 해제
            GameEvents.OnRoundStarted -= HandleRoundStarted;
            GameEvents.OnResetClicked -= HandleResetClicked;

            GameEvents.OnCardClicked -= HandleCardClicked;
            GameEvents.OnOperatorSelected -= HandleOperatorSelected;
            GameEvents.OnSquareRootClicked -= HandleSquareRootClicked;
        }

        #endregion

        #region 공개 메서드 (다른 컨트롤러가 호출)

        /// <summary>
        /// RoundController가 새 라운드의 손패를 설정합니다.
        /// </summary>
        public void SetHand(Hand hand)
        {
            currentHand = hand;
            ResetExpressionState(); // 손패가 바뀌었으니 수식 초기화
        }

        /// <summary>
        /// RoundController가 평가를 위해 현재 수식을 가져갑니다.
        /// </summary>
        public Expression GetExpression()
        {
            // [학습 포인트] 방어적 복사 (Defensive Copy)
            // 원본이 아닌 복제본을 전달하여, 외부에서 수식을 수정하는 것을 방지합니다.
            return currentExpression.Clone();
        }

        #endregion

        #region 이벤트 핸들러

        /// <summary>
        /// 새 라운드 시작 시 호출됩니다.
        /// </summary>
        private void HandleRoundStarted()
        {
            ResetExpressionState();
            GameEvents.InvokeExpressionUpdated(""); // [추가] UI 텍스트 초기화
        }

        /// <summary>
        /// 리셋 버튼 클릭 시 호출됩니다.
        /// </summary>
        private void HandleResetClicked()
        {
            ResetExpressionState();
            GameEvents.InvokeExpressionUpdated(""); // [추가] UI 텍스트 초기화
            // (선택 사항) UI에게도 수식이 리셋되었음을 알릴 수 있습니다.
            // GameEvents.InvokeExpressionReset();
        }

        /// <summary>
        /// 카드 클릭 이벤트 처리 (현재는 숫자 카드만 클릭 가능하다고 가정)
        /// </summary>
        private void HandleCardClicked(Card card)
        {
            // 1. 숫자 카드가 아니면 무시
            if (card is not NumberCard numberCard) return;

            // 2. 손패에 없는 카드거나, 이미 사용한 카드면 무시
            if (!currentHand.NumberCards.Contains(numberCard) ||
                (usedNumberCards.ContainsKey(numberCard) && usedNumberCards[numberCard]))
            {
                return;
            }

            // 3. 수식 상태가 '숫자'를 기대하는 상태가 아니면 무시
            if (!currentExpression.ExpectingNumber())
            {
                return;
            }

            // [로직] 수식에 숫자 추가
            usedNumberCards[numberCard] = true;
            currentExpression.AddNumber(numberCard.Value, nextNumberHasSqrt);

            // √ 플래그 초기화
            nextNumberHasSqrt = false;
            GameEvents.InvokeExpressionUpdated(currentExpression.ToDisplayString()); // [추가]
            // (선택 사항) UI 업데이트를 위해 이벤트 발행
            // GameEvents.InvokeExpressionUpdated(currentExpression.ToDisplayString());
        }

        /// <summary>
        /// 연산자 버튼 클릭 이벤트 처리
        /// </summary>
        private void HandleOperatorSelected(OperatorCard.OperatorType op)
        {
            // 1. 수식 상태가 '연산자'를 기대하는 상태가 아니면 무시
            if (currentExpression.ExpectingNumber() || currentExpression.IsEmpty())
            {
                return;
            }

            // 2. √ 플래그 리셋 (연산자가 눌리면 √는 취소됨)
            nextNumberHasSqrt = false;

            // 3. 사용 가능 여부 확인 (비활성화 또는 사용량 초과)
            if (op == OperatorCard.OperatorType.Multiply)
            {
                if (usedMultiplyCount >= currentHand.GetMultiplyCount()) return; // × 개수 초과
                usedMultiplyCount++;
            }
            else
            {
                if (!currentHand.IsOperatorEnabled(op)) return; // 비활성화된 연산자
            }

            // [로직] 수식에 연산자 추가
            currentExpression.AddOperator(op);

            // (선택 사항) UI 업데이트를 위해 이벤트 발행
            // GameEvents.InvokeExpressionUpdated(currentExpression.ToDisplayString());
        }

        /// <summary>
        /// 제곱근(√) 버튼 클릭 이벤트 처리
        /// </summary>
        private void HandleSquareRootClicked()
        {
            // 1. 수식 상태가 '숫자'를 기대하는 상태가 아니면 무시
            if (!currentExpression.ExpectingNumber())
            {
                return;
            }

            // 2. √ 개수 초과 시 무시
            if (usedSqrtCount >= currentHand.GetSquareRootCount())
            {
                return;
            }

            // 3. 이미 √를 누른 상태면 무시 (중복 방지)
            if (nextNumberHasSqrt) return;

            // [로직] 다음 숫자에 √를 적용하도록 플래그 설정
            nextNumberHasSqrt = true;
            usedSqrtCount++;

            // UI 업데이트를 위해 이벤트 발행
            string currentText = currentExpression.ToDisplayString();
            string prefix = currentText.Length > 0 ? " " : "";
            GameEvents.InvokeExpressionUpdated(currentText + prefix + "√");
            // GameEvents.InvokeExpressionUpdated(currentExpression.ToDisplayString() + " (√ 대기)");
        }

        #endregion

        #region 내부 유틸리티

        /// <summary>
        /// 수식과 관련된 모든 상태를 초기화합니다.
        /// (새 라운드 시작 또는 리셋 시 사용)
        /// </summary>
        private void ResetExpressionState()
        {
            currentExpression.Clear();
            usedNumberCards.Clear();
            usedSqrtCount = 0;
            usedMultiplyCount = 0;
            nextNumberHasSqrt = false;

            // 손패에 있는 모든 숫자 카드를 "사용 안 함"으로 초기화
            if (currentHand != null)
            {
                foreach (var card in currentHand.NumberCards)
                {
                    usedNumberCards[card] = false;
                }
            }
        }

        #endregion
    }
}