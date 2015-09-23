using System.Collections.Generic;
using Trapl.Diagnostics;
using System;


namespace Trapl.Semantics
{
    public class ASTTypeUtil
    {
        public static Type Resolve(Interface.Session session, PatternReplacementCollection repl, Grammar.ASTNode node, bool acceptVoid = true)
        {
            if (node.kind != Grammar.ASTNodeKind.TypeName)
                throw new InternalException("node is not a TypeName");

            // Read the indirection level of the type. (ex.: '&&Int32' has 2 levels)
            var indirectionLevels = 0;
            foreach (var child in node.EnumerateChildren())
            {
                if (child.kind == Grammar.ASTNodeKind.Operator)
                    indirectionLevels++;
            }

            // Read the name. (ex.: '&&List::<Int32>' will read 'List')
            var nameASTNode = node.Child(0);
            var name = nameASTNode.GetExcerpt();

            // Check if it is the Void type.
            if (name == "Void")
            {
                if (!acceptVoid)
                {
                    session.diagn.Add(MessageKind.Error, MessageCode.ExplicitVoid,
                        "cannot use 'Void' here", nameASTNode.GetOriginalSpan());
                    throw new Semantics.CheckException();
                }

                return new TypeVoid();
            }

            // Find a matching TopDecl.
            var matchingTopDecl = ASTTopDeclFinder.Find(session, nameASTNode, node.Child(1));

            // Check that what the matching TopDecl defines is a struct.
            if (!(matchingTopDecl.def is DefStruct))
            {
                session.diagn.Add(MessageKind.Error, MessageCode.UnknownType,
                    "'" + name + "' is not a struct", nameASTNode.GetOriginalSpan());
                throw new Semantics.CheckException();
            }

            // Build a Type with the matching TopDecl's struct.
            Type resolvedType = new TypeStruct((DefStruct)matchingTopDecl.def);

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
                else if (child.kind == Grammar.ASTNodeKind.GenericIdentifier)
                    result += "gen " + child.GetExcerpt();
                else if (child.kind == Grammar.ASTNodeKind.ParameterPattern)
                {
                    if (!ASTPatternUtil.IsEmpty(child))
                        result += "::" + ASTPatternUtil.GetString(child);
                }
                else if (child.kind == Grammar.ASTNodeKind.Operator)
                    prefix += child.GetExcerpt();
                else
                    result += "?";
            }

            return prefix + result;
        }
    }
}
