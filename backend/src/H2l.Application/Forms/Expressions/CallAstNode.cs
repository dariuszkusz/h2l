namespace H2l.Application.Forms.Expressions;

public sealed record CallAstNode(string Callee, IReadOnlyList<AstNode> Arguments) : AstNode;
