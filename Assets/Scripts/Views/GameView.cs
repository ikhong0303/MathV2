using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using MathHighLow.Models;
using MathHighLow.Services;

namespace MathHighLow.Views
{
    /// <summary>
    /// [학습 포인트] 메인 View
    /// 
    /// Canvas에 부착되어 모든 UI 요소를 관리하고 이벤트를 구독/발행합니다.
    /// 이 스크립트는 로직(Controller)을 전혀 모르며, 오직 GameEvents와 통신합니다.
    /// </summary>
    public class GameView : MonoBehaviour
    {
        [Header("프리팹")]
        [SerializeField] private CardView cardPrefab;

        [Header("UI 컨테이너 (손패)")]
        [SerializeField] private Transform playerHandContainer;
        [SerializeField] private Transform aiHandContainer;

        [Header("상태 텍스트")]
        [SerializeField] private TextMeshProUGUI playerScoreText;
        [SerializeField] private TextMeshProUGUI aiScoreText;
        [SerializeField] private TextMeshProUGUI playerExpressionText;
        [SerializeField] private TextMeshProUGUI timerText;
        [SerializeField] private TextMeshProUGUI statusText; // (예: "카드를 분배합니다...")

        [Header("베팅 UI")]
        [SerializeField] private TextMeshProUGUI betText;
        [SerializeField] private Button betIncreaseButton;
        [SerializeField] private Button betDecreaseButton;
        private int currentBetDisplay; // UI가 현재 표시 중인 베팅 값

        [Header("목표값 버튼")]
        [SerializeField] private List<Button> targetButtons; // 인스펙터에서 타겟 버튼들 연결

        [Header("기본 버튼")]
        [SerializeField] private Button submitButton;
        [SerializeField] private Button resetButton;

        [Header("연산자 버튼")]
        [SerializeField] private Button addButton;
        [SerializeField] private Button subtractButton;
        [SerializeField] private Button divideButton;
        [SerializeField] private Button multiplyButton;
        [SerializeField] private Button sqrtButton;

        [Header("결과 패널")]
        [SerializeField] private GameObject resultPanel;
        [SerializeField] private TextMeshProUGUI resultSummaryText;
        [SerializeField] private TextMeshProUGUI resultDetailText;

        // 생성된 카드 뷰 오브젝트들을 관리 (라운드 리셋 시 파괴하기 위함)
        private List<GameObject> spawnedCards = new List<GameObject>();

        #region 이벤트 구독 (OnEnable / OnDisable)

        void OnEnable()
        {
            // 점수
            GameEvents.OnScoreChanged += UpdateScoreText;

            // 라운드 진행
            GameEvents.OnRoundStarted += HandleRoundStarted;
            GameEvents.OnCardAdded += HandleCardAdded;
            GameEvents.OnOperatorDisabled += HandleOperatorDisabled;
            GameEvents.OnRoundEnded += HandleRoundEnded;

            // 플레이어 입력
            GameEvents.OnExpressionUpdated += UpdateExpressionText;
            GameEvents.OnTargetSelected += HandleTargetSelected;
            GameEvents.OnBetChanged += UpdateBetText;

            // 게임 종료
            GameEvents.OnGameOver += HandleGameOver;
        }

        void OnDisable()
        {
            GameEvents.OnScoreChanged -= UpdateScoreText;
            GameEvents.OnRoundStarted -= HandleRoundStarted;
            GameEvents.OnCardAdded -= HandleCardAdded;
            GameEvents.OnOperatorDisabled -= HandleOperatorDisabled;
            GameEvents.OnRoundEnded -= HandleRoundEnded;
            GameEvents.OnExpressionUpdated -= UpdateExpressionText;
            GameEvents.OnTargetSelected -= HandleTargetSelected;
            GameEvents.OnBetChanged -= UpdateBetText;
            GameEvents.OnGameOver -= HandleGameOver;
        }

        #endregion

        #region 버튼 리스너 (Start)

        void Start()
        {
            // [학습 포인트] View가 입력을 발행하는 방법

            // 기본 버튼
            submitButton.onClick.AddListener(() => GameEvents.InvokeSubmit());
            resetButton.onClick.AddListener(() => GameEvents.InvokeReset());

            // 연산자 버튼
            addButton.onClick.AddListener(() => GameEvents.InvokeOperatorSelected(OperatorCard.OperatorType.Add));
            subtractButton.onClick.AddListener(() => GameEvents.InvokeOperatorSelected(OperatorCard.OperatorType.Subtract));
            divideButton.onClick.AddListener(() => GameEvents.InvokeOperatorSelected(OperatorCard.OperatorType.Divide));
            multiplyButton.onClick.AddListener(() => GameEvents.InvokeOperatorSelected(OperatorCard.OperatorType.Multiply));
            sqrtButton.onClick.AddListener(() => GameEvents.InvokeSquareRootClicked());

            // 베팅 버튼
            betIncreaseButton.onClick.AddListener(HandleBetIncrease);
            betDecreaseButton.onClick.AddListener(HandleBetDecrease);

            // 목표값 버튼 (인스펙터에서 설정한 값 사용)
            // 예시: 버튼이 2개이고 타겟이 1, 20 이라고 가정
            if (targetButtons.Count > 0 && targetButtons[0] != null)
                targetButtons[0].onClick.AddListener(() => GameEvents.InvokeTargetSelected(1)); // (GameConfig와 맞춰야 함)
            if (targetButtons.Count > 1 && targetButtons[1] != null)
                targetButtons[1].onClick.AddListener(() => GameEvents.InvokeTargetSelected(20)); // (GameConfig와 맞춰야 함)

            // 초기화
            resultPanel.SetActive(false);
            UpdateScoreText(0, 0); // (GameController가 시작 시 덮어쓸 것임)
            UpdateExpressionText("");
        }

        #endregion

        #region 입력 핸들러 (UI -> Event)

        private void HandleBetIncrease()
        {
            // (참고: Min/MaxBet는 GameConfig에 있지만 View는 Config를 모름)
            // 일단 값을 올리고 이벤트 발행 -> 로직(RoundController)이 받아서 검증 후
            // 다시 OnBetChanged 이벤트를 발행하면 UI가 거기에 맞춰짐.
            currentBetDisplay++;
            GameEvents.InvokeBetChanged(currentBetDisplay);
        }

        private void HandleBetDecrease()
        {
            currentBetDisplay--;
            if (currentBetDisplay < 1) currentBetDisplay = 1; // 최소 1
            GameEvents.InvokeBetChanged(currentBetDisplay);
        }

        #endregion

        #region 로직 핸들러 (Event -> UI)

        private void UpdateScoreText(int playerScore, int aiScore)
        {
            playerScoreText.text = $"Player: ${playerScore}";
            aiScoreText.text = $"AI: ${aiScore}";
        }

        private void HandleRoundStarted()
        {
            // 기존 카드 오브젝트 모두 파괴
            foreach (var card in spawnedCards)
            {
                Destroy(card);
            }
            spawnedCards.Clear();

            // 패널/텍스트 초기화
            resultPanel.SetActive(false);
            UpdateExpressionText("");
            statusText.text = "카드를 분배합니다...";

            // 연산자 버튼 활성화
            addButton.interactable = true;
            subtractButton.interactable = true;
            divideButton.interactable = true;
        }

        private void HandleCardAdded(Card card, bool isPlayer)
        {
            // 1. 프리팹 생성
            Transform parent = isPlayer ? playerHandContainer : aiHandContainer;
            CardView newCardView = Instantiate(cardPrefab, parent);

            // 2. 프리팹 초기화 (데이터 주입)
            newCardView.Initialize(card, isPlayer);

            // 3. 리스트에 추가하여 관리
            spawnedCards.Add(newCardView.gameObject);
        }

        private void HandleOperatorDisabled(OperatorCard.OperatorType op)
        {
            // × 카드로 비활성화된 연산자 버튼을 회색 처리
            switch (op)
            {
                case OperatorCard.OperatorType.Add:
                    addButton.interactable = false;
                    break;
                case OperatorCard.OperatorType.Subtract:
                    subtractButton.interactable = false;
                    break;
                case OperatorCard.OperatorType.Divide:
                    divideButton.interactable = false;
                    break;
            }
        }

        private void HandleRoundEnded(RoundResult result)
        {
            // 결과 패널 표시
            resultSummaryText.text = result.GetSummary();
            resultDetailText.text = result.GetDetail();
            resultPanel.SetActive(true);
        }

        private void UpdateExpressionText(string expressionText)
        {
            playerExpressionText.text = string.IsNullOrEmpty(expressionText) ? "..." : expressionText;
        }

        private void HandleTargetSelected(int target)
        {
            // (구현) 선택된 타겟 버튼을 하이라이트
        }

        private void UpdateBetText(int newBet)
        {
            currentBetDisplay = newBet;
            betText.text = $"${currentBetDisplay}";
        }

        private void HandleGameOver(string winner)
        {
            statusText.text = $"게임 종료! 최종 승자: {winner}";
            // (구현) 모든 버튼 비활성화
        }

        #endregion
    }
}