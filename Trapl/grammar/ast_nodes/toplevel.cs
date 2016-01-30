﻿using System.Collections.Generic;


namespace Trapl.Grammar
{
    public class ASTNodeTopLevel : ASTNode
    {
        public List<ASTNodeUse> useDirectives = new List<ASTNodeUse>();
        public List<ASTNode> decls = new List<ASTNode>();


        public void AddUseNode(ASTNodeUse use)
        {
            use.SetParent(this);
            this.AddSpan(use.GetSpan());
            this.useDirectives.Add(use);
        }


        public void AddDeclNode(ASTNode decl)
        {
            decl.SetParent(this);
            this.AddSpan(decl.GetSpan());
            this.decls.Add(decl);
        }


        public override IEnumerable<ASTNode> EnumerateChildren()
        {
            foreach (var use in this.useDirectives)
                yield return use;

            foreach (var decl in this.decls)
                yield return decl;
        }
    }
}