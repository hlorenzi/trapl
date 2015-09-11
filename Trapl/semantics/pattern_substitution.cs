using Trapl.Diagnostics;


namespace Trapl.Semantics
{
    public class ASTPatternSubstitution
    {
        public static Grammar.ASTNode CloneAndSubstitute(Interface.Session session, Interface.SourceCode src, Grammar.ASTNode node, DeclPatternSubstitution subst)
        {
            return Clone(session, src, node, subst);
        }


        private static Grammar.ASTNode Clone(Interface.Session session, Interface.SourceCode src, Grammar.ASTNode node, DeclPatternSubstitution subst)
        {
            var result = node.CloneWithoutChildren();
            if (result.kind == Grammar.ASTNodeKind.TypeName)
            {
                // Check whether this node's generic identifier has a substitute.
                if (node.ChildIs(0, Grammar.ASTNodeKind.GenericIdentifier))
                {
                    var genericIdent = node.Child(0).GetExcerpt(src);
                    if (subst.nameToASTNodeMap.ContainsKey(genericIdent))
                    {
                        // Check if the node's kind corresponds with the substitute's kind.
                        var substNode = subst.nameToASTNodeMap[genericIdent][0];
                        if (substNode.astNode.kind == Grammar.ASTNodeKind.TypeName)
                        {
                            // Then clone from substitute node!
                            result = CloneWithExcerpt(session, src, substNode.astNode, subst, node.Span());
                            result.SetSpan(node.Span());

                            for (int i = 1; i < node.ChildNumber(); i++)
                            {
                                result.AddChild(CloneWithExcerpt(session, src, node.Child(i), subst, node.Span()));
                            }

                            return result;
                        }
                    }
                    else
                    {
                        session.diagn.Add(MessageKind.Error, MessageCode.UnknownType,
                            "unresolved generic identifier", src, node.Child(0).Span());
                        throw new Semantics.CheckException();
                    }
                }
            }

            foreach (var child in node.EnumerateChildren())
            {
                result.AddChild(Clone(session, src, child, subst));
            }

            return result;
        }


        private static Grammar.ASTNode CloneWithExcerpt(Interface.Session session, Interface.SourceCode src, Grammar.ASTNode node, DeclPatternSubstitution subst, Diagnostics.Span substSpan)
        {
            var result = node.CloneWithoutChildren();
            result.OverwriteExcerpt(node.GetExcerpt(src));
            result.SetSpan(substSpan);

            if (result.kind == Grammar.ASTNodeKind.TypeName)
            {
                // Check whether this node's generic identifier has a substitute.
                if (node.ChildIs(0, Grammar.ASTNodeKind.GenericIdentifier))
                {
                    var genericIdent = node.Child(0).GetExcerpt(src);
                    if (subst.nameToASTNodeMap.ContainsKey(genericIdent))
                    {
                        // Check if the node's kind corresponds with the substitute's kind.
                        var substNode = subst.nameToASTNodeMap[genericIdent][0];
                        if (substNode.astNode.kind == Grammar.ASTNodeKind.TypeName)
                        {
                            // Then clone from substitute node!
                            result = CloneWithExcerpt(session, src, substNode.astNode, subst, substSpan);
                            result.SetSpan(node.Span());

                            for (int i = 1; i < node.ChildNumber(); i++)
                            {
                                result.AddChild(CloneWithExcerpt(session, src, node.Child(i), subst, substSpan));
                            }

                            return result;
                        }
                    }
                    else
                    {
                        session.diagn.Add(MessageKind.Error, MessageCode.UnknownType,
                            "unresolved generic identifier", src, node.Child(0).Span());
                        throw new Semantics.CheckException();
                    }
                }
            }

            foreach (var child in node.EnumerateChildren())
            {
                result.AddChild(CloneWithExcerpt(session, src, child, subst, substSpan));
            }

            return result;
        }
    }
}
