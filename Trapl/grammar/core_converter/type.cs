using System.Collections.Generic;


namespace Trapl.Grammar
{
    public partial class CoreConverter
    {
        public Core.Type ConvertType(ASTNodeType typeNode, IList<Core.UseDirective> useDirectives)
        {
            var typeStructNode = typeNode as ASTNodeTypeStruct;
            if (typeStructNode != null)
            {
                var name = ConvertName(typeStructNode.name);

                var foundDecls = session.GetDeclsWithUseDirectives(name, typeStructNode.name.path.isRooted, useDirectives);
                if (!session.ValidateSingleDecl(foundDecls, name, typeStructNode.name.GetSpan()))
                    return new Core.TypeError();

                if (!session.ValidateAsType(foundDecls[0], name, typeStructNode.name.GetSpan()))
                    return new Core.TypeError();

                return Core.TypeStruct.Of(foundDecls[0].index);
            }

            var typeTupleNode = typeNode as ASTNodeTypeTuple;
            if (typeTupleNode != null)
            {
                var elementTypes = new Core.Type[typeTupleNode.elements.Count];
                for (var i = 0; i < typeTupleNode.elements.Count; i++)
                    elementTypes[i] = ConvertType(typeTupleNode.elements[i], useDirectives);

                return Core.TypeTuple.Of(elementTypes);
            }

            throw new System.NotImplementedException();
        }
    }
}
