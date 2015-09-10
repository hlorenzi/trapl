using System.Collections.Generic;


namespace Trapl.Semantics
{
    public class DefinitionFunct
    {
        public class Variable
        {
            public string name;
            public Type type;
            public Diagnostics.Span declSpan;
            public bool outOfScope;

            public Variable(string name, Type type, Diagnostics.Span declSpan)
            {
                this.name = name;
                this.type = type;
                this.declSpan = declSpan;
                this.outOfScope = false;
            }
        }

        public List<Variable> arguments = new List<Variable>();
        public Type returnType;

        public List<Variable> localVariables = new List<Variable>();
        //public CodeSegment body;

        public DeclPattern templateList;
        public Grammar.ASTNode declASTNode;

        public Interface.SourceCode source;
        public Diagnostics.Span nameSpan;
        public Diagnostics.Span declSpan;
    }
}
