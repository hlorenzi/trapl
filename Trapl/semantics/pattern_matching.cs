using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Trapl.Semantics
{
    public class ASTPatternMatcher
    {
        public static bool Match(DeclPatternSubstitution subst, DeclPattern genericPattern, DeclPattern concretePattern)
        {
            var matcher = new ASTPatternMatcher(subst, genericPattern, concretePattern);
            Console.Out.WriteLine("Pattern matching start");
            Grammar.AST.PrintDebug(genericPattern.src, genericPattern.astNode, 4);
            Grammar.AST.PrintDebug(concretePattern.src, concretePattern.astNode, 4);
            var hadInnerGeneric = false;
            var result = matcher.Match(genericPattern.astNode, concretePattern.astNode, ref hadInnerGeneric);
            Console.Out.WriteLine("  " + (result ? "MATCHED!" : "FAILED"));
            foreach (var pair in subst.nameToASTNodeMap)
            {
                Console.Out.WriteLine("  == '" + pair.Key + "'");
                foreach (var match in pair.Value)
                {
                    Console.Out.WriteLine("  ==== MATCH");
                    Grammar.AST.PrintDebug(concretePattern.src, match.astNode, 4);
                }
            }
            Console.Out.WriteLine();
            return result;
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
            if (thisNode.kind == Grammar.ASTNodeKind.GenericPattern)
            {
                if (otherNode.kind != Grammar.ASTNodeKind.GenericPattern)
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


            else if (thisNode.kind == Grammar.ASTNodeKind.VariadicGenericPattern)
            {
                if (otherNode.kind != Grammar.ASTNodeKind.GenericPattern)
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
                    var genericName = thisNode.Child(genericChildIndex).GetExcerpt(genericPattern.src);
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

                    // If the generic name is the last sibling, there is no pattern after it,
                    // so clone everything after the matching prefix.
                    if (genericChildIndex == thisNode.ChildNumber() - 1)
                    {
                        var typeNodeWithPattern = otherNode.CloneWithoutChildren();
                        var excerpt = "";
                        for (int i = genericChildIndex; i < otherNode.ChildNumber(); i++)
                        {
                            typeNodeWithPattern.AddChild(otherNode.Child(i).CloneWithChildren());
                            excerpt += otherNode.Child(i).GetExcerpt(this.concretePattern.src);
                        }
                        typeNodeWithPattern.OverwriteExcerpt(excerpt);
                        this.subst.Add(genericName, concretePattern.src, typeNodeWithPattern);
                    }
                    else
                    {
                        // Else, clone the other node up to and including the concrete name.
                        var typeNodeWithoutPattern = otherNode.CloneWithoutChildren();
                        var excerpt = "";
                        for (int i = genericChildIndex; i <= concreteChildIndex; i++)
                        {
                            typeNodeWithoutPattern.AddChild(otherNode.Child(i).CloneWithChildren());
                            excerpt += otherNode.Child(i).GetExcerpt(this.concretePattern.src);
                        }
                        typeNodeWithoutPattern.OverwriteExcerpt(excerpt);
                        this.subst.Add(genericName, concretePattern.src, typeNodeWithoutPattern);

                        // Then check if the pattern in both nodes match.
                        if (thisNode.ChildNumber() - genericChildIndex != otherNode.ChildNumber() - concreteChildIndex)
                            return false;

                        for (int i = 1; i < thisNode.ChildNumber() - genericChildIndex; i++)
                        {
                            if (!this.Match(thisNode.Child(i + genericChildIndex), otherNode.Child(i + concreteChildIndex), ref hadInnerGeneric))
                                return false;
                        }
                    }

                    innerGeneric = true;
                    return true;
                }


                else if (thisNode.kind == otherNode.kind &&
                    thisNode.GetExcerpt(this.genericPattern.src) == otherNode.GetExcerpt(this.concretePattern.src))
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

            return false;
        }
    }
}
