using System;


namespace Trapl.Semantics
{
    public static class NameUtil
    {
        public static string GetDisplayString(Grammar.ASTNode name)
        {
            if (name.kind != Grammar.ASTNodeKind.Name)
                throw new InvalidOperationException("node is not a Name");

            var result = PathUtil.GetDisplayString(name.Child(0));
            if (name.ChildIs(1, Grammar.ASTNodeKind.TemplateList))
                result += GetDisplayStringRecursive(name.Child(1));

            return result;
        }


        private static string GetDisplayStringRecursive(Grammar.ASTNode node)
        {
            var result = "";
            if (node.kind == Grammar.ASTNodeKind.TemplateList)
            {
                result += "<";
                for (int i = 0; i < node.ChildNumber(); i++)
                {
                    result += GetDisplayStringRecursive(node.Child(i));
                    if (i < node.ChildNumber() - 1)
                        result += ", ";
                }
                result += ">";
            }
            else if (node.kind == Grammar.ASTNodeKind.TemplateParameter)
                result += GetDisplayStringRecursive(node.Child(0));
            else if (node.kind == Grammar.ASTNodeKind.Type)
                result += GetDisplayString(node.Child(0));
            else
                result += "???";

            return result;
        }
    }
}
