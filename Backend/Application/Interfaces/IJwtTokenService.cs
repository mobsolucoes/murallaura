namespace HashtagWall.Application.Interfaces;

public interface IJwtTokenService
{
    string CreateToken(Guid userId, string username);
}
