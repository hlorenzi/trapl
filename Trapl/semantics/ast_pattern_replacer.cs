using Trapl.Diagnostics;


namespace Trapl.Semantics
{
    public static class ASTPatternReplacer
    {
        public static Grammar.ASTNode CloneReplaced(Interface.Session session, Grammar.ASTNode node, PatternReplacementCollection repl)
        {
            return CloneReplacedRecursive(session, node, repl, false, new Diagnostics.Span());
        }


        private static Grammar.ASTNode CloneReplacedRecursive(Interface.Session session, Grammar.ASTNode node, PatternReplacementCollection repl, bool isSubstitute, Diagnostics.Span originalSpan)
        {
            var result = node.CloneWithoutChildren();
            if (isSubstitute)
                result.SetOriginalSpan(originalSpan);

            if (result.kind == Grammar.ASTNodeKind.TypeName)
            {
                // Check whether this node has a generic identifier.
                var genericIdentifierIndex = node.children.FindIndex(n => n.kind == Grammar.ASTNodeKind.GenericIdentifier);
                if (genericIdentifierIndex >= 0)
                {
                    // Check whether the generic identifier has a replacement.
                    var genericIdent = node.Child(genericIdentifierIndex).GetExcerpt();
                    if (repl.nameToASTNodeMap.ContainsKey(genericIdent))
                    {
                        // Check whether the replacement's kind is also a TypeName.
                        var substNode = repl.nameToASTNodeMap[genericIdent][0];
                        if (substNode.kind == Grammar.ASTNodeKind.TypeName)
                        {
                            // Then clone from the replacement node!
                            result = CloneReplacedRecursive(session, substNode, repl, true, node.Span());

                            // And insert at the start everything from before the generic name.
                            for (int i = 0; i < genericIdentifierIndex; i++)
                                result.children.Insert(0, CloneReplacedRecursive(session, node.Child(i), repl, true, node.Child(i).Span()));

                            var genericPatternIndex = node.children.FindIndex(n => n.kind == Grammar.ASTNodeKind.ParameterPattern);
                            var substPatternIndex = result.children.FindIndex(n => n.kind == Grammar.ASTNodeKind.ParameterPattern);

                            if (substPatternIndex >= 0 && result.Child(substPatternIndex).ChildNumber() != 0 &&
                                node.Child(genericPatternIndex).ChildNumber() != 0)
                            {
                                session.diagn.Add(MessageKind.Error, MessageCode.IncompatibleTemplate,
                                    "replaced type already has a pattern", node.Child(genericPatternIndex).Span());
                                throw new Semantics.CheckException();
                            }

                            if (substPatternIndex < 0)
                            {
                                result.children.Add(CloneReplacedRecursive(session, node.Child(genericPatternIndex), repl, true, node.Child(genericPatternIndex).Span()));
                            }
                            else if (result.Child(substPatternIndex).ChildNumber() == 0)
                            {
                                result.children.RemoveAt(substPatternIndex);
                                result.children.Insert(substPatternIndex, CloneReplacedRecursive(session, node.Child(genericPatternIndex), repl, true, node.Child(genericPatternIndex).Span()));
                            }

                            return result;
                        }
                    }
                    else
                    {
                        session.diagn.Add(MessageKind.Error, MessageCode.UnknownType,
                            "unresolved generic identifier", node.Child(0).Span());
                        throw new Semantics.CheckException();
                    }
                }
            }

            foreach (var child in node.EnumerateChildren())
            {
                result.AddChild(CloneReplacedRecursive(session, child, repl, isSubstitute, originalSpan));
            }

            return result;
        }
    }
}
