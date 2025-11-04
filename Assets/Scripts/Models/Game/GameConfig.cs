namespace MathHighLow.Models
{
    /// <summary>
    /// [학습 포인트] 게임 설정을 한 곳에 모으기
    /// 
    /// 게임의 모든 설정값을 관리합니다.
    /// 이렇게 하면 밸런스 조정이 쉬워집니다.
    /// </summary>
    [System.Serializable]
    public class GameConfig
    {
        // 시간 설정
        public float DealInterval = 0.2f;           // 카드 분배 간격
        public float SubmissionUnlockTime = 30f;    // 제출 잠금 시간
        public float RoundDuration = 180f;          // 라운드 제한 시간
        public float ResultsDisplayDuration = 10f;  // 결과 표시 시간

        // 카드 설정
        public int InitialCardCount = 3;            // 초기 카드 수
        public int NumberCopiesPerValue = 4;        // 숫자당 카드 복사본 수
        public int MultiplyCardsPerRound = 2;       // 라운드당 × 카드 수
        public int SquareRootCardsPerRound = 2;     // 라운드당 √ 카드 수

        // 게임 룰
        public int StartingCredits = 20;            // 초기 자금
        public int MinBet = 1;                      // 최소 베팅
        public int MaxBet = 5;                      // 최대 베팅
        public int[] TargetValues = { 1, 20 };      // 목표값 옵션

        /// <summary>
        /// 기본 설정을 반환합니다.
        /// </summary>
        public static GameConfig Default()
        {
            return new GameConfig();
        }

        /// <summary>
        /// 쉬운 난이도 설정
        /// </summary>
        public static GameConfig Easy()
        {
            return new GameConfig
            {
                RoundDuration = 300f,
                SubmissionUnlockTime = 20f,
                StartingCredits = 30,
                TargetValues = new[] { 5, 10 }
            };
        }

        /// <summary>
        /// 어려운 난이도 설정
        /// </summary>
        public static GameConfig Hard()
        {
            return new GameConfig
            {
                RoundDuration = 120f,
                SubmissionUnlockTime = 40f,
                StartingCredits = 15,
                MinBet = 2,
                MaxBet = 10,
                TargetValues = new[] { 1, 50, 100 }
            };
        }
    }

    /// <summary>
    /// [학습 포인트] 결과 데이터 구조
    /// 
    /// 한 라운드의 결과를 담는 데이터 클래스입니다.
    /// 승패 판정, 점수 변동 등의 정보를 포함합니다.
    /// </summary>
    [System.Serializable]
    public class RoundResult
    {
        /// <summary>
        /// 목표값
        /// </summary>
        public int Target { get; set; }

        /// <summary>
        /// 베팅 금액
        /// </summary>
        public int Bet { get; set; }

        /// <summary>
        /// 플레이어 수식
        /// </summary>
        public string PlayerExpression { get; set; }

        /// <summary>
        /// 플레이어 계산 결과
        /// </summary>
        public float PlayerValue { get; set; }

        /// <summary>
        /// 플레이어 차이 (|결과 - 목표|)
        /// </summary>
        public float PlayerDifference { get; set; }

        /// <summary>
        /// 플레이어 에러 메시지
        /// </summary>
        public string PlayerError { get; set; }

        /// <summary>
        /// AI 수식
        /// </summary>
        public string AIExpression { get; set; }

        /// <summary>
        /// AI 계산 결과
        /// </summary>
        public float AIValue { get; set; }

        /// <summary>
        /// AI 차이
        /// </summary>
        public float AIDifference { get; set; }

        /// <summary>
        /// AI 에러 메시지
        /// </summary>
        public string AIError { get; set; }

        /// <summary>
        /// 승자 ("Player", "AI", "Draw", "Invalid")
        /// </summary>
        public string Winner { get; set; }

        /// <summary>
        /// 플레이어 점수 변화
        /// </summary>
        public int PlayerScoreChange { get; set; }

        /// <summary>
        /// AI 점수 변화
        /// </summary>
        public int AIScoreChange { get; set; }

        /// <summary>
        /// 생성자
        /// </summary>
        public RoundResult()
        {
            PlayerExpression = "";
            AIExpression = "";
            PlayerError = "";
            AIError = "";
            Winner = "Invalid";
        }

        /// <summary>
        /// 결과 요약 문자열
        /// </summary>
        public string GetSummary()
        {
            return Winner switch
            {
                "Player" => $"플레이어 승리! (+${PlayerScoreChange})",
                "AI" => $"AI 승리! (-${-PlayerScoreChange})",
                "Draw" => "무승부",
                _ => "라운드 무효"
            };
        }

        /// <summary>
        /// 상세 결과 문자열
        /// </summary>
        public string GetDetail()
        {
            string detail = $"목표: {Target} | 베팅: ${Bet}\n";
            
            if (!string.IsNullOrEmpty(PlayerError))
            {
                detail += $"플레이어: {PlayerError}\n";
            }
            else
            {
                detail += $"플레이어: {PlayerExpression} = {PlayerValue:F2} (차이: {PlayerDifference:F2})\n";
            }

            if (!string.IsNullOrEmpty(AIError))
            {
                detail += $"AI: {AIError}";
            }
            else
            {
                detail += $"AI: {AIExpression} = {AIValue:F2} (차이: {AIDifference:F2})";
            }

            return detail;
        }
    }
}
