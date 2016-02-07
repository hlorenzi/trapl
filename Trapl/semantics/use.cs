namespace Trapl.Semantics
{
    public static class UseDirectiveResolver
    {
        public static Core.UseDirective Resolve(Grammar.ASTNodeUse useNode)
        {
            var useAllNode = useNode as Grammar.ASTNodeUseAll;
            if (useAllNode != null)
            {
                var useAll = new Core.UseDirectiveAll();
                useAll.name = NameResolver.ResolvePath(useAllNode.path);
                return useAll;
            }
            else
                throw new System.NotImplementedException();
        }
    }
}
