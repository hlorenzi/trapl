namespace Trapl.Grammar
{
    public partial class CoreConverter
    {
        public static Core.Type ConvertType(Core.Session session, ASTNodeType typeNode)
        {
            var typeStructNode = typeNode as ASTNodeTypeStruct;
            if (typeStructNode != null)
            {
                var name = ConvertName(typeStructNode.name);
                var useDirectives = FindOuterUseDirectives(session, typeStructNode);

                var foundDecls = session.GetDeclsWithUseDirectives(name, typeStructNode.name.path.isRooted, useDirectives);
                if (!session.ValidateSingleDecl(foundDecls, name, typeStructNode.name.GetSpan()))
                    return new Core.TypeError();

                if (!session.ValidateType(foundDecls[0], name, typeStructNode.name.GetSpan()))
                    return new Core.TypeError();

                return Core.TypeStruct.Of(foundDecls[0].index);
            }
            else
                throw new System.NotImplementedException();
        }
    }
}
