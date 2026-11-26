namespace H2l.Application.Auth.Commands;

public sealed record ActivateAccountCommand(string Token, string Password);
