using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Trapl.Semantics
{
    public static class UtilASTNumber
    {
        public static string ParseNumberValue(Grammar.ASTNode numberASTNode)
        {
            if (numberASTNode.kind != Grammar.ASTNodeKind.NumberLiteral)
                throw new InternalException("node is not a NumberLiteral");

            var numBase = 10;
            var numStr = numberASTNode.GetExcerpt();

            if (numStr.StartsWith("0b"))
            {
                numBase = 2;
                numStr = numStr.Substring(2);
            }
            else if (numStr.StartsWith("0o"))
            {
                numBase = 8;
                numStr = numStr.Substring(2);
            }
            else if (numStr.StartsWith("0x"))
            {
                numBase = 16;
                numStr = numStr.Substring(2);
            }

            return Convert.ToInt64(numStr, numBase).ToString();
        }


        public static Type ParseNumberType(Infrastructure.Session session, Grammar.ASTNode numberASTNode)
        {
            if (numberASTNode.kind != Grammar.ASTNodeKind.NumberLiteral)
                throw new InternalException("node is not a NumberLiteral");

            return new TypeStruct(session.primitiveInt);
        }
    }
}
