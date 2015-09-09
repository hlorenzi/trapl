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
                if (child.kind == Grammar.ASTNodeKind.TypeName)
                {
                    if (child.ChildIs(0, Grammar.ASTNodeKind.Identifier))
                    {
                        var ident = child.Child(0).GetExcerpt(src);
                        if (subst.nameToASTNodeMap.ContainsKey(ident))
                        {
                            if (subst.nameToASTNodeMap[ident].Count != 1)
                                throw new InternalException("unimplemented");

                            var substitutionNode = subst.nameToASTNodeMap[ident][0];
                            var substitutedNode = CloneAndSubstituteRecursive(src, substitutionNode, subst);

                            for (int i = 1; i < child.ChildNumber(); i++)
                            {
                                substitutedNode.AddChild(CloneAndSubstituteRecursive(src, child.Child(i), subst));
                            }

                            result.AddChild(substitutedNode);
                            continue;
                        }
                    }
                }

                result.AddChild(CloneAndSubstituteRecursive(src, child, subst));
            }

            return result;
        }
    }
}
