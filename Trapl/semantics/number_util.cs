using System;
using Trapl.Infrastructure;


namespace Trapl.Semantics
{
    public static class NumberUtil
    {
        public static string ResolveValueFromAST(Grammar.ASTNode numberASTNode)
        {
            if (numberASTNode.kind != Grammar.ASTNodeKind.NumberLiteral)
                throw new InternalException("node is not a NumberLiteral");

            string prefix, value, suffix;
            Grammar.Number.GetParts(numberASTNode.GetExcerpt(), out prefix, out value, out suffix);

            return Convert.ToInt64(Grammar.Number.GetValueWithoutSpecials(value), Grammar.Number.GetBase(prefix)).ToString();
        }


        public static Infrastructure.Type ResolveTypeFromAST(Infrastructure.Session session, Grammar.ASTNode numberASTNode)
        {
            if (numberASTNode.kind != Grammar.ASTNodeKind.NumberLiteral)
                throw new InternalException("node is not a NumberLiteral");

            return new TypeStruct(session.primitiveInt);
        }
    }
}
