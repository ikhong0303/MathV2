using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using MathHighLow.Models;
using MathHighLow.Services;

namespace MathHighLow.Views
{
    /// <summary>
    /// ✅ 수정: 연산자 버튼 완전 제거
    /// 
    /// Canvas에 부착되어 모든 UI 요소를 관리하고 이벤트를 구독/발행합니다.
    /// 이제 연산자는 카드로만 처리합니다!
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
        [SerializeField] private TextMeshProUGUI statusText;

        [Header("베팅 UI")]
        [SerializeField] private TextMeshProUGUI betText;
        [SerializeField] private Button betIncreaseButton;
        [SerializeField] private Button betDecreaseButton;
        private int currentBetDisplay;

        [Header("목표값 버튼")]
        [SerializeField] private List<Button> targetButtons;
        [SerializeField] private Color normalColor = Color.white;
        [SerializeField] private Color selectedColor = Color.blue;
        private int currentSelectedTarget = -1;

        [Header("기본 버튼")]
        [SerializeField] private Button submitButton;
        [SerializeField] private Button resetButton;

        // ❌ 제거: 연산자 버튼들 (더 이상 사용 안 함)
        // [SerializeField] private Button addButton;
        // [SerializeField] private Button subtractButton;
        // [SerializeField] private Button divideButton;
        // [SerializeField] private Button multiplyButton;
        // [SerializeField] private Button sqrtButton;

        [Header("결과 패널")]
        [SerializeField] private GameObject resultPanel;
        [SerializeField] private TextMeshProUGUI resultSummaryText;
        [SerializeField] private TextMeshProUGUI resultDetailText;

        // 생성된 카드 뷰 오브젝트들을 관리
        private List<GameObject> spawnedCards = new List<GameObject>();

        #region 이벤트 구독 (OnEnable / OnDisable)

        void OnEnable()
        {
            // 점수
            GameEvents.OnScoreChanged += UpdateScoreText;

            // 라운드 진행
            GameEvents.OnRoundStarted += HandleRoundStarted;
            GameEvents.OnCardAdded += HandleCardAdded;
            GameEvents.OnRoundEnded += HandleRoundEnded;

            // 플레이어 입력
            GameEvents.OnExpressionUpdated += UpdateExpressionText;
            GameEvents.OnTargetSelected += HandleTargetSelected;
            GameEvents.OnBetChanged += UpdateBetText;
            GameEvents.OnTimerUpdated += UpdateTimerText;

            // 제출 가능 여부
            GameEvents.OnSubmitAvailabilityChanged += UpdateSubmitAvailability;

            // ✅ 추가: 상태 텍스트
            GameEvents.OnStatusTextUpdated += UpdateStatusText;

            // 게임 종료
            GameEvents.OnGameOver += HandleGameOver;
        }

        void OnDisable()
        {
            GameEvents.OnScoreChanged -= UpdateScoreText;
            GameEvents.OnRoundStarted -= HandleRoundStarted;
            GameEvents.OnCardAdded -= HandleCardAdded;
            GameEvents.OnRoundEnded -= HandleRoundEnded;
            GameEvents.OnExpressionUpdated -= UpdateExpressionText;
            GameEvents.OnTargetSelected -= HandleTargetSelected;
            GameEvents.OnBetChanged -= UpdateBetText;
            GameEvents.OnTimerUpdated -= UpdateTimerText;
            GameEvents.OnSubmitAvailabilityChanged -= UpdateSubmitAvailability;
            GameEvents.OnStatusTextUpdated -= UpdateStatusText; // ✅ 추가
            GameEvents.OnGameOver -= HandleGameOver;
        }

        #endregion

        #region 버튼 리스너 (Start)

        void Start()
        {
            // 기본 버튼
            submitButton.onClick.AddListener(() => GameEvents.InvokeSubmit());
            resetButton.onClick.AddListener(() => GameEvents.InvokeReset());

            // ❌ 제거: 연산자 버튼 리스너
            // addButton.onClick.AddListener(() => GameEvents.InvokeOperatorSelected(OperatorCard.OperatorType.Add));
            // subtractButton.onClick.AddListener(() => GameEvents.InvokeOperatorSelected(OperatorCard.OperatorType.Subtract));
            // divideButton.onClick.AddListener(() => GameEvents.InvokeOperatorSelected(OperatorCard.OperatorType.Divide));
            // multiplyButton.onClick.AddListener(() => GameEvents.InvokeOperatorSelected(OperatorCard.OperatorType.Multiply));
            // sqrtButton.onClick.AddListener(() => GameEvents.InvokeSquareRootClicked());

            // 베팅 버튼
            betIncreaseButton.onClick.AddListener(HandleBetIncrease);
            betDecreaseButton.onClick.AddListener(HandleBetDecrease);

            // 목표값 버튼
            if (targetButtons.Count > 0 && targetButtons[0] != null)
                targetButtons[0].onClick.AddListener(() => SelectTarget(0, 1));
            if (targetButtons.Count > 1 && targetButtons[1] != null)
                targetButtons[1].onClick.AddListener(() => SelectTarget(1, 20));

            // 초기화
            resultPanel.SetActive(false);
            UpdateScoreText(0, 0);
            UpdateExpressionText("");
            UpdateTimerText(0, 180);
        }

        #endregion

        #region 입력 핸들러 (UI -> Event)

        private void HandleBetIncrease()
        {
            currentBetDisplay++;

            // ✅ 추가: 최대 5원 제한
            if (currentBetDisplay > 5)
            {
                currentBetDisplay = 5;
            }

            GameEvents.InvokeBetChanged(currentBetDisplay);
        }

        private void HandleBetDecrease()
        {
            currentBetDisplay--;
            if (currentBetDisplay < 1) currentBetDisplay = 1;
            GameEvents.InvokeBetChanged(currentBetDisplay);
        }

        private void SelectTarget(int buttonIndex, int targetValue)
        {
            // ✅ 수정: 바로 색상 변경
            currentSelectedTarget = targetValue;

            // 즉시 버튼 색상 업데이트
            for (int i = 0; i < targetButtons.Count; i++)
            {
                if (targetButtons[i] == null) continue;

                // 버튼에 연결된 목표값 확인 (0번=1, 1번=20)
                int buttonTargetValue = (i == 0) ? 1 : 20;

                // 선택된 버튼은 파란색, 나머지는 흰색
                ColorBlock colors = targetButtons[i].colors;
                colors.normalColor = (buttonTargetValue == targetValue) ? selectedColor : normalColor;
                targetButtons[i].colors = colors;
            }

            // 이벤트 발행 (다른 시스템에 알림)
            GameEvents.InvokeTargetSelected(targetValue);
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

            // 패널 초기화
            resultPanel.SetActive(false);
            UpdateExpressionText("");

            // ✅ 수정: 초기 상태는 RoundController에서 설정
            // statusText는 OnStatusTextUpdated 이벤트로 업데이트됨

            // ✅ 추가: 제출 버튼 초기 비활성화
            submitButton.interactable = false;

            UpdateTimerText(0, 180);
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

        // ❌ 제거: HandleOperatorDisabled (버튼이 없으므로 불필요)
        /*
        private void HandleOperatorDisabled(OperatorCard.OperatorType op)
        {
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
        */

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

        /// <summary>
        /// ❌ 더 이상 사용 안 함: 색상 변경은 SelectTarget에서 바로 처리
        /// 이벤트는 다른 시스템(RoundController 등)에 알리는 용도로만 사용
        /// </summary>
        private void HandleTargetSelected(int target)
        {
            // 색상 변경은 SelectTarget에서 이미 처리됨
            // 여기서는 아무것도 하지 않음
        }

        private void UpdateBetText(int newBet)
        {
            currentBetDisplay = newBet;
            betText.text = $"${currentBetDisplay}";

            // ✅ 추가: 최대값에 도달하면 버튼 비활성화
            if (currentBetDisplay >= 5)
            {
                betIncreaseButton.interactable = false;
            }
            else
            {
                betIncreaseButton.interactable = true;
            }

            // 최소값에 도달하면 감소 버튼 비활성화
            if (currentBetDisplay <= 1)
            {
                betDecreaseButton.interactable = false;
            }
            else
            {
                betDecreaseButton.interactable = true;
            }
        }

        private void UpdateTimerText(float currentTime, float maxTime)
        {
            float remainingTime = maxTime - currentTime;

            if (remainingTime < 0)
            {
                timerText.text = "00:00";
                timerText.color = Color.red;
            }
            else
            {
                int minutes = Mathf.FloorToInt(remainingTime / 60f);
                int seconds = Mathf.FloorToInt(remainingTime % 60f);
                timerText.text = $"{minutes:00}:{seconds:00}";

                // 30초 이하면 빨간색으로 경고
                if (remainingTime <= 30)
                {
                    timerText.color = Color.red;
                }
                else
                {
                    timerText.color = Color.white;
                }
            }
        }

        private void HandleGameOver(string winner)
        {
            statusText.text = $"게임 종료! 최종 승자: {winner}";
            // (구현) 모든 버튼 비활성화
        }

        /// <summary>
        /// ✅ 수정: 제출 가능 여부 업데이트 (버튼만 제어)
        /// </summary>
        private void UpdateSubmitAvailability(bool canSubmit)
        {
            // 제출 버튼만 활성화/비활성화
            submitButton.interactable = canSubmit;

            // 색상 변경 (선택)
            var colors = submitButton.colors;
            colors.normalColor = canSubmit ? Color.white : Color.gray;
            submitButton.colors = colors;
        }

        /// <summary>
        /// ✅ 추가: 상태 텍스트 업데이트
        /// </summary>
        private void UpdateStatusText(string message)
        {
            statusText.text = message;

            // 메시지별 색상 설정
            if (message.Contains("분배"))
            {
                statusText.color = Color.white;
            }
            else if (message.Contains("완성하세요"))
            {
                statusText.color = Color.cyan;
            }
            else if (message.Contains("제출"))
            {
                statusText.color = Color.green;
            }
            else if (message.Contains("결과"))
            {
                statusText.color = Color.yellow;
            }
            else
            {
                statusText.color = Color.white;
            }
        }

        #endregion
    }
}