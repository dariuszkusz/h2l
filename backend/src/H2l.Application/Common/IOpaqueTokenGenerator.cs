namespace H2l.Application.Common;

public interface IOpaqueTokenGenerator
{
    string GenerateToken();

    string ComputeSha256Hash(string rawToken);
}
