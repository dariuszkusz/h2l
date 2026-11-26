namespace H2l.Application.Auth.Commands;

public sealed record ActivateAccountRequest(string Token, string Password);
