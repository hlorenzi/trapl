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
            //Console.Out.WriteLine("Pattern matching start");
            var result = matcher.Match(genericPattern.astNode, concretePattern.astNode);
            /*Console.Out.WriteLine("  " + (result ? "MATCHED!" : "FAILED"));
            foreach (var pair in subst.nameToASTNodeMap)
            {
                Console.Out.WriteLine("  == '" + pair.Key + "'");
                foreach (var match in pair.Value)
                {
                    Console.Out.WriteLine("  ==== MATCH");
                    Grammar.AST.PrintDebug(concretePattern.src, match, 4);
                }
            }
            Console.Out.WriteLine();*/
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


        private bool Match(Grammar.ASTNode thisNode, Grammar.ASTNode otherNode)
        {
            if (thisNode.kind == Grammar.ASTNodeKind.GenericPattern)
            {
                if (otherNode.kind != Grammar.ASTNodeKind.GenericPattern)
                    return false;

                if (thisNode.ChildNumber() != otherNode.ChildNumber())
                    return false;

                for (int i = 0; i < thisNode.ChildNumber(); i++)
                {
                    if (!this.Match(thisNode.Child(i), otherNode.Child(i)))
                        return false;
                }

                return true;
            }


            else if (thisNode.kind == Grammar.ASTNodeKind.VariadicGenericPattern)
            {
                if (otherNode.kind != Grammar.ASTNodeKind.GenericPattern)
                    return false;

                if (otherNode.ChildNumber() < thisNode.ChildNumber() - 1)
                    return false;

                for (int i = 0; i < otherNode.ChildNumber(); i++)
                {
                    if (!this.Match(thisNode.Child(Math.Min(i, thisNode.ChildNumber() - 1)), otherNode.Child(i)))
                        return false;
                }

                return true;
            }


            else if (thisNode.kind == Grammar.ASTNodeKind.TypeName)
            {
                // Find the index of a GenericType child, if there is one.
                var genericTypeChildIndex =
                    thisNode.children.FindIndex(n => n.kind == Grammar.ASTNodeKind.GenericType);

                // If there is no generic type, match exactly.
                if (genericTypeChildIndex == -1)
                {
                    if (thisNode.ChildNumber() != otherNode.ChildNumber())
                        return false;

                    for (int i = 0; i < thisNode.ChildNumber(); i++)
                    {
                        if (!this.Match(thisNode.Child(i), otherNode.Child(i)))
                            return false;
                    }

                    return true;
                }

                // If there is a generic type, match first to the left of the generic type.
                for (int i = 0; i < genericTypeChildIndex; i++)
                {
                    if (!this.Match(thisNode.Child(i), otherNode.Child(i)))
                        return false;
                }

                var genericName = genericPattern.src.GetExcerpt(thisNode.Child(genericTypeChildIndex).Span());

                // If there is nothing to the right, set all the rest as the substitution.
                if (genericTypeChildIndex + 1 >= thisNode.ChildNumber())
                {
                    var node = new Grammar.ASTNode(otherNode.kind);
                    for (int i = genericTypeChildIndex; i < otherNode.ChildNumber(); i++)
                    {
                        node.AddChild(otherNode.Child(i));
                        if (i == genericTypeChildIndex)
                            node.SetLastChildSpan();
                        else
                            node.AddLastChildSpan();
                    }
                    this.subst.Add(genericName, node);
                    return true;
                }

                if (thisNode.ChildNumber() != otherNode.ChildNumber())
                    return false;

                this.subst.Add(genericName, otherNode.Child(genericTypeChildIndex));

                // Or else, match up to the inner pattern, and match recursively into it.
                for (int i = genericTypeChildIndex + 1; i < thisNode.ChildNumber(); i++)
                {
                    if (!this.Match(thisNode.Child(i), otherNode.Child(i)))
                        return false;
                }

                return true;
            }


            else if (thisNode.kind == otherNode.kind)
                return true;

            return false;
        }
    }
}
