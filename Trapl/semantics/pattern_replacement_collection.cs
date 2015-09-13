using System.Collections.Generic;


namespace Trapl.Semantics
{
    public class PatternReplacementCollection
    {
        public Dictionary<string, List<Grammar.ASTNode>> nameToASTNodeMap = new Dictionary<string, List<Grammar.ASTNode>>();

        
        public void Merge(PatternReplacementCollection other)
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
                        result += "'" + ASTTypeUtil.GetString(pair.Value[i]) + "'";
                        if (i < pair.Value.Count - 1)
                            result += ", ";
                    }
                    
                }
                return result + "]";
            }
        }
    }
}
