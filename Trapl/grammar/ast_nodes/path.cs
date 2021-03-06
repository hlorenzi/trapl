﻿using System.Collections.Generic;


namespace Trapl.Grammar
{
    public class ASTNodePath : ASTNode
    {
        public bool isRooted = false;
        public List<ASTNodeIdentifier> identifiers = new List<ASTNodeIdentifier>();


        public void SetRooted(bool isRooted)
        {
            this.isRooted = isRooted;
        }


        public void AddIdentifierNode(ASTNodeIdentifier identifier)
        {
            identifier.SetParent(this);
            this.AddSpan(identifier.GetSpan());
            this.identifiers.Add(identifier);
        }


        public override IEnumerable<ASTNode> EnumerateChildren()
        {
            foreach (var identifier in this.identifiers)
                yield return identifier;
        }
    }
}
