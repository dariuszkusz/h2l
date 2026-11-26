namespace H2l.Application.Forms.Expressions;

public sealed record UnaryAstNode(string Operator, AstNode Argument) : AstNode;
