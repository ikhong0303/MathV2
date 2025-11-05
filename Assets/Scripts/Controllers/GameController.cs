using UnityEngine;
using MathHighLow.Models;
using MathHighLow.Services;
using System.Collections;

namespace MathHighLow.Controllers
{
    /// <summary>
    /// ✅ 수정: 베팅 검증 기능 추가
    /// - 최대 5원까지만
    /// - 보유 머니 초과 방지
    /// </summary>
    [RequireComponent(typeof(RoundController))]
    [RequireComponent(typeof(PlayerController))]
    [RequireComponent(typeof(AIController))]
    public class GameController : MonoBehaviour
    {
        [HideInInspector] public RoundController roundController;
        [HideInInspector] public PlayerController playerController;
        [HideInInspector] public AIController aiController;

        private GameConfig config;
        private DeckService deckService;
        private int playerCredits;
        private int aiCredits;

        private enum GameState
        {
            Initializing,
            Playing,
            GameOver
        }
        private GameState currentState;

        #region Unity 생명주기 및 이벤트 구독

        void Awake()
        {
            config = GameConfig.Default();
            deckService = new DeckService(config);

            roundController = GetComponent<RoundController>();
            playerController = GetComponent<PlayerController>();
            aiController = GetComponent<AIController>();

            roundController.Initialize(config, deckService);
            playerController.Initialize();
            aiController.Initialize();
        }

        void OnEnable()
        {
            GameEvents.OnRoundEnded += HandleRoundEnded;
            GameEvents.OnBetChanged += HandleBetChanged; // ✅ 추가
        }

        void OnDisable()
        {
            GameEvents.OnRoundEnded -= HandleRoundEnded;
            GameEvents.OnBetChanged -= HandleBetChanged; // ✅ 추가
        }

        void Start()
        {
            StartGame();
        }

        #endregion

        #region 게임 흐름 제어

        private void StartGame()
        {
            currentState = GameState.Playing;
            playerCredits = config.StartingCredits;
            aiCredits = config.StartingCredits;
            GameEvents.InvokeScoreChanged(playerCredits, aiCredits);
            roundController.StartNewRound();
        }

        private void HandleRoundEnded(RoundResult result)
        {
            if (currentState != GameState.Playing) return;

            playerCredits += result.PlayerScoreChange;
            aiCredits += result.AIScoreChange;
            GameEvents.InvokeScoreChanged(playerCredits, aiCredits);

            if (playerCredits <= 0)
            {
                EndGame("AI");
            }
            else if (aiCredits <= 0)
            {
                EndGame("Player");
            }
            // ✅ 자동 재시작은 RoundController에서 처리
        }

        /// <summary>
        /// ✅ 추가: 베팅 검증 로직
        /// </summary>
        private void HandleBetChanged(int requestedBet)
        {
            // 1. 최대 베팅 확인 (5원)
            if (requestedBet > config.MaxBet)
            {
                GameEvents.InvokeStatusTextUpdated($"베팅은 최대 {config.MaxBet}원까지만 가능합니다.");
                GameEvents.InvokeBetChanged(config.MaxBet);
                return;
            }

            // 2. 보유 머니 확인
            if (requestedBet > playerCredits)
            {
                GameEvents.InvokeStatusTextUpdated($"보유 머니({playerCredits}원)보다 많이 걸 수 없습니다.");
                int maxAffordable = Mathf.Min(playerCredits, config.MaxBet);
                GameEvents.InvokeBetChanged(maxAffordable);
                return;
            }

            // 3. 정상 베팅
            Debug.Log($"[GameController] 베팅: {requestedBet}원 (보유: {playerCredits}원)");
        }

        private void EndGame(string winner)
        {
            currentState = GameState.GameOver;
            GameEvents.InvokeGameOver(winner);
            Debug.Log($"[GameController] 게임 종료! 최종 승자: {winner}");
        }

        #endregion
    }
}