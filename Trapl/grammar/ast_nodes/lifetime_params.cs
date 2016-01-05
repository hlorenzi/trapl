using System.Collections.Generic;


namespace Trapl.Grammar
{ 
    public class ASTNodeLifetimeParams : ASTNode
    {
        public List<ASTNodeLifetime> lifetimes = new List<ASTNodeLifetime>();


        public override IEnumerable<ASTNode> EnumerateChildren()
        {
            foreach (var lifetime in this.lifetimes)
                yield return lifetime;
        }
    }
}
