namespace H2l.Application.Forms.Expressions;

public sealed record MemberAstNode(AstNode Target, string Property) : AstNode;
