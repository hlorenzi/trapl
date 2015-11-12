using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Trapl.Semantics
{
    public static class UtilASTName
    {
        public static string GetString(Grammar.ASTNode name)
        {
            if (name.kind != Grammar.ASTNodeKind.Name)
                throw new InvalidOperationException("node is not a Name");

            var result = UtilASTPath.GetString(name.Child(0));
            if (name.ChildIs(1, Grammar.ASTNodeKind.TemplateList))
                result += GetStringRecursive(name.Child(1));

            return result;
        }


        private static string GetStringRecursive(Grammar.ASTNode node)
        {
            var result = "";
            if (node.kind == Grammar.ASTNodeKind.TemplateList)
            {
                result += "<";
                for (int i = 0; i < node.ChildNumber(); i++)
                {
                    result += GetStringRecursive(node.Child(i));
                    if (i < node.ChildNumber() - 1)
                        result += ", ";
                }
                result += ">";
            }
            else if (node.kind == Grammar.ASTNodeKind.TemplateParameter)
                result += GetStringRecursive(node.Child(0));
            else if (node.kind == Grammar.ASTNodeKind.Type)
                result += GetString(node.Child(0));
            else
                result += "???";

            return result;
        }
    }
}
