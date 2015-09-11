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
                if (child.kind == Grammar.ASTNodeKind.GenericIdentifier)
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
                return GetStringRecursive(session, this.astNode);
        }


        private string GetStringRecursive(Interface.Session session, Grammar.ASTNode node)
        {
            if (node.kind == Grammar.ASTNodeKind.GenericPattern)
            {
                var result = "<";
                for (int i = 0; i < node.ChildNumber(); i++)
                {
                    result += GetStringRecursive(session, node.Child(i));
                    if (i < node.ChildNumber() - 1)
                        result += ", ";
                }
                return result + ">";
            }
            else
            {
                var result = node.GetExcerpt(this.src);
                return result;
            }
        }


        public int GetGenericParameterNumber()
        {
            return GetGenericParameterNumberRecursive(this.astNode);
        }


        private int GetGenericParameterNumberRecursive(Grammar.ASTNode node)
        {
            var result = 0;
            if (node.kind == Grammar.ASTNodeKind.GenericIdentifier)
                result += 1;

            foreach (var child in node.EnumerateChildren())
            {
                result += GetGenericParameterNumberRecursive(child);
            }
            return result;
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
        public class SubstitutionData
        {
            public Grammar.ASTNode astNode;
            public Interface.SourceCode source;
        }

        public Dictionary<string, List<SubstitutionData>> nameToASTNodeMap = new Dictionary<string, List<SubstitutionData>>();

        
        public void Merge(DeclPatternSubstitution other)
        {
            foreach (var pair in other.nameToASTNodeMap)
                this.nameToASTNodeMap.Add(pair.Key, pair.Value);
        }


        public void Add(string name, Interface.SourceCode source, Grammar.ASTNode astNode)
        {
            List<SubstitutionData> list = null;
            if (!this.nameToASTNodeMap.TryGetValue(name, out list))
            {
                list = new List<SubstitutionData>();
                this.nameToASTNodeMap.Add(name, list);
            }
            var substData = new SubstitutionData();
            substData.astNode = astNode;
            substData.source = source;
            list.Add(substData);
        }


        public string GetString()
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
                        result += "'" + pair.Value[i].astNode.GetExcerpt(pair.Value[i].source) + "'";
                        if (i < pair.Value.Count - 1)
                            result += ", ";
                    }
                    
                }
                return result + "]";
            }
        }


        public void PrintDebug()
        {
            if (this.nameToASTNodeMap.Count > 0)
            {
                foreach (var pair in this.nameToASTNodeMap)
                {
                    Interface.Debug.BeginSection("GENERIC '" + pair.Key + "'");
                    for (int i = 0; i < pair.Value.Count; i++)
                    {
                        Interface.Debug.BeginSection("MATCH #" + i);
                        Interface.Debug.PrintAST(pair.Value[i].source, pair.Value[i].astNode);
                        Interface.Debug.EndSection();
                    }
                    Interface.Debug.EndSection();
                }
            }
        }
    }
}
