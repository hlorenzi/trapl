using System.Collections.Generic;


namespace Trapl.Grammar
{
    public class ASTNodeDeclGroup : ASTNode
    {
        public List<ASTNodeUse> useDirectives = new List<ASTNodeUse>();
        public List<ASTNodeDeclNamespace> namespaceDecls = new List<ASTNodeDeclNamespace>();
        public List<ASTNodeDeclStruct> structDecls = new List<ASTNodeDeclStruct>();
        public List<ASTNodeDeclFunct> functDecls = new List<ASTNodeDeclFunct>();


        public void AddUseNode(ASTNodeUse use)
        {
            use.SetParent(this);
            this.AddSpan(use.GetSpan());
            this.useDirectives.Add(use);
        }


        public void AddNamespaceDeclNode(ASTNodeDeclNamespace decl)
        {
            decl.SetParent(this);
            this.AddSpan(decl.GetSpan());
            this.namespaceDecls.Add(decl);
        }


        public void AddStructDeclNode(ASTNodeDeclStruct decl)
        {
            decl.SetParent(this);
            this.AddSpan(decl.GetSpan());
            this.structDecls.Add(decl);
        }


        public void AddFunctDeclNode(ASTNodeDeclFunct decl)
        {
            decl.SetParent(this);
            this.AddSpan(decl.GetSpan());
            this.functDecls.Add(decl);
        }


        public override IEnumerable<ASTNode> EnumerateChildren()
        {
            foreach (var use in this.useDirectives)
                yield return use;

            foreach (var decl in this.structDecls)
                yield return decl;

            foreach (var decl in this.functDecls)
                yield return decl;
        }
    }
}
