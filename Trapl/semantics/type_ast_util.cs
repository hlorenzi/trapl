using System.Collections.Generic;
using Trapl.Diagnostics;
using System;


namespace Trapl.Semantics
{
    public static class TypeASTUtil
    {
        public static Type Resolve(Infrastructure.Session session, Grammar.ASTNode node)
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
                    resolvedType = new TypeUnconstrained();
                }
                else
                {
                    // Find a matching TopDecl.
                    var matchingTopDecl = TopDeclFinder.FindStruct(session, nameASTNode);

                    // Build a Type with the matching TopDecl's struct.
                    resolvedType = new TypeStruct((DefStruct)matchingTopDecl.def);
                }
            }
            else if (node.kind == Grammar.ASTNodeKind.TupleType)
            {
                var tupleType = new TypeTuple();
                foreach (var elemNode in node.EnumerateChildren())
                {
                    if (IsTypeNode(elemNode.kind))
                        tupleType.elementTypes.Add(Resolve(session, elemNode));
                }
                resolvedType = tupleType;
            }
            else
                throw new InternalException("unreachable");

            // Wrap the type into as many pointer types as specified.
            foreach (var child in node.EnumerateChildren())
            {
                if (child.kind == Grammar.ASTNodeKind.Operator)
                    resolvedType = new TypePointer(resolvedType);
            }

            return resolvedType;
        }


        public static bool IsTypeNode(Grammar.ASTNodeKind kind)
        {
            return 
                (kind == Grammar.ASTNodeKind.Type ||
                kind == Grammar.ASTNodeKind.TupleType);
        }


        public static string GetString(Grammar.ASTNode typeNode)
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
