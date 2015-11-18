using Trapl.Diagnostics;
using Trapl.Infrastructure;


namespace Trapl.Semantics
{
    public static class TypeUtil
    {
        public static Type ResolveFromAST(Infrastructure.Session session, Grammar.ASTNode node, bool mustBeResolved)
        {
            if (!IsTypeNode(node.kind))
                throw new InternalException("node is not a Type");

            Type resolvedType = null;

            if (node.kind == Grammar.ASTNodeKind.Type)
            {
                // Get the name node. (ex.: '&&Structure::List::<Int32>' will get 'Structure::List::<Int32>')
                var nameASTNode = node.Child(0);

                // Get the name's string, if the path has only one level.
                string onlyName = null;
                if (nameASTNode.Child(0).ChildNumber() == 1)
                    onlyName = nameASTNode.Child(0).Child(0).GetExcerpt();

                // Check if it is a placeholder type.
                if (onlyName == "_")
                {
                    if (mustBeResolved)
                    {
                        session.diagn.Add(MessageKind.Error, MessageCode.UndeclaredTemplate,
                            "type must be fully resolved", nameASTNode.Child(0).Span());
                        throw new CheckException();
                    }

                    resolvedType = new TypePlaceholder();
                }
                else
                {
                    var structType = new TypeStruct();
                    structType.nameInference.pathASTNode = nameASTNode.Child(0);
                    structType.nameInference.template = TemplateUtil.ResolveFromNameAST(session, nameASTNode, mustBeResolved);

                    // Find structs with the given name.
                    structType.potentialStructs = session.structDecls.GetDeclsClone(nameASTNode.Child(0));
                    if (structType.potentialStructs.Count == 0)
                    {
                        session.diagn.Add(MessageKind.Error, MessageCode.UndeclaredTemplate,
                            "type '" + PathUtil.GetDisplayString(nameASTNode.Child(0)) + "' " +
                            "is not declared", nameASTNode.Child(0).Span());
                        throw new CheckException();
                    }

                    if (mustBeResolved)
                        structType.nameInference.template.unconstrained = false;

                    resolvedType = structType;

                    if (structType.nameInference.template.IsFullyResolved())
                    {
                        // Filter structs by template compatibility.
                        structType.potentialStructs =
                            structType.potentialStructs.FindAll(d => d.name.template.IsMatch(structType.nameInference.template));

                        if (structType.potentialStructs.Count == 0)
                        {
                            session.diagn.Add(MessageKind.Error, MessageCode.UndeclaredTemplate,
                                "no '" + PathUtil.GetDisplayString(nameASTNode.Child(0)) + "' declaration " +
                                "accepts this template", nameASTNode.Span());
                            throw new CheckException();
                        }
                        else if (structType.potentialStructs.Count > 1)
                        {
                            session.diagn.Add(MessageKind.Error, MessageCode.UndeclaredTemplate,
                                "multiple '" + PathUtil.GetDisplayString(nameASTNode.Child(0)) + "' " +
                                "declarations accept this template", nameASTNode.Span());
                            throw new CheckException();
                        }

                        // Ask the declaration to resolve itself.
                        structType.potentialStructs[0].Resolve(session);
                    }
                    else if (mustBeResolved)
                    {
                        session.diagn.Add(MessageKind.Error, MessageCode.UndeclaredTemplate,
                            "type must be fully resolved", nameASTNode.Span());
                        throw new CheckException();
                    }
                }
            }
            else if (node.kind == Grammar.ASTNodeKind.TupleType)
            {
                var tupleType = new TypeTuple();
                foreach (var elemNode in node.EnumerateChildren())
                {
                    if (IsTypeNode(elemNode.kind))
                        tupleType.elementTypes.Add(ResolveFromAST(session, elemNode, mustBeResolved));
                }
                resolvedType = tupleType;
            }
            else
                throw new InternalException("unreachable");

            // Wrap the type into as many pointer types as specified.
            foreach (var child in node.EnumerateChildren())
            {
                if (child.kind == Grammar.ASTNodeKind.Operator)
                    resolvedType = new TypeReference(resolvedType);
            }

            return resolvedType;
        }


        public static bool IsTypeNode(Grammar.ASTNodeKind kind)
        {
            return 
                (kind == Grammar.ASTNodeKind.Type ||
                kind == Grammar.ASTNodeKind.TupleType);
        }


        public static string GetDisplayString(Grammar.ASTNode typeNode)
        {
            var result = "";
            var prefix = "";

            foreach (var child in typeNode.EnumerateChildren())
            {
                if (child.kind == Grammar.ASTNodeKind.Name)
                    result += child.GetExcerpt();
                else if (child.kind == Grammar.ASTNodeKind.Operator)
                    prefix += child.GetExcerpt();
                else
                    result += "?";
            }

            return prefix + result;
        }
    }
}
