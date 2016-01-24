using System;
using System.Collections.Generic;


namespace Trapl.Infrastructure
{
    public partial class Session
    {
        private class DeclReference
        {
            public enum Kind
            {
                Struct, Funct
            }


            public Kind kind;
            public int index;


            public override bool Equals(object obj)
            {
                var other = obj as DeclReference;

                if (other == null)
                    return false;

                return this.kind == other.kind && this.index == other.index;
            }
        }


        private NameTree<DeclReference> declTree = new NameTree<DeclReference>();
        private List<DeclStruct> declStructs = new List<DeclStruct>();
        private List<int> declFuncts = new List<int>();


        public int CreateStruct(Name name)
        {
            var decl = new DeclStruct();
            this.declStructs.Add(decl);

            var declRef = new DeclReference
            {
                kind = DeclReference.Kind.Struct,
                index = this.declStructs.Count - 1
            };

            this.declTree.Add(declRef, name);
            return this.declStructs.Count - 1;
        }


        public Name GetStructName(int structIndex)
        {
            var declRef = new DeclReference
            {
                kind = DeclReference.Kind.Struct,
                index = structIndex
            };

            Name name;
            if (this.declTree.FindByValue(declRef, out name))
                return name;

            return null;
        }


        public int AddStructField(int structIndex, Name name, Type fieldType)
        {
            var decl = this.declStructs[structIndex];
            var fieldIndex = decl.fieldTypes.Count;
            decl.fieldTypes.Add(fieldType);
            decl.fieldNames.Add(fieldIndex, name);
            return fieldIndex;
        }


        public void PrintDeclsToConsole(bool printContents)
        {
            foreach (var decl in this.declTree.Enumerate())
            {
                Console.ResetColor();
                Console.Out.Write(decl.Item1.GetString());
                Console.Out.Write(" ");
                Console.ForegroundColor = ConsoleColor.DarkGray;
                Console.Out.Write(Enum.GetName(typeof(DeclReference.Kind), decl.Item2.kind).ToLower());
                Console.ResetColor();
                Console.Out.WriteLine();

                if (printContents)
                {
                    if (decl.Item2.kind == DeclReference.Kind.Struct)
                        PrintStructToConsole(this.declStructs[decl.Item2.index], "  ");
                }
            }
        }


        public void PrintStructToConsole(DeclStruct decl, string indentation)
        {
            for (var i = 0; i < decl.fieldTypes.Count; i++)
            {
                Console.ResetColor();
                Console.Out.Write(indentation);

                Name fieldName;
                decl.fieldNames.FindByValue(i, out fieldName);
                Console.Out.Write(fieldName.GetString());
                Console.Out.Write(" ");
                Console.ForegroundColor = ConsoleColor.DarkGray;
                Console.Out.Write(decl.fieldTypes[i].GetString(this));
                Console.ResetColor();
                Console.Out.WriteLine();
            }
        }
    }
}
