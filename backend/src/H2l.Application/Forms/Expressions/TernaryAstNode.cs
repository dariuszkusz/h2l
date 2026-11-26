namespace H2l.Application.Forms.Expressions;

public sealed record TernaryAstNode(AstNode Condition, AstNode Consequent, AstNode Alternate) : AstNode;
