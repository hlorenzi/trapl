using System.Collections.Generic;


namespace Trapl.Semantics
{
    public static class TypeResolver
    {
        public static Core.Type Resolve(
            Core.Session session,
            Grammar.ASTNodeType typeNode,
            IList<Core.UseDirective> useDirectives)
        {
            var typeStructNode = typeNode as Grammar.ASTNodeTypeStruct;
            if (typeStructNode != null)
            {
                var name = NameResolver.Resolve(typeStructNode.name);

                var foundDecls = session.GetDeclsWithUseDirectives(name, typeStructNode.name.path.isAbsolute, useDirectives);
                if (!session.ValidateSingleDecl(foundDecls, name, typeStructNode.name.GetSpan()))
                    return new Core.TypeError();

                if (!session.ValidateAsType(foundDecls[0], name, typeStructNode.name.GetSpan()))
                    return new Core.TypeError();

                return Core.TypeStruct.Of(foundDecls[0].index);
            }

            var typeTupleNode = typeNode as Grammar.ASTNodeTypeTuple;
            if (typeTupleNode != null)
            {
                var elementTypes = new Core.Type[typeTupleNode.elements.Count];
                for (var i = 0; i < typeTupleNode.elements.Count; i++)
                    elementTypes[i] = Resolve(session, typeTupleNode.elements[i], useDirectives);

                return Core.TypeTuple.Of(elementTypes);
            }

            throw new System.NotImplementedException();
        }
    }
}
