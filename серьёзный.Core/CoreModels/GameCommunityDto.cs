namespace серьёзный.Core.CoreModels;

public enum GameCommunityAction
{
    GetDetails,
    RateGame,
    AddReview,
    AskQuestion
}

public class GameCommunityRequestDto
{
    public GameCommunityAction Action { get; set; }

    public string GameName { get; set; } = "";

    public int Stars { get; set; }

    public string ReviewText { get; set; } = "";

    public string QuestionText { get; set; } = "";
}

public class GameReviewDto
{
    public string PlayerName { get; set; } = "";

    public int Stars { get; set; }

    public string Text { get; set; } = "";

    public DateTime Time { get; set; }
}

public class GameHelpQuestionDto
{
    public long Id { get; set; }

    public string Question { get; set; } = "";

    public string? Answer { get; set; }

    public bool Answered { get; set; }

    public string AskedBy { get; set; } = "";

    public DateTime Time { get; set; }
}

public class GameCommunityDetailsDto
{
    public string GameName { get; set; } = "";

    public double AverageRating { get; set; }

    public int RatingCount { get; set; }

    public int MyRating { get; set; }

    public List<GameReviewDto> Reviews { get; set; } = new();

    public int ReviewCount { get; set; }

    public List<GameHelpQuestionDto> Help { get; set; } = new();
}

public class GameCommunityResultDto
{
    public bool Success { get; set; }

    public string? Error { get; set; }

    public GameCommunityDetailsDto? Details { get; set; }
}