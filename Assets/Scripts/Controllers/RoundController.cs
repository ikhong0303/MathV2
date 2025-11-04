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
    /// 코루틴을 사용하여 시간 기반의 흐름을 관리합니다.
    /// </summary>
    public class RoundController : MonoBehaviour
    {
        // --- 의존성 (GameController로부터 주입받음) ---
        private GameConfig config;
        private DeckService deckService;

        // --- 컨트롤러 참조 (GameController가 관리) ---
        // [수정됨] [SerializeField] 대신 [HideInInspector] 사용
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

        private bool playerSubmitted; // 플레이어가 제출했는지
        private float roundTimer;     // 라운드 경과 시간

        /// <summary>
        /// 컨트롤러 초기화 (GameController가 호출)
        /// [학습 포인트] 의존성 주입
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

        // [추가됨] Start 메서드에서 컨트롤러 참조 자동 할당
        void Start()
        {
            // [학습 포인트] GetComponent를 이용한 자동 참조
            // 같은 게임 오브젝트에 붙어있는 다른 컨트롤러를 자동으로 찾습니다.
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
            // [학습 포인트] 이벤트 구독
            // 플레이어가 '제출' 버튼을 눌렀을 때를 감지합니다.
            GameEvents.OnSubmitClicked += HandleSubmitClicked;
            GameEvents.OnTargetSelected += HandleTargetSelected;
            GameEvents.OnBetChanged += HandleBetChanged;
        }

        void OnDisable()
        {
            // [학습 포인트] 이벤트 구독 해제
            GameEvents.OnSubmitClicked -= HandleSubmitClicked;
            GameEvents.OnTargetSelected -= HandleTargetSelected;
            GameEvents.OnBetChanged -= HandleBetChanged;
        }

        #endregion

        #region 이벤트 핸들러

        /// <summary>
        /// 제출 버튼 클릭 이벤트를 처리합니다.
        /// </summary>
        private void HandleSubmitClicked()
        {
            // 입력 대기 상태이고, 제출 잠금 시간이 지났을 때만 유효
            if (currentPhase == RoundPhase.Waiting && roundTimer >= config.SubmissionUnlockTime)
            {
                playerSubmitted = true;
            }
        }

        /// <summary>
        /// 목표값 선택 이벤트를 처리합니다.
        /// </summary>
        private void HandleTargetSelected(int target)
        {
            currentTarget = target;
        }

        /// <summary>
        /// 베팅 금액 변경 이벤트를 처리합니다.
        /// </summary>
        private void HandleBetChanged(int bet)
        {
            currentBet = bet;
        }

        #endregion

        #region 라운드 흐름 제어 (코루틴)

        /// <summary>
        /// 새 라운드를 시작합니다. (GameController가 호출)
        /// </summary>
        public void StartNewRound()
        {
            // 이미 다른 라운드가 진행 중이면 중복 실행 방지
            if (currentPhase != RoundPhase.Idle && currentPhase != RoundPhase.Results)
            {
                Debug.LogWarning("[RoundController] 이미 라운드가 진행 중입니다.");
                return;
            }

            // 라운드 진행 코루틴 시작
            StartCoroutine(RoundLoopRoutine());
        }

        /// <summary>
        /// [학습 포인트] 코루틴을 사용한 게임 루프
        /// 라운드의 각 단계를 순차적으로 실행합니다.
        /// </summary>
        private IEnumerator RoundLoopRoutine()
        {
            // --- 1. Dealing (분배) ---
            currentPhase = RoundPhase.Dealing;
            yield return StartCoroutine(DealingPhase());

            // --- 2. Waiting (대기) ---
            currentPhase = RoundPhase.Waiting;
            yield return StartCoroutine(WaitingPhase());

            // --- 3. Evaluating (평가) ---
            currentPhase = RoundPhase.Evaluating;
            RoundResult result = EvaluatePhase(); // 평가는 즉시 실행

            // --- 4. Results (결과) ---
            currentPhase = RoundPhase.Results;

            // [학습 포인트] 이벤트 발행
            // 라운드 결과를 GameController와 UI(View)에 알립니다.
            GameEvents.InvokeRoundEnded(result);
        }

        /// <summary>
        /// 1단계: 카드 분배 코루틴
        /// </summary>
        private IEnumerator DealingPhase()
        {
            // 상태 초기화
            playerSubmitted = false;
            roundTimer = 0f;
            playerHand.Clear();
            aiHand.Clear();
            deckService.BuildSlotDeck(); // 새 라운드를 위해 덱 다시 구성

            // UI와 PlayerController에 라운드 시작 알림
            GameEvents.InvokeRoundStarted();

            // 목표값과 베팅 초기화
            currentTarget = config.TargetValues[0];
            currentBet = config.MinBet;
            GameEvents.InvokeTargetSelected(currentTarget);
            GameEvents.InvokeBetChanged(currentBet);

            // [학습 포인트] 시간차를 둔 이벤트 발행
            // 카드가 한 장씩 날아가는 연출을 위해 대기 시간을 둡니다.
            for (int i = 0; i < config.InitialCardCount; i++)
            {
                // 플레이어 카드 분배
                Card playerCard = deckService.DrawSlotCard();
                playerHand.AddCard(playerCard);
                GameEvents.InvokeCardAdded(playerCard, true); // true = isPlayer

                // AI 카드 분배
                Card aiCard = (playerCard.GetCardType() == "Special")
                    ? playerCard.Clone() // 특수 카드는 동일하게
                    : deckService.DrawRandomNumberCard(); // 숫자 카드는 랜덤하게

                aiHand.AddCard(aiCard);
                GameEvents.InvokeCardAdded(aiCard, false); // false = isPlayer

                // 카드 분배 간격 대기
                yield return new WaitForSeconds(config.DealInterval);
            }

            // × 카드 효과 처리: 연산자 비활성화
            // (간소화: 여기서는 랜덤하게 하나 비활성화)
            int multiplyCount = playerHand.GetMultiplyCount();
            if (multiplyCount > 0)
            {
                var ops = playerHand.GetAvailableOperators();
                if (ops.Count > 0)
                {
                    var opToDisable = ops[Random.Range(0, ops.Count)];
                    playerHand.DisableOperator(opToDisable);
                    aiHand.DisableOperator(opToDisable);

                    // UI에 비활성화 알림
                    GameEvents.InvokeOperatorDisabled(opToDisable);
                }
            }

            // Player/AI Controller에 완성된 Hand 정보 전달
            playerController.SetHand(playerHand);
            aiController.PlayTurn(aiHand, currentTarget); // AI는 즉시 계산 시작
        }

        /// <summary>
        /// 2단계: 플레이어 입력/타이머 대기 코루틴
        /// </summary>
        private IEnumerator WaitingPhase()
        {
            while (roundTimer < config.RoundDuration)
            {
                // 플레이어가 제출 버튼을 누르면 즉시 종료
                if (playerSubmitted)
                {
                    yield break;
                }

                roundTimer += Time.deltaTime;
                yield return null; // 1프레임 대기
            }

            // 타임 오버
            Debug.Log("[RoundController] 시간 초과! 강제 제출합니다.");
        }

        /// <summary>
        /// 3단계: 수식 평가 및 결과 생성
        /// </summary>
        private RoundResult EvaluatePhase()
        {
            // 1. 플레이어 수식 가져오기
            Expression playerExpr = playerController.GetExpression();
            var playerValidation = ExpressionValidator.Validate(playerExpr, playerHand);

            // [수정됨] 모호한 참조 해결
            var playerEvaluation = playerValidation.IsValid
                ? MathHighLow.Models.ExpressionEvaluator.Evaluate(playerExpr)
                : new MathHighLow.Models.ExpressionEvaluator.EvaluationResult { Success = false, ErrorMessage = playerValidation.ErrorMessage };

            // 2. AI 수식 가져오기
            Expression aiExpr = aiController.GetExpression();

            // [수정됨] 모호한 참조 해결
            var aiEvaluation = MathHighLow.Models.ExpressionEvaluator.Evaluate(aiExpr);

            // 3. 결과 객체 생성 (Models/RoundResult.cs)
            return CreateRoundResult(playerExpr, playerEvaluation, aiExpr, aiEvaluation);
        }

        /// <summary>
        /// 최종 결과 객체를 생성합니다.
        /// [수정됨] 모호한 참조 해결
        /// </summary>
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

            // 차이 계산
            result.PlayerDifference = playerEval.Success ? Mathf.Abs(result.PlayerValue - result.Target) : float.PositiveInfinity;
            result.AIDifference = aiEval.Success ? Mathf.Abs(result.AIValue - result.Target) : float.PositiveInfinity;

            // 승패 판정
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