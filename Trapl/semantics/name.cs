using System.Collections.Generic;


namespace Trapl.Semantics
{
    public static class NameResolver
    {
        public static Core.Name Resolve(Grammar.ASTNodeName nameNode)
        {
            var identifiers = new List<string>();

            foreach (var ident in nameNode.path.identifiers)
                identifiers.Add(ident.GetExcerpt());

            return Core.Name.FromPath(identifiers.ToArray());
        }


        public static Core.Name ResolvePath(Grammar.ASTNodePath pathNode)
        {
            var identifiers = new List<string>();

            foreach (var ident in pathNode.identifiers)
                identifiers.Add(ident.GetExcerpt());

            return Core.Name.FromPath(identifiers.ToArray());
        }
    }
}
