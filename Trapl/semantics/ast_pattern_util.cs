
namespace Trapl.Semantics
{
    public static class ASTPatternUtil
    {
        public static bool IsEmpty(Grammar.ASTNode patternNode)
        {
            return (patternNode.ChildNumber() == 0);
        }


        public static bool IsGeneric(Grammar.ASTNode patternNode)
        {
            return IsGenericRecursive(patternNode);
        }


        private static bool IsGenericRecursive(Grammar.ASTNode node)
        {
            var result = false;
            foreach (var child in node.EnumerateChildren())
            {
                if (child.kind == Grammar.ASTNodeKind.GenericIdentifier)
                    return true;
                else
                    result = (result || IsGenericRecursive(child));
            }
            return result;
        }


        public static string GetString(Grammar.ASTNode patternNode)
        {
            return GetStringRecursive(patternNode);
        }


        private static string GetStringRecursive(Grammar.ASTNode node)
        {
            if (node.kind == Grammar.ASTNodeKind.ParameterPattern)
            {
                var result = "<";
                for (int i = 0; i < node.ChildNumber(); i++)
                {
                    result += GetStringRecursive(node.Child(i));
                    if (i < node.ChildNumber() - 1)
                        result += ", ";
                }
                return result + ">";
            }
            else
            {
                return ASTTypeUtil.GetString(node);
            }
        }


        public static int GetGenericParameterNumber(Grammar.ASTNode patternNode)
        {
            return GetGenericParameterNumberRecursive(patternNode);
        }


        private static int GetGenericParameterNumberRecursive(Grammar.ASTNode node)
        {
            var result = 0;
            if (node.kind == Grammar.ASTNodeKind.GenericIdentifier)
                result += 1;

            foreach (var child in node.EnumerateChildren())
                result += GetGenericParameterNumberRecursive(child);

            return result;
        }
    }
}
