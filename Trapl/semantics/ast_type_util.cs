using System.Collections.Generic;
using Trapl.Diagnostics;
using System;


namespace Trapl.Semantics
{
    public static class ASTTypeUtil
    {
        public static Type Resolve(Infrastructure.Session session, Grammar.ASTNode node)
        {
            if (node.kind != Grammar.ASTNodeKind.Type)
                throw new InternalException("node is not a Type");

            // Read the indirection level of the type. (ex.: '&&Int32' has 2 levels)
            var indirectionLevels = 0;
            foreach (var child in node.EnumerateChildren())
            {
                if (child.kind == Grammar.ASTNodeKind.Operator)
                    indirectionLevels++;
            }

            // Get the name node. (ex.: '&&Structure::List::<Int32>' will get 'Structure::List::<Int32>')
            var nameASTNode = node.Child(0);

            // Get the name's string, if the path has only one level.
            string onlyName = null;
            if (nameASTNode.Child(0).ChildNumber() == 1)
                onlyName = nameASTNode.Child(0).Child(0).GetExcerpt();

            Type resolvedType = null;

            // Check if it is the Void type.
            if (onlyName == "Void")
            {
                resolvedType = new TypeVoid();
            }
            // Check if it is a placeholder type.
            else if (onlyName == "_")
            {
                resolvedType = new TypeUnconstrained();
            }
            else
            {
                // Find a matching TopDecl.
                var matchingTopDecl = ASTTopDeclFinder.FindStruct(session, nameASTNode);

                // Build a Type with the matching TopDecl's struct.
                resolvedType = new TypeStruct((DefStruct)matchingTopDecl.def);
            }

            // Wrap the type into as many pointer types as specified.
            for (int i = 0; i < indirectionLevels; i++)
                resolvedType = new TypePointer(resolvedType);

            return resolvedType;
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
