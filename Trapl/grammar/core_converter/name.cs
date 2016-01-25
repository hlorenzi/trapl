using System.Collections.Generic;


namespace Trapl.Grammar
{
    public partial class CoreConverter
    {
        public static Core.Name ConvertName(ASTNodeName nameNode)
        {
            var identifiers = new List<string>();

            foreach (var ident in nameNode.path.identifiers)
                identifiers.Add(ident.GetExcerpt());

            return Core.Name.FromPath(identifiers.ToArray());
        }


        public static Core.Name ConvertName(ASTNodePath pathNode)
        {
            var identifiers = new List<string>();

            foreach (var ident in pathNode.identifiers)
                identifiers.Add(ident.GetExcerpt());

            return Core.Name.FromPath(identifiers.ToArray());
        }
    }
}
