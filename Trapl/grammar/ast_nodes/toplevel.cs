using System.Collections.Generic;


namespace Trapl.Grammar
{
    public class ASTNodeTopLevel : ASTNode
    {
        public List<ASTNode> statements = new List<ASTNode>();


        public void AddStatementNode(ASTNode statement)
        {
            statement.SetParent(this);
            this.statements.Add(statement);
        }


        public override IEnumerable<ASTNode> EnumerateChildren()
        {
            foreach (var statement in this.statements)
                yield return statement;
        }
    }
}
