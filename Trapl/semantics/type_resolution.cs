using System.Collections.Generic;
using Trapl.Diagnostics;
using System;


namespace Trapl.Semantics
{
    public class TypeResolution
    {
        public static Type Resolve(Interface.Session session, DeclPatternSubstitution subst, Interface.SourceCode src, Grammar.ASTNode node)
        {
            Interface.Debug.BeginSection("TYPE RESOLVE '" + node.GetExcerpt(src) + "'");
            Interface.Debug.PrintAST(src, node);

            if (node.kind != Grammar.ASTNodeKind.TypeName)
                throw new InternalException("node is not a TypeName");

            // Index to iterate through the node's children.
            var curChild = 0;

            // Read the indirection level of the type. (ex.: '&&Int32' has 2 levels)
            var indirectionLevels = 0;
            while (node.ChildIs(curChild, Grammar.ASTNodeKind.Operator))
            {
                indirectionLevels++;
                curChild++;
            }

            // Check that the next node is a name.
            if (node.Child(curChild).kind != Grammar.ASTNodeKind.Identifier)
                throw new InternalException("missing Identifier node");

            // Read the name. (ex.: '&&List::<Int32>' will read 'List')
            var nameASTNode = node.Child(curChild);
            var name = nameASTNode.GetExcerpt(src);
            curChild++;

            // Find the TopDecls that match the name.
            var candidateTopDecls = session.topDecls.FindAll(decl => decl.qualifiedName == name);
            if (candidateTopDecls.Count == 0)
            {
                session.diagn.Add(MessageKind.Error, MessageCode.UnknownType,
                    "unknown type", src, nameASTNode.Span());
                throw new Semantics.CheckException();
            }

            // Read the generic pattern. (ex.: 'List::<Int32>' will read '<Int32>')
            var genPatternASTNode = new Grammar.ASTNode(Grammar.ASTNodeKind.GenericPattern, nameASTNode.Span());
            var genPattern = new DeclPattern(src, genPatternASTNode);

            if (node.ChildIs(curChild, Grammar.ASTNodeKind.GenericPattern) ||
                node.ChildIs(curChild, Grammar.ASTNodeKind.VariadicGenericPattern))
            {
                genPatternASTNode = node.Child(curChild);
                genPattern.SetPattern(genPatternASTNode);
                curChild++;
            }

            // Refine candidate TopDecls further by compatibility with the generic pattern.
            candidateTopDecls = candidateTopDecls.FindAll(decl => (decl.pattern.GetSubstitution(genPattern) != null));
            if (candidateTopDecls.Count == 0)
            {
                session.diagn.Add(MessageKind.Error, MessageCode.IncompatibleTemplate,
                    "pattern does not match any declarations",
                    MessageCaret.Primary(src, nameASTNode.Span()),
                    MessageCaret.Primary(src, genPatternASTNode.Span()));
                throw new Semantics.CheckException();
            }
            /*else if (candidateTopDecls.Count > 1)
            {
                session.diagn.Add(MessageKind.Error, MessageCode.IncompatibleTemplate,
                    "pattern matches more than one declaration",
                    MessageCaret.Primary(src, nameASTNode.Span()),
                    MessageCaret.Primary(src, genPatternASTNode.Span()));
                throw new Semantics.CheckException();
            }*/

            // Ask the matching TopDecl to parse and resolve its definition, if not yet done.
            var matchingTopDecl = candidateTopDecls[0];
            var patternSubst = matchingTopDecl.pattern.GetSubstitution(genPattern);
            if (matchingTopDecl.generic)
            {
                Interface.Debug.BeginSection("PERFORM SUBSTITUTION");
                patternSubst.PrintDebug();
                matchingTopDecl = matchingTopDecl.CloneAndSubstitute(session, patternSubst);
                session.topDecls.Add(matchingTopDecl);
                Interface.Debug.EndSection();
            }

            session.diagn.EnterSubstitutionContext(patternSubst);
            matchingTopDecl.Resolve(session);
            session.diagn.ExitSubstitutionContext();

            // Check that what the matching TopDecl defines is a struct.
            if (!(matchingTopDecl.def is DefStruct))
            {
                session.diagn.Add(MessageKind.Error, MessageCode.UnknownType,
                    "name does not define a struct", src, nameASTNode.Span());
                throw new Semantics.CheckException();
            }

            Interface.Debug.EndSection();

            // Build a Type with the matching TopDecl's struct.
            var resolvedType = (Type)new TypeStruct((DefStruct)matchingTopDecl.def);

            // Wrap the type into as many pointer types as needed.
            for (int i = 0; i < indirectionLevels; i++)
                resolvedType = new TypePointer(resolvedType);

            return resolvedType;
        }


        public static string GetName(Interface.Session session, Type type)
        {
            if (type is TypePointer)
            {
                return "&" + GetName(session, ((TypePointer)type).pointeeType);
            }
            else if (type is TypeStruct)
            {
                foreach (var topDecl in session.topDecls)
                {
                    if (((TypeStruct)type).structDef == topDecl.def)
                    {
                        return topDecl.qualifiedName + "::" +
                            topDecl.pattern.GetString(session) + " " +
                            topDecl.patternSubst.GetString();
                    }
                }
            }

            return "<unknown>";
        }
    }
}
