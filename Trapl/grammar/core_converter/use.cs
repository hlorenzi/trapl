using System.Collections.Generic;


namespace Trapl.Grammar
{
    public partial class CoreConverter
    {
        public Core.UseDirective ConvertUseDirective(ASTNodeUse useNode)
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
    }
}
