using System;


namespace Trapl.Semantics
{
    public static class ASTPatternMatcher
    {
        public static PatternReplacementCollection Match(Grammar.ASTNode genericPatternNode, Grammar.ASTNode concretePatternNode)
        {
            var repl = new PatternReplacementCollection();
            if (!Match(repl, genericPatternNode, concretePatternNode))
                return null;
            else
                return repl;
        }


        private static bool Match(PatternReplacementCollection subst, Grammar.ASTNode thisNode, Grammar.ASTNode otherNode)
        {
            if (thisNode.kind == Grammar.ASTNodeKind.ParameterPattern)
            {
                if (otherNode.kind != Grammar.ASTNodeKind.ParameterPattern)
                    return false;

                if (thisNode.ChildNumber() != otherNode.ChildNumber())
                    return false;

                for (int i = 0; i < thisNode.ChildNumber(); i++)
                {
                    if (!Match(subst, thisNode.Child(i), otherNode.Child(i)))
                        return false;
                }

                return true;
            }


            else if (thisNode.kind == Grammar.ASTNodeKind.VariadicParameterPattern)
            {
                if (otherNode.kind != Grammar.ASTNodeKind.ParameterPattern)
                    return false;

                if (otherNode.ChildNumber() < thisNode.ChildNumber() - 1)
                    return false;

                for (int i = 0; i < otherNode.ChildNumber(); i++)
                {
                    if (!Match(subst, thisNode.Child(Math.Min(i, thisNode.ChildNumber() - 1)), otherNode.Child(i)))
                        return false;
                }

                return true;
            }


            else
            {
                // Check if this node has a generic name, and that the other node has a concrete name.
                var genericChildIndex = thisNode.children.FindIndex(c => c.kind == Grammar.ASTNodeKind.GenericIdentifier);
                if (thisNode.kind == Grammar.ASTNodeKind.TypeName &&
                    otherNode.kind == Grammar.ASTNodeKind.TypeName &&
                    genericChildIndex >= 0)
                {
                    // Read the generic name, and find the concrete name index.
                    var genericName = thisNode.Child(genericChildIndex).GetExcerpt();
                    var concreteChildIndex = otherNode.children.FindIndex(c => c.kind == Grammar.ASTNodeKind.Identifier);

                    if (concreteChildIndex < genericChildIndex)
                        return false;

                    // Match everything that comes before any of the two names.
                    for (int i = 0; i < genericChildIndex && i < concreteChildIndex; i++)
                    {
                        if (!Match(subst, thisNode.Child(i), otherNode.Child(i)))
                            return false;
                    }

                    // Clone the other node up to and including the concrete name.
                    var matchedNode = otherNode.CloneWithoutChildren();
                    for (int i = genericChildIndex; i <= concreteChildIndex; i++)
                    {
                        matchedNode.AddChild(otherNode.Child(i).CloneWithChildren());
                    }

                    // Then check if the pattern in both nodes match.
                    if (thisNode.ChildNumber() - genericChildIndex != otherNode.ChildNumber() - concreteChildIndex)
                        return false;

                    // If the generic name's pattern is empty, match everything else.
                    if (thisNode.ChildIs(genericChildIndex + 1, Grammar.ASTNodeKind.ParameterPattern) &&
                        thisNode.Child(genericChildIndex + 1).ChildNumber() == 0)
                    {
                        for (int i = concreteChildIndex + 1; i < otherNode.ChildNumber(); i++)
                        {
                            matchedNode.AddChild(otherNode.Child(i).CloneWithChildren());
                        }
                        subst.Add(genericName, matchedNode);
                    }
                    else
                    {
                        for (int i = 1; i < thisNode.ChildNumber() - genericChildIndex; i++)
                        {
                            if (!Match(subst, thisNode.Child(i + genericChildIndex), otherNode.Child(i + concreteChildIndex)))
                                return false;
                        }
                        subst.Add(genericName, matchedNode);
                    }

                    return true;
                }


                else if (thisNode.kind == otherNode.kind &&
                    thisNode.GetExcerpt() == otherNode.GetExcerpt())
                {
                    if (thisNode.ChildNumber() != otherNode.ChildNumber())
                        return false;

                    for (int i = 0; i < thisNode.ChildNumber(); i++)
                    {
                        if (!Match(subst, thisNode.Child(i), otherNode.Child(i)))
                            return false;
                    }

                    return true;
                }

                return false;
            }
        }
    }
}
