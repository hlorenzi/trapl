using System.Collections.Generic;


namespace Trapl.Semantics
{
    public partial class TypeInferencer
    {
        public List<Core.Type> inferredTypes = new List<Core.Type>();
        public List<Rule> rules = new List<Rule>();


        public int AddSlot()
        {
            this.inferredTypes.Add(null);
            return this.inferredTypes.Count - 1;
        }


        public void InferType(int slot, Core.Type type)
        {
            this.inferredTypes[slot] = type;
        }


        public void AddRule(Rule rule)
        {
            this.rules.Add(rule);
        }
    }
}
