using System.Collections.Generic;
using Trapl.Diagnostics;
using System;


namespace Trapl.Semantics
{
    public class TypeResolution
    {
        public static Type Resolve(Interface.Session session, DeclPatternSubstitution subst, Interface.SourceCode src, Grammar.ASTNode node)
        {
            if (node.kind != Grammar.ASTNodeKind.TypeName)
                throw new InternalException("node is not a TypeName");

            // Check if the type has an indirection. (ex.: '&Int32')
            if (node.ChildIs(0, Grammar.ASTNodeKind.Operator))
            {
                // Wrap the inner Type into a pointer type.
                return new TypePointer(TypeResolution.Resolve(session, subst, src, node.Child(1)));
            }

            // Check that the next node is a name.
            if (node.Child(0).kind != Grammar.ASTNodeKind.Identifier)
                throw new InternalException("missing name node");

            // Read the name. (ex.: '&&Data::List::<Int32>' will read 'Data::List')
            var nameASTNode = node.Child(0);
            var name = nameASTNode.GetExcerpt(src);

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

            if (node.ChildIs(1, Grammar.ASTNodeKind.GenericPattern) ||
                node.ChildIs(1, Grammar.ASTNodeKind.VariadicGenericPattern))
            {
                genPatternASTNode = node.Child(1);
                genPattern.SetPattern(genPatternASTNode);
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
            else if (candidateTopDecls.Count > 1)
            {
                session.diagn.Add(MessageKind.Error, MessageCode.IncompatibleTemplate,
                    "pattern matches more than one declaration",
                    MessageCaret.Primary(src, nameASTNode.Span()),
                    MessageCaret.Primary(src, genPatternASTNode.Span()));
                throw new Semantics.CheckException();
            }

            // Ask the matching TopDecl to parse and resolve its definition, if not yet done.
            var matchingTopDecl = candidateTopDecls[0];
            var patternSubst = matchingTopDecl.pattern.GetSubstitution(genPattern);
            if (matchingTopDecl.generic)
            {
                matchingTopDecl = matchingTopDecl.CloneAndSubstitute(session, patternSubst);
                session.topDecls.Add(matchingTopDecl);
                matchingTopDecl.defASTNode.PrintDebugRecursive(matchingTopDecl.source, 1);
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

            // Build a Type with the matching TopDecl's struct.
            return new TypeStruct((DefStruct)matchingTopDecl.def);
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
