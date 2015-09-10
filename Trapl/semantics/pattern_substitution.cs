using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Trapl.Semantics
{
    public class ASTPatternSubstitution
    {
        public static Grammar.ASTNode CloneAndSubstitute(Interface.SourceCode src, Grammar.ASTNode node, DeclPatternSubstitution subst)
        {
            return CloneAndSubstituteRecursive(src, node, subst);
        }


        private static Grammar.ASTNode CloneAndSubstituteRecursive(Interface.SourceCode src, Grammar.ASTNode node, DeclPatternSubstitution subst)
        {
            var result = node.CloneWithoutChildren();

            foreach (var child in node.EnumerateChildren())
            {
                // Find whether this node's excerpt matches a generic name.
                var ident = child.GetExcerpt(src);
                if (subst.nameToASTNodeMap.ContainsKey(ident))
                {
                    // Check if the node's kind corresponds with the substitute's kind.
                    var substNode = subst.nameToASTNodeMap[ident][0];
                    if (substNode.astNode.kind == child.kind)
                    {
                        // Then clone children from substitute node!
                        result.AddChild(CloneSubstituteRecursive(src, child, substNode.astNode));
                        continue;
                    }
                }

                // Or else, just clone the children as they are.
                var newNode = CloneAndSubstituteRecursive(src, child, subst);
                result.AddChild(newNode);
            }

            return result;
        }


        private static Grammar.ASTNode CloneSubstituteRecursive(Interface.SourceCode src, Grammar.ASTNode node, Grammar.ASTNode substituteNode)
        {
            var result = node.CloneWithoutChildren();
            result.OverwriteExcerpt(substituteNode.GetExcerpt(src));

            var curIndex = 0;
            while (curIndex < node.ChildNumber())
            {
                var child = node.Child(curIndex);

                // Check whether the original node's kind matches the substitution's kind.
                if (!substituteNode.ChildIs(curIndex, child.kind))
                    break;

                var newNode = CloneSubstituteRecursive(src, child, substituteNode.Child(curIndex));
                result.AddChild(newNode);

                curIndex++;
            }

            while (curIndex < substituteNode.ChildNumber())
            {
                var newNode = CloneSubstituteRecursive(src, substituteNode.Child(curIndex), substituteNode.Child(curIndex));
                result.AddChild(newNode);

                curIndex++;
            }

            return result;
        }
    }
}
