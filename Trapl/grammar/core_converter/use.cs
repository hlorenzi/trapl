using System.Collections.Generic;


namespace Trapl.Grammar
{
    public partial class CoreConverter
    {
        public static Core.UseDirective ConvertUseDirective(Core.Session session, ASTNodeUse useNode)
        {
            var useAllNode = useNode as ASTNodeUseAll;
            if (useAllNode != null)
            {
                var useAll = new Core.UseDirectiveAll();
                useAll.name = ConvertName(useAllNode.path);
                return useAll;
            }
            else
                throw new System.NotImplementedException();
        }


        public static List<Core.UseDirective> FindOuterUseDirectives(Core.Session session, ASTNode startNode)
        {
            var result = new List<Core.UseDirective>();

            if (startNode.parent == null)
                return result;

            var lastNode = startNode;
            var curNode = startNode.parent;
            while (curNode != null)
            {
                foreach (var child in curNode.EnumerateChildren())
                {
                    if (child is ASTNodeUse)
                        result.Add(ConvertUseDirective(session, (ASTNodeUse)child));

                    if (child == lastNode)
                        break;
                }

                lastNode = curNode;
                curNode = curNode.parent;
            }

            return result;
        }
    }
}
