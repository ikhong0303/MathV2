using System;
using MathHighLow.Models;

namespace MathHighLow.Services
{
    /// <summary>
    /// ✅ 수정: 타이머 및 제출 가능 여부 이벤트 추가
    /// </summary>
    public static class GameEvents
    {
        // ===== 카드 관련 이벤트 =====

        public static event Action<Card> OnCardClicked;
        public static event Action<Card, bool> OnCardAdded; // Card, isPlayer

        // ===== 연산자 관련 이벤트 =====

        public static event Action<OperatorCard.OperatorType> OnOperatorSelected;
        public static event Action OnSquareRootClicked;
        public static event Action<OperatorCard.OperatorType> OnOperatorDisabled;

        // ===== 게임 진행 이벤트 =====

        public static event Action OnRoundStarted;
        public static event Action<RoundResult> OnRoundEnded;
        public static event Action OnSubmitClicked;
        public static event Action OnResetClicked;

        // ===== 설정 관련 이벤트 =====

        public static event Action<int> OnTargetSelected;
        public static event Action<int> OnBetChanged;

        // ===== 점수 관련 이벤트 =====

        public static event Action<int, int> OnScoreChanged; // playerScore, aiScore
        public static event Action<string> OnGameOver; // winner
        public static event Action<string> OnExpressionUpdated; // 수식 텍스트가 변경됨

        // ===== ✅ 추가: 타이머 및 제출 가능 여부 =====

        /// <summary>
        /// 타이머가 업데이트되었을 때
        /// </summary>
        public static event Action<float, float> OnTimerUpdated; // currentTime, maxTime

        /// <summary>
        /// 제출 가능 여부가 변경되었을 때
        /// </summary>
        public static event Action<bool> OnSubmitAvailabilityChanged; // canSubmit

        /// <summary>
        /// ✅ 추가: 상태 텍스트 업데이트
        /// </summary>
        public static event Action<string> OnStatusTextUpdated; // statusMessage

        // ===== 유틸리티 메서드 =====

        public static void ClearAllEvents()
        {
            OnCardClicked = null;
            OnCardAdded = null;
            OnOperatorSelected = null;
            OnSquareRootClicked = null;
            OnOperatorDisabled = null;
            OnRoundStarted = null;
            OnRoundEnded = null;
            OnSubmitClicked = null;
            OnResetClicked = null;
            OnTargetSelected = null;
            OnBetChanged = null;
            OnScoreChanged = null;
            OnGameOver = null;
            OnExpressionUpdated = null;
            OnTimerUpdated = null; // ✅ 추가
            OnSubmitAvailabilityChanged = null; // ✅ 추가
            OnStatusTextUpdated = null; // ✅ 추가
        }

        // ===== 이벤트 발행 메서드 =====

        public static void InvokeExpressionUpdated(string expressionText)
        {
            OnExpressionUpdated?.Invoke(expressionText);
        }

        public static void InvokeCardClicked(Card card)
        {
            OnCardClicked?.Invoke(card);
        }

        public static void InvokeOperatorSelected(OperatorCard.OperatorType op)
        {
            OnOperatorSelected?.Invoke(op);
        }

        public static void InvokeSquareRootClicked()
        {
            OnSquareRootClicked?.Invoke();
        }

        public static void InvokeSubmit()
        {
            OnSubmitClicked?.Invoke();
        }

        public static void InvokeReset()
        {
            OnResetClicked?.Invoke();
        }

        public static void InvokeTargetSelected(int target)
        {
            OnTargetSelected?.Invoke(target);
        }

        public static void InvokeBetChanged(int bet)
        {
            OnBetChanged?.Invoke(bet);
        }

        public static void InvokeRoundStarted()
        {
            OnRoundStarted?.Invoke();
        }

        public static void InvokeRoundEnded(RoundResult result)
        {
            OnRoundEnded?.Invoke(result);
        }

        public static void InvokeScoreChanged(int playerScore, int aiScore)
        {
            OnScoreChanged?.Invoke(playerScore, aiScore);
        }

        public static void InvokeGameOver(string winner)
        {
            OnGameOver?.Invoke(winner);
        }

        public static void InvokeCardAdded(Card card, bool isPlayer)
        {
            OnCardAdded?.Invoke(card, isPlayer);
        }

        public static void InvokeOperatorDisabled(OperatorCard.OperatorType op)
        {
            OnOperatorDisabled?.Invoke(op);
        }

        // ===== ✅ 추가: 새로운 이벤트 발행 메서드 =====

        /// <summary>
        /// 타이머 업데이트 이벤트 발행
        /// </summary>
        public static void InvokeTimerUpdated(float currentTime, float maxTime)
        {
            OnTimerUpdated?.Invoke(currentTime, maxTime);
        }

        /// <summary>
        /// 제출 가능 여부 변경 이벤트 발행
        /// </summary>
        public static void InvokeSubmitAvailabilityChanged(bool canSubmit)
        {
            OnSubmitAvailabilityChanged?.Invoke(canSubmit);
        }

        /// <summary>
        /// ✅ 추가: 상태 텍스트 업데이트 이벤트 발행
        /// </summary>
        public static void InvokeStatusTextUpdated(string statusMessage)
        {
            OnStatusTextUpdated?.Invoke(statusMessage);
        }
    }
}