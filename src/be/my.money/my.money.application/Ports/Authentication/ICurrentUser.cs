namespace my.money.application.Ports.Authentication;

public interface ICurrentUser
{
    string? UserId { get; }
    bool IsAuthenticated { get; }
}
