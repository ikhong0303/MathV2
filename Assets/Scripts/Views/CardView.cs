using UnityEngine;
using UnityEngine.UI;
using TMPro;
using MathHighLow.Models;
using MathHighLow.Services;

namespace MathHighLow.Views
{
    /// <summary>
    /// [학습 포인트] 프리팹 스크립트
    /// 
    /// 카드 프리팹에 부착되어 개별 카드의 UI를 담당합니다.
    /// 자신의 Card 데이터를 소유하고, 클릭 시 GameEvents를 발행합니다.
    /// </summary>
    [RequireComponent(typeof(Button))]
    public class CardView : MonoBehaviour
    {
        [Header("UI 참조")]
        [SerializeField] private TextMeshProUGUI displayText;
        [SerializeField] private Image backgroundImage;
        [SerializeField] private Button button;

        [Header("카드 설정")]
        [SerializeField] private Color playerCardColor = Color.white;
        [SerializeField] private Color aiCardColor = new Color(0.9f, 0.9f, 1f);

        private Card card;
        private bool isPlayerCard;

        void OnEnable()
        {
            // 리셋/라운드 시작 시 다시 활성화
            GameEvents.OnRoundStarted += ResetCard;
            GameEvents.OnResetClicked += ResetCard;

            // 다른 카드가 클릭되었을 때가 아닌, "내가" 클릭되었을 때 비활성화
            GameEvents.OnCardClicked += HandleCardUsed;
        }

        void OnDisable()
        {
            GameEvents.OnRoundStarted -= ResetCard;
            GameEvents.OnResetClicked -= ResetCard;
            GameEvents.OnCardClicked -= HandleCardUsed;
        }

        /// <summary>
        /// HandView(또는 GameView)가 카드를 생성할 때 호출합니다.
        /// </summary>
        public void Initialize(Card cardData, bool isPlayer)
        {
            this.card = cardData;
            this.isPlayerCard = isPlayer;

            // 1. 텍스트 설정
            displayText.text = card.GetDisplayText();

            // 2. 배경색 설정
            backgroundImage.color = isPlayer ? playerCardColor : aiCardColor;

            // 3. 버튼 이벤트 설정
            button.onClick.RemoveAllListeners();
            if (isPlayer && card is NumberCard)
            {
                // 플레이어의 숫자 카드만 클릭 가능
                button.interactable = true;
                button.onClick.AddListener(HandleClick);
            }
            else
            {
                // AI 카드나 특수 카드(UI 버튼이 따로 있음)는 클릭 불가
                button.interactable = false;
            }
        }

        /// <summary>
        /// 플레이어가 이 카드를 클릭했을 때
        /// </summary>
        private void HandleClick()
        {
            // [학습 포인트] 이벤트 발행
            // 이 카드가 클릭되었음을 컨트롤러(PlayerController)에 알림
            GameEvents.InvokeCardClicked(this.card);
        }

        /// <summary>
        /// 카드가 사용되었을 때 (PlayerController가 수식에 추가했을 때)
        /// </summary>
        private void HandleCardUsed(Card usedCard)
        {
            // 내가 클릭된 카드라면 비활성화
            if (usedCard == this.card)
            {
                button.interactable = false;
            }
        }

        /// <summary>
        /// 라운드 시작 또는 리셋 시 카드 상태 초기화
        /// </summary>
        private void ResetCard()
        {
            // 플레이어의 숫자 카드만 다시 활성화
            if (isPlayerCard && card is NumberCard)
            {
                button.interactable = true;
            }
        }
    }
}