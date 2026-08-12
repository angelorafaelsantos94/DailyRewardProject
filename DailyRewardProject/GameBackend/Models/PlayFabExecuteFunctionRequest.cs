namespace GameBackend.Models;

public class PlayFabExecuteFunctionRequest
{
    public FunctionArgument? FunctionArgument { get; set; }

    public CallerEntityProfile? CallerEntityProfile { get; set; }

    public TitleAuthenticationContext? TitleAuthenticationContext { get; set; }
}

public class FunctionArgument
{
    public string? QuestId { get; set; }

    public string? ClientRequestId { get; set; }

    public string? ClientPlayerId { get; set; }
}

public class CallerEntityProfile
{
    public Entity? Entity { get; set; }
}

public class Entity
{
    public string? Id { get; set; }

    public string? Type { get; set; }
}

public class TitleAuthenticationContext
{
    public string? Id { get; set; }

    public string? EntityToken { get; set; }
}