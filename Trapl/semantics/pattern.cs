using System;
using System.Collections.Generic;
using Trapl.Diagnostics;


namespace Trapl.Semantics
{
    public class DeclPattern
    {
        public Interface.SourceCode src;
        public Grammar.ASTNode astNode;


        public DeclPattern(Interface.SourceCode src, Grammar.ASTNode node)
        {
            if (node.kind != Grammar.ASTNodeKind.GenericPattern &&
                node.kind != Grammar.ASTNodeKind.VariadicGenericPattern)
                throw new InternalException("ASTNode is not a GenericPattern");

            this.src = src;
            this.astNode = node;
        }


        public void SetPattern(Grammar.ASTNode node)
        {
            if (node.kind != Grammar.ASTNodeKind.GenericPattern &&
                node.kind != Grammar.ASTNodeKind.VariadicGenericPattern)
                throw new InternalException("ASTNode is not a GenericPattern");

            this.astNode = node;
        }


        public bool IsGeneric()
        {
            if (this.astNode == null)
                return false;
            else
                return IsGenericRecursive(this.astNode);
        }


        private bool IsGenericRecursive(Grammar.ASTNode node)
        {
            var result = false;
            foreach (var child in node.EnumerateChildren())
            {
                if (child.kind == Grammar.ASTNodeKind.GenericType)
                    return true;
                else
                    result = (result || IsGenericRecursive(child));
            }
            return result;
        }


        public string GetString(Interface.Session session)
        {
            if (this.astNode.ChildNumber() == 0)
                return "<>";
            else
                return this.src.GetExcerpt(this.astNode.Span());
        }


        public DeclPatternSubstitution GetSubstitution(DeclPattern other)
        {
            if (this.astNode == null || other.astNode == null)
                throw new Semantics.InternalException("an AST node is null");

            var subst = new DeclPatternSubstitution();
            if (!ASTPatternMatcher.Match(subst, this, other))
                return null;
            else
                return subst;
        }
    }


    public class DeclPatternSubstitution
    {
        public Dictionary<string, List<Grammar.ASTNode>> nameToASTNodeMap = new Dictionary<string, List<Grammar.ASTNode>>();

        
        public void Merge(DeclPatternSubstitution other)
        {
            foreach (var pair in other.nameToASTNodeMap)
                this.nameToASTNodeMap.Add(pair.Key, pair.Value);
        }


        public void Add(string name, Grammar.ASTNode astNode)
        {
            List<Grammar.ASTNode> list = null;
            if (!this.nameToASTNodeMap.TryGetValue(name, out list))
            {
                list = new List<Grammar.ASTNode>();
                this.nameToASTNodeMap.Add(name, list);
            }
            list.Add(astNode);
        }


        public string GetString(Interface.Session session, Interface.SourceCode src)
        {
            if (this.nameToASTNodeMap.Count == 0)
                return "";
            else
            {
                var result = "[with ";
                var count = 0;
                foreach (var pair in this.nameToASTNodeMap)
                {
                    if (count != 0)
                        result += "; ";

                    count++;
                    result += pair.Key + " = ";
                    for (int i = 0; i < pair.Value.Count; i++)
                    {
                        result += "'" + src.GetExcerpt(pair.Value[i].Span()) + "'";
                        if (i < pair.Value.Count - 1)
                            result += ", ";
                    }
                    
                }
                return result + "]";
            }
        }
    }
}
