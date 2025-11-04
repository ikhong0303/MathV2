using UnityEngine;
using MathHighLow.Models;
using MathHighLow.Services;
using System.Collections;

namespace MathHighLow.Controllers
{
    /// <summary>
    /// [학습 포인트] 최상위 컨트롤러 (메인)
    /// 
    /// 게임의 전체 생명주기(시작, 진행, 종료)를 관리합니다.
    /// 다른 컨트롤러들(Round, Player, AI)을 조정하고,
    /// 게임의 핵심 상태(점수, 게임 상태)를 소유합니다.
    /// </summary>
    [RequireComponent(typeof(RoundController))]
    [RequireComponent(typeof(PlayerController))]
    [RequireComponent(typeof(AIController))]
    public class GameController : MonoBehaviour
    {
        // [수정됨] 인스펙터에서 숨기고 public으로 변경
        [HideInInspector] public RoundController roundController;
        [HideInInspector] public PlayerController playerController;
        [HideInInspector] public AIController aiController;

        // --- 내부 서비스 및 상태 ---

        /// <summary>
        /// 게임의 모든 설정값
        /// </summary>
        private GameConfig config;

        /// <summary>
        /// 게임의 덱 관리 서비스
        /// </summary>
        private DeckService deckService;

        /// <summary>
        /// 플레이어의 현재 점수(크레딧)
        /// </summary>
        private int playerCredits;

        /// <summary>
        /// AI의 현재 점수(크레딧)
        /// </summary>
        private int aiCredits;

        /// <summary>
        /// 게임의 현재 상태
        /// </summary>
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
            // 1. 설정 및 서비스 생성
            // [학습 포인트] 의존성 생성
            // 게임에 필요한 핵심 객체들을 생성합니다.
            config = GameConfig.Default(); // Models에서 기본 설정 로드
            deckService = new DeckService(config);

            // 2. 컨트롤러 참조 확인 (자동 할당)
            // [수정됨] RequireComponent가 보장하므로, 바로 할당합니다.
            roundController = GetComponent<RoundController>();
            playerController = GetComponent<PlayerController>();
            aiController = GetComponent<AIController>();

            // 3. 하위 컨트롤러 초기화 (의존성 주입)
            // [학습 포인트] 의존성 주입 (Dependency Injection)
            // GameController가 생성한 객체를 하위 컨트롤러에 전달합니다.
            roundController.Initialize(config, deckService);
            playerController.Initialize(); // PlayerController는 GameEvents를 구독
            aiController.Initialize();     // AIController도 자체 초기화
        }

        void OnEnable()
        {
            // [학습 포인트] 이벤트 구독
            // GameController는 "라운드가 끝났다"는 '결과'에만 관심이 있습니다.
            GameEvents.OnRoundEnded += HandleRoundEnded;
        }

        void OnDisable()
        {
            // [학습 포인트] 이벤트 구독 해제
            // 오브젝트가 파괴되거나 비활성화될 때 메모리 누수를 방지합니다.
            GameEvents.OnRoundEnded -= HandleRoundEnded;
        }

        void Start()
        {
            StartGame();
        }

        #endregion

        #region 게임 흐름 제어

        /// <summary>
        /// 게임을 시작합니다.
        /// </summary>
        private void StartGame()
        {
            currentState = GameState.Playing;

            // 1. 초기 점수 설정 (Config에서 가져옴)
            playerCredits = config.StartingCredits;
            aiCredits = config.StartingCredits;

            // 2. UI에 점수 변경 알림 (이벤트 발행)
            // [학습 포인트] 이벤트 발행
            // GameController가 점수를 '소유'하고, 변경될 때마다 UI에 알립니다.
            GameEvents.InvokeScoreChanged(playerCredits, aiCredits);

            // 3. 첫 라운드 시작
            // RoundController에게 라운드 시작을 '명령'
            roundController.StartNewRound();
        }

        /// <summary>
        /// 라운드 종료 이벤트를 처리합니다.
        /// </summary>
        private void HandleRoundEnded(RoundResult result)
        {
            // 게임 오버 상태에서는 더 이상 라운드를 진행하지 않음
            if (currentState != GameState.Playing) return;

            // 1. 결과에 따라 점수 갱신
            playerCredits += result.PlayerScoreChange;
            aiCredits += result.AIScoreChange;

            // 2. UI에 점수 변경 알림 (이벤트 발행)
            GameEvents.InvokeScoreChanged(playerCredits, aiCredits);

            // 3. 게임 오버 조건 확인
            if (playerCredits <= 0)
            {
                EndGame("AI");
            }
            else if (aiCredits <= 0)
            {
                EndGame("Player");
            }
            else
            {
                // 4. 다음 라운드 시작 (결과 표시 시간을 위해 잠시 대기)
                StartCoroutine(NextRoundRoutine());
            }
        }

        /// <summary>
        /// 다음 라운드를 잠시 대기 후 시작합니다.
        /// </summary>
        private IEnumerator NextRoundRoutine()
        {
            // [학습 포인트] 코루틴을 사용한 시간 지연
            // 결과 표시 시간을 config에서 가져와 대기
            yield return new WaitForSeconds(config.ResultsDisplayDuration);

            // 대기하는 동안 게임이 끝나지 않았다면 다음 라운드 시작
            if (currentState == GameState.Playing)
            {
                roundController.StartNewRound();
            }
        }

        /// <summary>
        /// 게임을 종료합니다.
        /// </summary>
        private void EndGame(string winner)
        {
            currentState = GameState.GameOver;

            // [학습 포인트] 이벤트 발행
            // 게임이 끝났음을 UI(View)와 다른 시스템에 알립니다.
            GameEvents.InvokeGameOver(winner);

            Debug.Log($"[GameController] 게임 종료! 최종 승자: {winner}");

            // (선택 사항) 여기서 게임 재시작 버튼을 활성화할 수 있음
        }

        #endregion
    }
}