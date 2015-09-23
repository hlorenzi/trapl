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
                if (node.ChildIs(0, Grammar.ASTNodeKind.GenericIdentifier))
                {
                    // Check whether the generic identifier has a replacement.
                    var genericIdent = node.Child(0).GetExcerpt();
                    if (repl.nameToASTNodeMap.ContainsKey(genericIdent))
                    {
                        // Check whether the replacement's kind is also a TypeName.
                        var substNode = repl.nameToASTNodeMap[genericIdent][0];
                        if (substNode.kind == Grammar.ASTNodeKind.TypeName)
                        {
                            // Then clone from the replacement node!
                            result = CloneReplacedRecursive(session, substNode, repl, true, node.Span());

                            // And add any modifiers from the generic type.
                            for (int i = 2; i < node.ChildNumber(); i++)
                                result.children.Add(CloneReplacedRecursive(session, node.Child(i), repl, true, node.Child(i).Span()));

                            // Check if there's no conflict of patterns.
                            if (result.Child(1).ChildNumber() != 0 &&
                                node.Child(1).ChildNumber() != 0)
                            {
                                session.diagn.Add(MessageKind.Error, MessageCode.IncompatibleTemplate,
                                    "'gen " + genericIdent + "' replacement already has a pattern", node.Span());
                                throw new Semantics.CheckException();
                            }
                            
                            // And add the pattern from the generic type if possible!
                            if (result.Child(1).ChildNumber() == 0)
                            {
                                result.children.RemoveAt(1);
                                result.children.Insert(1, CloneReplacedRecursive(session, node.Child(1), repl, true, node.Child(1).Span()));
                            }

                            return result;
                        }
                    }
                    else
                    {
                        session.diagn.Add(MessageKind.Error, MessageCode.UnknownType,
                            "'gen " + genericIdent + "' has no replacement", node.Child(0).Span());
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
