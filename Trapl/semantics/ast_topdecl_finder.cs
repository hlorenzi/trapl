using Trapl.Diagnostics;


namespace Trapl.Semantics
{
    public static class ASTTopDeclFinder
    {
        public static TopDecl Find(Interface.Session session, Grammar.ASTNode nameASTNode, Grammar.ASTNode patternASTNode)
        {
            // Read the name.
            var name = nameASTNode.GetExcerpt();

            // Find the TopDecls that match the name.
            var candidateTopDecls = session.topDecls.FindAll(decl => decl.qualifiedName == name);
            if (candidateTopDecls.Count == 0)
            {
                session.diagn.Add(MessageKind.Error, MessageCode.UnknownType,
                    "'" + name + "' is not declared", nameASTNode.GetOriginalSpan());
                throw new Semantics.CheckException();
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
                    nameASTNode.GetOriginalSpan(), patternASTNode.GetOriginalSpan());
                throw new Semantics.CheckException();
            }

            // Check that there is no ambiguity for the best matched TopDecl.
            if (candidateTopDecls.Count > 1 &&
                ASTPatternUtil.GetGenericParameterNumber(candidateTopDecls[0].patternASTNode) == ASTPatternUtil.GetGenericParameterNumber(candidateTopDecls[1].patternASTNode))
            {
                session.diagn.Add(MessageKind.Error, MessageCode.IncompatibleTemplate,
                    "more than one '" + name + "' declaration accepts this pattern '" + ASTPatternUtil.GetString(patternASTNode) + "'",
                    nameASTNode.GetOriginalSpan(), patternASTNode.GetOriginalSpan());
                throw new Semantics.CheckException();
            }

            // Ask the matching TopDecl to parse and resolve its definition, if not yet done.
            var matchingTopDecl = candidateTopDecls[0];
            var innerRepl = ASTPatternMatcher.Match(matchingTopDecl.patternASTNode, patternASTNode);
            if (matchingTopDecl.generic)
            {
                var synthTopDecl = matchingTopDecl.Clone();

                synthTopDecl.patternRepl = innerRepl;
                synthTopDecl.patternASTNode = ASTPatternReplacer.CloneReplaced(session, matchingTopDecl.patternASTNode, innerRepl);
                synthTopDecl.defASTNode = ASTPatternReplacer.CloneReplaced(session, matchingTopDecl.defASTNode, innerRepl);

                session.topDecls.Add(synthTopDecl);
                matchingTopDecl = synthTopDecl;
            }

            matchingTopDecl.Resolve(session);
            return matchingTopDecl;
        }
    }
}
