using UnityEngine;
using UnityEngine.UI;
using TMPro;
using MathHighLow.Models;
using MathHighLow.Services;

namespace MathHighLow.Views
{
    /// <summary>
    /// ✅ 수정: 모든 카드 타입 처리
    /// - 숫자 카드: 클릭 가능
    /// - 연산자 카드: 클릭 가능 (새로운 기능!)
    /// - 특수 카드: × 와 √ 모두 클릭 가능
    /// </summary>
    [RequireComponent(typeof(Button))]
    public class CardView : MonoBehaviour
    {
        [Header("UI 참조")]
        [SerializeField] private TextMeshProUGUI displayText;
        [SerializeField] private Image backgroundImage;
        [SerializeField] private Button button;

        [Header("카드 색상")]
        [SerializeField] private Color playerNumberCardColor = Color.white;
        [SerializeField] private Color playerOperatorCardColor = new Color(0.8f, 1f, 0.8f); // 연두색
        [SerializeField] private Color specialCardColor = new Color(1f, 0.9f, 0.5f); // 노란색
        [SerializeField] private Color aiCardColor = new Color(0.9f, 0.9f, 1f); // 연한 파란색

        private Card card;
        private bool isPlayerCard;

        void OnEnable()
        {
            GameEvents.OnRoundStarted += ResetCard;
            GameEvents.OnResetClicked += ResetCard;
            GameEvents.OnCardClicked += HandleCardUsed;
            GameEvents.OnSpecialCardConsumed += HandleSpecialCardConsumed;
        }

        void OnDisable()
        {
            GameEvents.OnRoundStarted -= ResetCard;
            GameEvents.OnResetClicked -= ResetCard;
            GameEvents.OnCardClicked -= HandleCardUsed;
            GameEvents.OnSpecialCardConsumed -= HandleSpecialCardConsumed;
        }

        /// <summary>
        /// ✅ 수정: 모든 카드 타입 초기화
        /// </summary>
        public void Initialize(Card cardData, bool isPlayer)
        {
            this.card = cardData;
            this.isPlayerCard = isPlayer;

            // null 체크
            if (card == null)
            {
                Debug.LogError("[CardView] 카드 데이터가 null입니다!");
                return;
            }

            // 1. 텍스트 설정
            displayText.text = card.GetDisplayText();

            // 2. 배경색 설정 (카드 타입별)
            if (!isPlayer)
            {
                // AI 카드는 모두 같은 색
                backgroundImage.color = aiCardColor;
            }
            else if (card is NumberCard)
            {
                // 플레이어 숫자 카드
                backgroundImage.color = playerNumberCardColor;
            }
            else if (card is OperatorCard)
            {
                // ✅ 플레이어 연산자 카드 (새로운 색상!)
                backgroundImage.color = playerOperatorCardColor;
            }
            else if (card is SpecialCard)
            {
                // 특수 카드
                backgroundImage.color = specialCardColor;
            }

            // 3. 버튼 이벤트 설정
            if (button == null)
            {
                button = GetComponent<Button>();
            }

            button.onClick.RemoveAllListeners();

            // ✅ 수정: 숫자 카드 + 연산자 카드 + 특수 카드(×, √) 클릭 가능
            if (isPlayer && (card is NumberCard || card is OperatorCard || IsClickableSpecial(card)))
            {
                button.interactable = true;
                button.onClick.AddListener(HandleClick);
            }
            else
            {
                // AI 카드나 특수 카드는 클릭 불가
                button.interactable = false;
            }
        }

        private void HandleClick()
        {
            // null 체크
            if (card == null)
            {
                Debug.LogWarning("[CardView] 클릭된 카드가 null입니다.");
                return;
            }

            if (card is SpecialCard specialCard && !IsClickableSpecial(card))
            {
                Debug.LogWarning("[CardView] 클릭할 수 없는 특수 카드입니다.");
                return;
            }

            // 이벤트 발행
            GameEvents.InvokeCardClicked(this.card);
        }

        private void HandleCardUsed(Card usedCard)
        {
            // 내가 클릭된 카드라면 비활성화
            if (usedCard == this.card)
            {
                if (card is SpecialCard specialCard &&
                    IsClickableSpecial(card) &&
                    !specialCard.IsConsumed)
                {
                    // 특수 카드는 실제로 사용되기 전까지는 다시 클릭 가능
                    button.interactable = true;
                    backgroundImage.color = specialCardColor;
                    return;
                }

                button.interactable = false;
                backgroundImage.color = Color.gray;
            }
        }

        private void ResetCard()
        {
            // 플레이어의 숫자/연산자 카드만 다시 활성화
            if (isPlayerCard && (card is NumberCard || card is OperatorCard))
            {
                button.interactable = true;

                // 색상 복원
                if (card is NumberCard)
                {
                    backgroundImage.color = playerNumberCardColor;
                }
                else if (card is OperatorCard)
                {
                    backgroundImage.color = playerOperatorCardColor;
                }
            }
            else if (isPlayerCard && card is SpecialCard specialCard &&
                     IsClickableSpecial(card))
            {
                specialCard.ResetUsage();
                button.interactable = true;
                backgroundImage.color = specialCardColor;
            }
        }

        private bool IsClickableSpecial(Card targetCard)
        {
            if (targetCard is SpecialCard specialCard)
            {
                return specialCard.Type == SpecialCard.SpecialType.Multiply ||
                       specialCard.Type == SpecialCard.SpecialType.SquareRoot;
            }

            return false;
        }

        private void HandleSpecialCardConsumed(SpecialCard consumedCard)
        {
            if (card == consumedCard)
            {
                button.interactable = false;
                backgroundImage.color = Color.gray;
            }
        }
    }
}