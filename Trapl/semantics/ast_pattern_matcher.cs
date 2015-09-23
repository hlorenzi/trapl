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


        private static bool Match(PatternReplacementCollection repl, Grammar.ASTNode thisNode, Grammar.ASTNode otherNode)
        {
            if (thisNode.kind == Grammar.ASTNodeKind.ParameterPattern)
            {
                if (otherNode.kind != Grammar.ASTNodeKind.ParameterPattern)
                    return false;

                if (thisNode.ChildNumber() != otherNode.ChildNumber())
                    return false;

                for (int i = 0; i < thisNode.ChildNumber(); i++)
                {
                    if (!Match(repl, thisNode.Child(i), otherNode.Child(i)))
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
                    if (!Match(repl, thisNode.Child(Math.Min(i, thisNode.ChildNumber() - 1)), otherNode.Child(i)))
                        return false;
                }

                return true;
            }


            else
            {
                // Check if this node has a generic name, and that the other node has a concrete name.
                if (thisNode.kind == Grammar.ASTNodeKind.TypeName &&
                    otherNode.kind == Grammar.ASTNodeKind.TypeName &&
                    thisNode.ChildIs(0, Grammar.ASTNodeKind.GenericIdentifier) &&
                    otherNode.ChildIs(0, Grammar.ASTNodeKind.Name))
                {
                    // Read the generic name.
                    var genericName = thisNode.Child(0).GetExcerpt();

                    // Clone the other node with only the concrete name child.
                    var matchedNode = otherNode.CloneWithoutChildren();
                    matchedNode.AddChild(otherNode.Child(0).CloneWithChildren());

                    // If the generic name's pattern is empty, match any concrete pattern.
                    if (thisNode.ChildIs(1, Grammar.ASTNodeKind.ParameterPattern) &&
                        thisNode.Child(1).ChildNumber() == 0)
                    {
                        matchedNode.AddChild(otherNode.Child(1).CloneWithChildren());
                    }
                    // Else, match the pattern, and add an empty one to the replacement.
                    else
                    {
                        matchedNode.AddChild(new Grammar.ASTNode(Grammar.ASTNodeKind.ParameterPattern, otherNode.Span().JustAfter()));
                        if (!Match(repl, thisNode.Child(1), otherNode.Child(1)))
                            return false;
                    }

                    // Then, match as many modifiers as there are in the generic node...
                    var curModifier = 2;
                    while (curModifier < thisNode.ChildNumber())
                    {
                        if (curModifier >= otherNode.ChildNumber())
                            return false;

                        if (!Match(repl, thisNode.Child(curModifier), otherNode.Child(curModifier)))
                            return false;

                        curModifier++;
                    }
                    
                    // ...and add the rest of the modifiers to the replacement.
                    while (curModifier < otherNode.ChildNumber())
                    {
                        matchedNode.AddChild(otherNode.Child(curModifier).CloneWithChildren());
                        curModifier++;
                    }

                    repl.Add(genericName, matchedNode);
                    return true;
                }

                else if (thisNode.kind == Grammar.ASTNodeKind.Name &&
                    otherNode.kind == Grammar.ASTNodeKind.Name)
                {
                    if (thisNode.GetExcerpt() != otherNode.GetExcerpt())
                        return false;

                    if (thisNode.ChildNumber() != otherNode.ChildNumber())
                        return false;

                    for (int i = 0; i < thisNode.ChildNumber(); i++)
                    {
                        if (!Match(repl, thisNode.Child(i), otherNode.Child(i)))
                            return false;
                    }

                    return true;
                }


                else if (thisNode.kind == otherNode.kind)
                {
                    if (thisNode.ChildNumber() != otherNode.ChildNumber())
                        return false;

                    for (int i = 0; i < thisNode.ChildNumber(); i++)
                    {
                        if (!Match(repl, thisNode.Child(i), otherNode.Child(i)))
                            return false;
                    }

                    return true;
                }

                return false;
            }
        }
    }
}
