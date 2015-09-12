using Trapl.Diagnostics;


namespace Trapl.Semantics
{
    public class ASTPatternSubstitution
    {
        public static Grammar.ASTNode CloneAndSubstitute(Interface.Session session, Grammar.ASTNode node, DeclPatternSubstitution subst)
        {
            return Clone(session, node, subst);
        }


        private static Grammar.ASTNode Clone(Interface.Session session, Grammar.ASTNode node, DeclPatternSubstitution subst)
        {
            var result = node.CloneWithoutChildren();
            if (result.kind == Grammar.ASTNodeKind.TypeName)
            {
                // Check whether this node has a generic identifier.
                var genericIdentifierIndex = node.children.FindIndex(n => n.kind == Grammar.ASTNodeKind.GenericIdentifier);
                if (genericIdentifierIndex >= 0)
                {
                    // Check whether the generic identifier has a substitute.
                    var genericIdent = node.Child(genericIdentifierIndex).GetExcerpt();
                    if (subst.nameToASTNodeMap.ContainsKey(genericIdent))
                    {
                        // Check whether the substitute's kind is also a TypeName.
                        var substNode = subst.nameToASTNodeMap[genericIdent][0];
                        if (substNode.astNode.kind == Grammar.ASTNodeKind.TypeName)
                        {
                            // Then clone from substitute node!
                            result = Clone(session, substNode.astNode, subst);
                            var genericPatternIndex = node.children.FindIndex(n => n.kind == Grammar.ASTNodeKind.ParameterPattern);
                            var substPatternIndex = result.children.FindIndex(n => n.kind == Grammar.ASTNodeKind.ParameterPattern);

                            if (substPatternIndex >= 0 && result.Child(substPatternIndex).ChildNumber() != 0 &&
                                node.Child(genericPatternIndex).ChildNumber() != 0)
                            {
                                session.diagn.Add(MessageKind.Error, MessageCode.IncompatibleTemplate,
                                    "substituted type already has a pattern", node.Child(genericPatternIndex).Span());
                                throw new Semantics.CheckException();
                            }

                            if (substPatternIndex < 0)
                            {
                                result.children.Add(Clone(session, node.Child(genericPatternIndex), subst));
                            }
                            else if (result.Child(substPatternIndex).ChildNumber() == 0)
                            {
                                result.children.RemoveAt(substPatternIndex);
                                result.children.Insert(substPatternIndex, Clone(session, node.Child(genericPatternIndex), subst));
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
                result.AddChild(Clone(session, child, subst));
            }

            return result;
        }
    }
}
