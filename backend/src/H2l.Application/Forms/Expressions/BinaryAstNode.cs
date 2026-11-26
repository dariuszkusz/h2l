namespace H2l.Application.Forms.Expressions;

public sealed record BinaryAstNode(string Operator, AstNode Left, AstNode Right) : AstNode;
