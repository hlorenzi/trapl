using System;


namespace Trapl.Semantics
{
    public static class PathUtil
    {
        public static bool Compare(Grammar.ASTNode path1, Grammar.ASTNode path2)
        {
            if (path1.kind != Grammar.ASTNodeKind.Path)
                throw new InvalidOperationException("node is not a Path");

            if (path2.kind != Grammar.ASTNodeKind.Path)
                throw new InvalidOperationException("node is not a Path");

            if (path1.ChildNumber() != path2.ChildNumber())
                return false;

            for (var i = 0; i < path1.ChildNumber(); i++)
            {
                if (path1.Child(i).GetExcerpt() != path2.Child(i).GetExcerpt())
                    return false;
            }

            return true;
        }


        public static string GetDisplayString(Grammar.ASTNode path)
        {
            if (path.kind != Grammar.ASTNodeKind.Path)
                throw new InvalidOperationException("node is not a Path");

            var result = "";

            for (var i = 0; i < path.ChildNumber(); i++)
            {
                result += path.Child(i).GetExcerpt();
                if (i < path.ChildNumber() - 1)
                    result += "::";
            }

            return result;
        }
    }
}
