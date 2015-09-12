using System;


namespace Trapl.Semantics
{
    public class ASTPatternMatcher
    {
        public static bool Match(DeclPatternSubstitution subst, DeclPattern genericPattern, DeclPattern concretePattern)
        {
            var matcher = new ASTPatternMatcher(subst, genericPattern, concretePattern);

            var hadInnerGeneric = false;
            return matcher.Match(genericPattern.astNode, concretePattern.astNode, ref hadInnerGeneric);
        }


        private DeclPatternSubstitution subst;
        private DeclPattern genericPattern;
        private DeclPattern concretePattern;


        private ASTPatternMatcher(DeclPatternSubstitution subst, DeclPattern genericPattern, DeclPattern concretePattern)
        {
            this.subst = subst;
            this.genericPattern = genericPattern;
            this.concretePattern = concretePattern;
        }


        private bool Match(Grammar.ASTNode thisNode, Grammar.ASTNode otherNode, ref bool innerGeneric)
        {
            if (thisNode.kind == Grammar.ASTNodeKind.ParameterPattern)
            {
                if (otherNode.kind != Grammar.ASTNodeKind.ParameterPattern)
                    return false;

                if (thisNode.ChildNumber() != otherNode.ChildNumber())
                    return false;

                var hadInnerGeneric = false;
                for (int i = 0; i < thisNode.ChildNumber(); i++)
                {
                    if (!this.Match(thisNode.Child(i), otherNode.Child(i), ref hadInnerGeneric))
                        return false;
                }

                innerGeneric = hadInnerGeneric;
                return true;
            }


            else if (thisNode.kind == Grammar.ASTNodeKind.VariadicParameterPattern)
            {
                if (otherNode.kind != Grammar.ASTNodeKind.ParameterPattern)
                    return false;

                if (otherNode.ChildNumber() < thisNode.ChildNumber() - 1)
                    return false;

                var hadInnerGeneric = false;
                for (int i = 0; i < otherNode.ChildNumber(); i++)
                {
                    if (!this.Match(thisNode.Child(Math.Min(i, thisNode.ChildNumber() - 1)), otherNode.Child(i), ref hadInnerGeneric))
                        return false;
                }

                innerGeneric = hadInnerGeneric;
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
                    var hadInnerGeneric = false;
                    for (int i = 0; i < genericChildIndex && i < concreteChildIndex; i++)
                    {
                        if (!this.Match(thisNode.Child(i), otherNode.Child(i), ref hadInnerGeneric))
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
                        this.subst.Add(genericName, concretePattern.src, matchedNode);
                    }
                    else
                    {
                        for (int i = 1; i < thisNode.ChildNumber() - genericChildIndex; i++)
                        {
                            if (!this.Match(thisNode.Child(i + genericChildIndex), otherNode.Child(i + concreteChildIndex), ref hadInnerGeneric))
                                return false;
                        }
                        this.subst.Add(genericName, concretePattern.src, matchedNode);
                    }

                    innerGeneric = true;
                    return true;
                }


                else if (thisNode.kind == otherNode.kind &&
                    thisNode.GetExcerpt() == otherNode.GetExcerpt())
                {
                    if (thisNode.ChildNumber() != otherNode.ChildNumber())
                        return false;

                    var hadInnerGeneric = false;
                    for (int i = 0; i < thisNode.ChildNumber(); i++)
                    {
                        if (!this.Match(thisNode.Child(i), otherNode.Child(i), ref hadInnerGeneric))
                            return false;
                    }

                    innerGeneric = hadInnerGeneric;
                    return true;
                }

                return false;
            }
        }
    }
}
