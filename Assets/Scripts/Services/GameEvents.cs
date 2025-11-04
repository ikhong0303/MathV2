using System;
using MathHighLow.Models;

namespace MathHighLow.Services
{
    /// <summary>
    /// [학습 포인트] 이벤트 기반 아키텍처
    /// 
    /// 게임 전체에서 사용하는 이벤트를 정의합니다.
    /// 이벤트를 사용하면 클래스 간의 의존성을 낮출 수 있습니다.
    /// 
    /// 사용 예시:
    /// - 발행: GameEvents.OnCardClicked?.Invoke(card);
    /// - 구독: GameEvents.OnCardClicked += HandleCardClick;
    /// 
    /// 실습 과제:
    /// 1. 새로운 이벤트 추가해보기
    /// 2. 이벤트 로깅 시스템 만들기
    /// </summary>
    public static class GameEvents
    {
        // ===== 카드 관련 이벤트 =====
        
        /// <summary>
        /// 카드가 클릭되었을 때
        /// </summary>
        public static event Action<Card> OnCardClicked;

        /// <summary>
        /// 카드가 손패에 추가되었을 때
        /// </summary>
        public static event Action<Card, bool> OnCardAdded; // Card, isPlayer

        // ===== 연산자 관련 이벤트 =====
        
        /// <summary>
        /// 연산자가 선택되었을 때
        /// </summary>
        public static event Action<OperatorCard.OperatorType> OnOperatorSelected;

        /// <summary>
        /// 제곱근 버튼이 클릭되었을 때
        /// </summary>
        public static event Action OnSquareRootClicked;

        /// <summary>
        /// 연산자가 비활성화되었을 때
        /// </summary>
        public static event Action<OperatorCard.OperatorType> OnOperatorDisabled;

        // ===== 게임 진행 이벤트 =====
        
        /// <summary>
        /// 라운드가 시작되었을 때
        /// </summary>
        public static event Action OnRoundStarted;

        /// <summary>
        /// 라운드가 종료되었을 때
        /// </summary>
        public static event Action<RoundResult> OnRoundEnded;

        /// <summary>
        /// 제출 버튼이 클릭되었을 때
        /// </summary>
        public static event Action OnSubmitClicked;

        /// <summary>
        /// 리셋 버튼이 클릭되었을 때
        /// </summary>
        public static event Action OnResetClicked;

        // ===== 설정 관련 이벤트 =====
        
        /// <summary>
        /// 목표값이 선택되었을 때
        /// </summary>
        public static event Action<int> OnTargetSelected;

        /// <summary>
        /// 베팅 금액이 변경되었을 때
        /// </summary>
        public static event Action<int> OnBetChanged;

        // ===== 점수 관련 이벤트 =====
        
        /// <summary>
        /// 점수가 변경되었을 때
        /// </summary>
        public static event Action<int, int> OnScoreChanged; // playerScore, aiScore

        /// <summary>
        /// 게임이 종료되었을 때 (누군가 돈이 떨어짐)
        /// </summary>
        public static event Action<string> OnGameOver; // winner
        public static event Action<string> OnExpressionUpdated; // 수식 텍스트가 변경됨


        // ===== 유틸리티 메서드 =====

        /// <summary>
        /// 모든 이벤트를 초기화합니다.
        /// 씬 전환 시 호출하여 메모리 누수를 방지합니다.
        /// </summary>
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
        }

        /// <summary>
        /// 이벤트를 안전하게 발행합니다.
        /// null 체크를 자동으로 수행합니다.
        /// </summary>
        /// 

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
    }
}
