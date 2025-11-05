using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using MathHighLow.Models;
using MathHighLow.Services;

namespace MathHighLow.Controllers
{
    /// <summary>
    /// [학습 포인트] 코루틴을 이용한 상태 머신
    /// 
    /// 한 라운드의 전체 흐름(카드 분배, 대기, 평가, 결과)을 제어합니다.
    /// 
    /// ✅ 새로운 카드 분배 규칙:
    /// 1. 기본 연산자 카드 3장 (+, -, ÷) 자동 제공
    /// 2. 숫자 카드 3장 무조건 보장
    /// 3. 특수 카드(×, √) 나오면 → 숫자 카드 1장 추가
    /// 4. AI도 동일 (남은 카드로)
    /// </summary>
    public class RoundController : MonoBehaviour
    {
        // --- 의존성 (GameController로부터 주입받음) ---
        private GameConfig config;
        private DeckService deckService;

        // --- 컨트롤러 참조 (GameController가 관리) ---
        [HideInInspector]
        public PlayerController playerController;
        [HideInInspector]
        public AIController aiController;

        // --- 라운드 상태 ---
        private Hand playerHand;
        private Hand aiHand;
        private int currentTarget;
        private int currentBet;

        private enum RoundPhase
        {
            Idle,
            Dealing,    // 카드 분배 중
            Waiting,    // 플레이어 입력 대기
            Evaluating, // 수식 평가 중
            Results     // 결과 표시 중
        }
        private RoundPhase currentPhase;

        private bool playerSubmitted; // 플레이어 제출했는지
        private float roundTimer;     // 라운드 경과 시간

        /// <summary>
        /// 컨트롤러 초기화 (GameController가 호출)
        /// </summary>
        public void Initialize(GameConfig config, DeckService deckService)
        {
            this.config = config;
            this.deckService = deckService;

            // 라운드에서 사용할 손패 객체 생성
            playerHand = new Hand();
            aiHand = new Hand();
        }

        #region Unity 생명주기 및 이벤트 구독

        void Start()
        {
            playerController = GetComponent<PlayerController>();
            aiController = GetComponent<AIController>();

            if (playerController == null)
            {
                Debug.LogError("[RoundController] PlayerController를 찾을 수 없습니다!");
            }
            if (aiController == null)
            {
                Debug.LogError("[RoundController] AIController를 찾을 수 없습니다!");
            }
        }

        void OnEnable()
        {
            GameEvents.OnSubmitClicked += HandleSubmitClicked;
            GameEvents.OnTargetSelected += HandleTargetSelected;
            GameEvents.OnBetChanged += HandleBetChanged;
        }

        void OnDisable()
        {
            GameEvents.OnSubmitClicked -= HandleSubmitClicked;
            GameEvents.OnTargetSelected -= HandleTargetSelected;
            GameEvents.OnBetChanged -= HandleBetChanged;
        }

        #endregion

        #region 이벤트 핸들러

        private void HandleSubmitClicked()
        {
            if (currentPhase == RoundPhase.Waiting && roundTimer >= config.SubmissionUnlockTime)
            {
                playerSubmitted = true;
            }
        }

        private void HandleTargetSelected(int target)
        {
            currentTarget = target;
        }

        private void HandleBetChanged(int bet)
        {
            currentBet = bet;
        }

        #endregion

        #region 라운드 흐름 제어 (코루틴)

        public void StartNewRound()
        {
            if (currentPhase != RoundPhase.Idle && currentPhase != RoundPhase.Results)
            {
                Debug.LogWarning("[RoundController] 이미 라운드가 진행 중입니다.");
                return;
            }

            StartCoroutine(RoundLoopRoutine());
        }

        private IEnumerator RoundLoopRoutine()
        {
            // --- 1. Dealing (분배) ---
            currentPhase = RoundPhase.Dealing;
            GameEvents.InvokeStatusTextUpdated("카드를 분배합니다...");
            yield return StartCoroutine(DealingPhase());

            // ✅ 추가: 분배 완료 후 안내
            GameEvents.InvokeStatusTextUpdated("수식을 완성하세요.");

            // --- 2. Waiting (대기) ---
            currentPhase = RoundPhase.Waiting;
            yield return StartCoroutine(WaitingPhase());

            // --- 3. Evaluating (평가) ---
            currentPhase = RoundPhase.Evaluating;
            RoundResult result = EvaluatePhase();

            // --- 4. Results (결과) ---
            currentPhase = RoundPhase.Results;
            GameEvents.InvokeRoundEnded(result);
            GameEvents.InvokeStatusTextUpdated("수식 결과를 확인하세요.");

            // ✅ 추가: 결과 확인 타이머
            yield return StartCoroutine(ResultsPhase());

            // ✅ 추가: 자동으로 다음 라운드 시작
            currentPhase = RoundPhase.Idle;
            StartNewRound();
        }

        /// <summary>
        /// ✅ 완전히 새로운 카드 분배 로직
        /// 
        /// 1. 기본 연산자 카드 3장 (+, -, ÷) 자동 제공
        /// 2. 숫자 카드 3장 무조건 보장
        /// 3. 특수 카드 나오면 → 숫자 카드 1장 추가
        /// </summary>
        private IEnumerator DealingPhase()
        {
            // 상태 초기화
            playerSubmitted = false;
            roundTimer = 0f;
            playerHand.Clear();
            aiHand.Clear();
            deckService.BuildSlotDeck(); // 덱 재구성

            // UI와 PlayerController에 라운드 시작 알림
            GameEvents.InvokeRoundStarted();

            // 목표값과 베팅 초기화
            currentTarget = config.TargetValues[0];
            currentBet = config.MinBet;
            GameEvents.InvokeTargetSelected(currentTarget);
            GameEvents.InvokeBetChanged(currentBet);

            // ===== 플레이어 카드 분배 =====
            yield return StartCoroutine(DealCardsToPlayer());

            // ===== AI 카드 분배 (남은 카드로) =====
            yield return StartCoroutine(DealCardsToAI());

            // Player/AI Controller에 완성된 Hand 정보 전달
            playerController.SetHand(playerHand);
            aiController.PlayTurn(aiHand, currentTarget);
        }

        /// <summary>
        /// ✅ 플레이어에게 카드 분배
        /// </summary>
        private IEnumerator DealCardsToPlayer()
        {
            Debug.Log("[RoundController] === 플레이어 카드 분배 시작 ===");

            // --- 1단계: 기본 연산자 카드 3장 (+, -, ÷) 자동 제공 ---
            Debug.Log("[RoundController] 1단계: 기본 연산자 카드 3장 제공");

            var basicOperators = new[] {
                OperatorCard.OperatorType.Add,
                OperatorCard.OperatorType.Subtract,
                OperatorCard.OperatorType.Divide
            };

            foreach (var op in basicOperators)
            {
                OperatorCard operatorCard = new OperatorCard(op);
                playerHand.AddCard(operatorCard);
                GameEvents.InvokeCardAdded(operatorCard, true);

                yield return new WaitForSeconds(config.DealInterval);
            }

            // --- 2단계: 숫자 카드 3장 무조건 보장 ---
            Debug.Log("[RoundController] 2단계: 숫자 카드 3장 뽑기");

            int numberCardsDrawn = 0;
            List<Card> specialCards = new List<Card>(); // 특수 카드 임시 저장

            while (numberCardsDrawn < 3)
            {
                Card drawnCard = deckService.DrawSlotCard();

                if (drawnCard.GetCardType() == "Number")
                {
                    // 숫자 카드: 즉시 추가
                    playerHand.AddCard(drawnCard);
                    GameEvents.InvokeCardAdded(drawnCard, true);
                    numberCardsDrawn++;

                    Debug.Log($"[RoundController] 숫자 카드: {drawnCard.GetDisplayText()} ({numberCardsDrawn}/3)");
                }
                else if (drawnCard.GetCardType() == "Special")
                {
                    // 특수 카드: 저장 (나중에 추가)
                    specialCards.Add(drawnCard);
                    Debug.Log($"[RoundController] 특수 카드 발견: {drawnCard.GetDisplayText()}");
                }

                yield return new WaitForSeconds(config.DealInterval);
            }

            // --- 3단계: 특수 카드가 있으면 → 숫자 카드 추가로 뽑기 ---
            if (specialCards.Count > 0)
            {
                Debug.Log($"[RoundController] 3단계: 특수 카드 {specialCards.Count}장 → 숫자 카드 {specialCards.Count}장 추가");

                foreach (var specialCard in specialCards)
                {
                    // 특수 카드 추가
                    playerHand.AddCard(specialCard);
                    GameEvents.InvokeCardAdded(specialCard, true);

                    yield return new WaitForSeconds(config.DealInterval);

                    // 숫자 카드 1장 추가로 뽑기
                    Card extraNumber = DrawNumberCardOnly();
                    playerHand.AddCard(extraNumber);
                    GameEvents.InvokeCardAdded(extraNumber, true);

                    Debug.Log($"[RoundController] 특수 카드 {specialCard.GetDisplayText()} → 숫자 추가 {extraNumber.GetDisplayText()}");

                    yield return new WaitForSeconds(config.DealInterval);
                }
            }

            Debug.Log($"[RoundController] === 플레이어 카드 분배 완료: 총 {playerHand.GetTotalCardCount()}장 ===");
        }

        /// <summary>
        /// ✅ AI에게 카드 분배 (플레이어와 동일한 방식, 남은 카드로)
        /// </summary>
        private IEnumerator DealCardsToAI()
        {
            Debug.Log("[RoundController] === AI 카드 분배 시작 ===");

            // --- 1단계: 기본 연산자 카드 3장 (+, -, ÷) 자동 제공 ---
            var basicOperators = new[] {
                OperatorCard.OperatorType.Add,
                OperatorCard.OperatorType.Subtract,
                OperatorCard.OperatorType.Divide
            };

            foreach (var op in basicOperators)
            {
                OperatorCard operatorCard = new OperatorCard(op);
                aiHand.AddCard(operatorCard);
                GameEvents.InvokeCardAdded(operatorCard, false);

                yield return new WaitForSeconds(config.DealInterval);
            }

            // --- 2단계: 숫자 카드 3장 무조건 보장 (남은 카드에서) ---
            int numberCardsDrawn = 0;
            List<Card> specialCards = new List<Card>();

            while (numberCardsDrawn < 3)
            {
                Card drawnCard = deckService.DrawSlotCard(); // 남은 카드에서 뽑기

                if (drawnCard.GetCardType() == "Number")
                {
                    aiHand.AddCard(drawnCard);
                    GameEvents.InvokeCardAdded(drawnCard, false);
                    numberCardsDrawn++;
                }
                else if (drawnCard.GetCardType() == "Special")
                {
                    specialCards.Add(drawnCard);
                }

                yield return new WaitForSeconds(config.DealInterval);
            }

            // --- 3단계: 특수 카드가 있으면 → 숫자 카드 추가로 뽑기 ---
            if (specialCards.Count > 0)
            {
                foreach (var specialCard in specialCards)
                {
                    aiHand.AddCard(specialCard);
                    GameEvents.InvokeCardAdded(specialCard, false);

                    yield return new WaitForSeconds(config.DealInterval);

                    Card extraNumber = DrawNumberCardOnly();
                    aiHand.AddCard(extraNumber);
                    GameEvents.InvokeCardAdded(extraNumber, false);

                    yield return new WaitForSeconds(config.DealInterval);
                }
            }

            Debug.Log($"[RoundController] === AI 카드 분배 완료: 총 {aiHand.GetTotalCardCount()}장 ===");
        }

        /// <summary>
        /// ✅ 숫자 카드만 뽑기 (특수 카드가 나올 때까지 계속 뽑음)
        /// </summary>
        private Card DrawNumberCardOnly()
        {
            Card card;
            do
            {
                card = deckService.DrawSlotCard();
            }
            while (card.GetCardType() != "Number");

            return card;
        }

        /// <summary>
        /// ✅ 수정: 제출 가능 여부 알림 추가
        /// 2단계: 플레이어 입력/타이머 대기 코루틴
        /// </summary>
        private IEnumerator WaitingPhase()
        {
            bool wasSubmitAvailable = false;

            while (roundTimer < config.RoundDuration)
            {
                if (playerSubmitted)
                {
                    yield break;
                }

                roundTimer += Time.deltaTime;
                GameEvents.InvokeTimerUpdated(roundTimer, config.RoundDuration);

                // ✅ 추가: 제출 가능 여부 체크 및 이벤트 발행
                bool isSubmitAvailable = roundTimer >= config.SubmissionUnlockTime;
                if (isSubmitAvailable != wasSubmitAvailable)
                {
                    GameEvents.InvokeSubmitAvailabilityChanged(isSubmitAvailable);
                    wasSubmitAvailable = isSubmitAvailable;

                    if (isSubmitAvailable)
                    {
                        Debug.Log($"[RoundController] 제출 가능! ({config.SubmissionUnlockTime}초 경과)");
                        // ✅ 추가: 제출 가능 안내 텍스트
                        GameEvents.InvokeStatusTextUpdated("수식을 완성하면 제출 버튼을 눌러 제출하세요.");
                    }
                }

                yield return null;
            }

            Debug.Log("[RoundController] 시간 초과! 강제 제출합니다.");
        }

        /// <summary>
        /// ✅ 추가: 결과 확인 페이즈
        /// </summary>
        private IEnumerator ResultsPhase()
        {
            float resultsTimer = 0f;

            while (resultsTimer < config.ResultsDisplayDuration)
            {
                resultsTimer += Time.deltaTime;

                // ✅ 결과 확인 타이머 표시
                GameEvents.InvokeTimerUpdated(resultsTimer, config.ResultsDisplayDuration);

                yield return null;
            }

            Debug.Log("[RoundController] 결과 확인 완료! 다음 라운드를 시작합니다.");
        }

        /// <summary>
        /// 3단계: 수식 평가 및 결과 생성
        /// </summary>
        private RoundResult EvaluatePhase()
        {
            Expression playerExpr = playerController.GetExpression();
            var playerValidation = ExpressionValidator.Validate(playerExpr, playerHand);

            var playerEvaluation = playerValidation.IsValid
                ? MathHighLow.Models.ExpressionEvaluator.Evaluate(playerExpr)
                : new MathHighLow.Models.ExpressionEvaluator.EvaluationResult { Success = false, ErrorMessage = playerValidation.ErrorMessage };

            Expression aiExpr = aiController.GetExpression();
            var aiEvaluation = MathHighLow.Models.ExpressionEvaluator.Evaluate(aiExpr);

            return CreateRoundResult(playerExpr, playerEvaluation, aiExpr, aiEvaluation);
        }

        private RoundResult CreateRoundResult(Expression playerExpr, MathHighLow.Models.ExpressionEvaluator.EvaluationResult playerEval,
                                              Expression aiExpr, MathHighLow.Models.ExpressionEvaluator.EvaluationResult aiEval)
        {
            RoundResult result = new RoundResult
            {
                Target = currentTarget,
                Bet = currentBet,
                PlayerExpression = playerEval.Success ? playerExpr.ToDisplayString() : "-",
                PlayerValue = playerEval.Success ? playerEval.Value : float.NaN,
                PlayerError = playerEval.Success ? "" : playerEval.ErrorMessage,
                AIExpression = aiEval.Success ? aiExpr.ToDisplayString() : "-",
                AIValue = aiEval.Success ? aiEval.Value : float.NaN,
                AIError = aiEval.Success ? "" : aiEval.ErrorMessage
            };

            result.PlayerDifference = playerEval.Success ? Mathf.Abs(result.PlayerValue - result.Target) : float.PositiveInfinity;
            result.AIDifference = aiEval.Success ? Mathf.Abs(result.AIValue - result.Target) : float.PositiveInfinity;

            if (result.PlayerDifference == float.PositiveInfinity && result.AIDifference == float.PositiveInfinity)
            {
                result.Winner = "Invalid";
                result.PlayerScoreChange = 0;
            }
            else if (Mathf.Approximately(result.PlayerDifference, result.AIDifference))
            {
                result.Winner = "Draw";
                result.PlayerScoreChange = 0;
            }
            else if (result.PlayerDifference < result.AIDifference)
            {
                result.Winner = "Player";
                result.PlayerScoreChange = currentBet;
            }
            else
            {
                result.Winner = "AI";
                result.PlayerScoreChange = -currentBet;
            }

            result.AIScoreChange = -result.PlayerScoreChange;
            return result;
        }

        #endregion
    }
}