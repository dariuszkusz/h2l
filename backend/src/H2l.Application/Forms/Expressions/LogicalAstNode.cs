namespace H2l.Application.Forms.Expressions;

public sealed record LogicalAstNode(string Operator, AstNode Left, AstNode Right) : AstNode;
