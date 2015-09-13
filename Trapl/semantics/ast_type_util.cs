using System.Collections.Generic;
using Trapl.Diagnostics;
using System;


namespace Trapl.Semantics
{
    public class ASTTypeUtil
    {
        public static Type Resolve(Interface.Session session, PatternReplacementCollection repl, Interface.SourceCode src, Grammar.ASTNode node)
        {
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
            var name = nameASTNode.GetExcerpt();
            curChild++;

            // Find the TopDecls that match the name.
            var candidateTopDecls = session.topDecls.FindAll(decl => decl.qualifiedName == name);
            if (candidateTopDecls.Count == 0)
            {
                session.diagn.Add(MessageKind.Error, MessageCode.UnknownType,
                    "unknown type '" + name + "'", nameASTNode.GetOriginalSpan());
                throw new Semantics.CheckException();
            }

            // Read the pattern. (ex.: 'List::<Int32>' will read '<Int32>')
            var patternASTNode = new Grammar.ASTNode(Grammar.ASTNodeKind.ParameterPattern, nameASTNode.Span());

            if (node.ChildIs(curChild, Grammar.ASTNodeKind.ParameterPattern) ||
                node.ChildIs(curChild, Grammar.ASTNodeKind.VariadicParameterPattern))
            {
                patternASTNode = node.Child(curChild);
                curChild++;
            }

            // Refine candidate TopDecls further by compatibility with the pattern.
            candidateTopDecls = candidateTopDecls.FindAll(decl => (ASTPatternMatcher.Match(decl.patternASTNode, patternASTNode) != null));

            // Sort candidates by increasing number of generic identifiers,
            // so that more concrete TopDecls appear first.
            candidateTopDecls.Sort((a, b) => ASTPatternUtil.GetGenericParameterNumber(a.patternASTNode) - ASTPatternUtil.GetGenericParameterNumber(b.patternASTNode));

            // Check that at least one TopDecl matched.
            if (candidateTopDecls.Count == 0)
            {
                session.diagn.Add(MessageKind.Error, MessageCode.IncompatibleTemplate,
                    "no '" + name + "' declaration accepts the pattern '" + ASTPatternUtil.GetString(patternASTNode) + "'",
                    MessageCaret.Primary(nameASTNode.GetOriginalSpan()),
                    MessageCaret.Primary(patternASTNode.GetOriginalSpan()));
                throw new Semantics.CheckException();
            }

            // Check that there is no ambiguity for the best matched TopDecl.
            if (candidateTopDecls.Count > 1 &&
                ASTPatternUtil.GetGenericParameterNumber(candidateTopDecls[0].patternASTNode) == ASTPatternUtil.GetGenericParameterNumber(candidateTopDecls[1].patternASTNode))
            {
                session.diagn.Add(MessageKind.Error, MessageCode.IncompatibleTemplate,
                    "more than one '" + name + "' declaration accepts this pattern '" + ASTPatternUtil.GetString(patternASTNode) + "'",
                    MessageCaret.Primary(nameASTNode.GetOriginalSpan()),
                    MessageCaret.Primary(patternASTNode.GetOriginalSpan()));
                throw new Semantics.CheckException();
            }

            // Ask the matching TopDecl to parse and resolve its definition, if not yet done.
            var matchingTopDecl = candidateTopDecls[0];
            var innerRepl = ASTPatternMatcher.Match(matchingTopDecl.patternASTNode, patternASTNode);
            if (matchingTopDecl.generic)
            {
                matchingTopDecl = matchingTopDecl.CloneAndSubstitute(session, innerRepl);
                session.topDecls.Add(matchingTopDecl);
            }

            session.diagn.EnterSubstitutionContext(innerRepl);
            matchingTopDecl.Resolve(session);
            session.diagn.ExitSubstitutionContext();

            // Check that what the matching TopDecl defines is a struct.
            if (!(matchingTopDecl.def is DefStruct))
            {
                session.diagn.Add(MessageKind.Error, MessageCode.UnknownType,
                    "'" + name + "' is not a struct", nameASTNode.GetOriginalSpan());
                throw new Semantics.CheckException();
            }

            // Build a Type with the matching TopDecl's struct.
            var resolvedType = (Type)new TypeStruct((DefStruct)matchingTopDecl.def);

            // Wrap the type into as many pointer types as needed.
            for (int i = 0; i < indirectionLevels; i++)
                resolvedType = new TypePointer(resolvedType);

            return resolvedType;
        }


        public static string GetString(Grammar.ASTNode typeNode)
        {
            var result = "";

            foreach (var child in typeNode.EnumerateChildren())
            {
                if (child.kind == Grammar.ASTNodeKind.Operator ||
                    child.kind == Grammar.ASTNodeKind.Identifier)
                    result += child.GetExcerpt();
                else if (child.kind == Grammar.ASTNodeKind.GenericIdentifier)
                    result += "gen " + child.GetExcerpt();
                else if (child.kind == Grammar.ASTNodeKind.ParameterPattern)
                {
                    if (!ASTPatternUtil.IsEmpty(child))
                        result += "::" + ASTPatternUtil.GetString(child);
                }
                else
                    result += "?";
            }

            return result;
        }
    }
}
