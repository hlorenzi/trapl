using System;
using System.Collections.Generic;


namespace Trapl.Core
{
    public partial class Session
    {
        public class DeclReference
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

            this.declTree.Add(name, declRef);
            return this.declStructs.Count - 1;
        }


        public int GetDecl(Name name)
        {
            DeclReference decl;
            if (!this.declTree.FindByName(name, out decl))
                throw new ArgumentException("not found");

            return decl.index;
        }


        public bool TryGetDecl(Name name, out int index)
        {
            index = -1;

            DeclReference decl;
            if (!this.declTree.FindByName(name, out decl))
                return false;

            index = decl.index;
            return true;
        }


        public List<DeclReference> GetDeclsWithUseDirectives(Name name, bool isAbsolutePath, List<UseDirective> useDirectives)
        {
            DeclReference decl;
            var foundDecls = new List<DeclReference>();

            if (!isAbsolutePath)
            {
                foreach (var directive in useDirectives)
                {
                    var useAllDirective = directive as UseDirectiveAll;
                    if (useAllDirective != null)
                    {
                        if (this.declTree.FindByName(useAllDirective.name.Concatenate(name), out decl))
                            foundDecls.Add(decl);
                    }
                    else
                        throw new NotImplementedException();
                }
            }

            if (this.declTree.FindByName(name, out decl))
                foundDecls.Add(decl);

            return foundDecls;
        }


        public bool ValidateSingleDecl(List<DeclReference> decls, Name origName, Diagnostics.Span span)
        {
            if (decls.Count == 0)
            {
                this.AddMessage(
                    Diagnostics.MessageKind.Error,
                    Diagnostics.MessageCode.Undeclared,
                    "undeclared '" + origName.GetString() + "'",
                    span);
                return false;
            }
            else if (decls.Count > 1)
            {
                this.AddMessage(
                    Diagnostics.MessageKind.Error,
                    Diagnostics.MessageCode.AmbiguousDeclaration,
                    "ambiguous '" + origName.GetString() + "' between " +
                    "'" + this.GetDeclName(decls[0]).GetString() + "'" +
                    (decls.Count == 2 ? " and " : ", ") +
                    "'" + this.GetDeclName(decls[1]).GetString() + "'" +
                    (decls.Count > 2 ? ", and other " + (decls.Count - 2) : ""),
                    span);

                return false;
            }
            else
                return true;
        }


        public bool ValidateType(DeclReference decl, Name origName, Diagnostics.Span span)
        {
            if (decl.kind != Core.Session.DeclReference.Kind.Struct)
            {
                this.AddMessage(
                    Diagnostics.MessageKind.Error,
                    Diagnostics.MessageCode.WrongDeclarationKind,
                    "'" + this.GetDeclName(decl).GetString() + "' is not a type",
                    span);
                return false;
            }

            return true;
        }


    public Name GetDeclName(DeclReference declRef)
        {
            Name name;
            if (!this.declTree.FindByValue(declRef, out name))
                throw new ArgumentException("not found");

            return name;
        }


        public Name GetStructName(int structIndex)
        {
            var declRef = new DeclReference
            {
                kind = DeclReference.Kind.Struct,
                index = structIndex
            };

            Name name;
            if (!this.declTree.FindByValue(declRef, out name))
                throw new ArgumentException("struct not found");

            return name;
        }


        public int AddStructField(int structIndex, Name name, Type fieldType)
        {
            var decl = this.declStructs[structIndex];
            var fieldIndex = decl.fieldTypes.Count;
            decl.fieldTypes.Add(fieldType);
            decl.fieldNames.Add(name, fieldIndex);
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

                    Console.Out.WriteLine();
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
