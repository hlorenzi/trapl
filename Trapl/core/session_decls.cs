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


            public override int GetHashCode()
            {
                return base.GetHashCode();
            }
        }


        private NameTree<DeclReference> declTree = new NameTree<DeclReference>();
        private List<DeclStruct> declStructs = new List<DeclStruct>();
        private List<DeclFunct> declFuncts = new List<DeclFunct>();
        private int primitiveBool = -1;
        private int primitiveInt = -1;
        private int primitiveUInt = -1;


        public int CreateStruct(Name name, Grammar.ASTNodeDeclStruct declASTNode)
        {
            var decl = new DeclStruct();
            decl.declASTNode = declASTNode;
            this.declStructs.Add(decl);

            var declRef = new DeclReference
            {
                kind = DeclReference.Kind.Struct,
                index = this.declStructs.Count - 1
            };

            this.declTree.Add(name, declRef);
            return this.declStructs.Count - 1;
        }


        public int CreatePrimitiveStruct(Name name)
        {
            var decl = new DeclStruct();
            decl.primitive = true;
            decl.declASTNode = null;
            this.declStructs.Add(decl);

            var declRef = new DeclReference
            {
                kind = DeclReference.Kind.Struct,
                index = this.declStructs.Count - 1
            };

            this.declTree.Add(name, declRef);
            return this.declStructs.Count - 1;
        }


        public int CreateFunct(Name name)
        {
            var decl = new DeclFunct();
            this.declFuncts.Add(decl);

            var declRef = new DeclReference
            {
                kind = DeclReference.Kind.Funct,
                index = this.declFuncts.Count - 1
            };

            this.declTree.Add(name, declRef);
            return this.declFuncts.Count - 1;
        }


        public DeclStruct GetStruct(int structIndex)
        {
            return this.declStructs[structIndex];
        }


        public List<DeclStruct> GetStructs()
        {
            return this.declStructs;
        }


        public DeclFunct GetFunct(int functIndex)
        {
            return this.declFuncts[functIndex];
        }


        public List<DeclFunct> GetFuncts()
        {
            return this.declFuncts;
        }


        public int PrimitiveBool
        {
            get { return primitiveBool; }
            set { primitiveBool = value; }
        }


        public int PrimitiveInt
        {
            get { return primitiveInt; }
            set { primitiveInt = value; }
        }


        public int PrimitiveUInt
        {
            get { return primitiveUInt; }
            set { primitiveUInt = value; }
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


        public List<DeclReference> GetDeclsWithUseDirectives(Name name, bool isAbsolutePath, IList<UseDirective> useDirectives)
        {
            DeclReference decl;
            var foundDecls = new List<DeclReference>();

            if (!isAbsolutePath && useDirectives != null)
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
                    Diagnostics.MessageCode.Unknown,
                    "unknown '" + origName.GetString() + "'",
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


        public bool ValidateAsType(DeclReference decl, Name origName, Diagnostics.Span span)
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


        public bool ValidateAsFunct(DeclReference decl, Name origName, Diagnostics.Span span)
        {
            if (decl.kind != Core.Session.DeclReference.Kind.Funct)
            {
                this.AddMessage(
                    Diagnostics.MessageKind.Error,
                    Diagnostics.MessageCode.WrongDeclarationKind,
                    "'" + this.GetDeclName(decl).GetString() + "' is not a funct",
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


        public Name GetFunctName(int functIndex)
        {
            var declRef = new DeclReference
            {
                kind = DeclReference.Kind.Funct,
                index = functIndex
            };

            Name name;
            if (!this.declTree.FindByValue(declRef, out name))
                throw new ArgumentException("struct not found");

            return name;
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

                    else if (decl.Item2.kind == DeclReference.Kind.Funct)
                        PrintFunctToConsole(this.declFuncts[decl.Item2.index], "  ");

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


        public void PrintFunctToConsole(DeclFunct decl, string indentation)
        {
            for (var i = 0; i < decl.registerTypes.Count; i++)
            {
                Console.ResetColor();
                Console.Out.Write(indentation);

                Console.Out.Write("#r" + i);
                Console.Out.Write(" ");
                if (decl.registerMutabilities[i])
                    Console.Out.Write("mut ");
                Console.ForegroundColor = ConsoleColor.DarkGray;
                Console.Out.Write(decl.registerTypes[i].GetString(this));
                Console.ResetColor();
                Console.Out.WriteLine();
            }

            Console.Out.WriteLine();

            for (var i = 0; i < decl.localBindings.Count; i++)
            {
                Console.ResetColor();
                Console.Out.Write(indentation);

                Console.Out.Write(decl.localBindings[i].name.GetString());
                Console.Out.Write(" ");
                Console.ForegroundColor = ConsoleColor.DarkGray;
                Console.Out.Write("#r" + decl.localBindings[i].registerIndex);
                Console.ResetColor();
                Console.Out.WriteLine();
            }

            Console.Out.WriteLine();

            for (var i = 0; i < decl.segments.Count; i++)
            {
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.Out.Write(indentation);
                Console.Out.Write("segment #s" + i);
                Console.ResetColor();
                Console.Out.WriteLine();

                for (var j = 0; j < decl.segments[i].instructions.Count; j++)
                    decl.segments[i].instructions[j].PrintToConsole(indentation + "  ");

                decl.segments[i].outFlow.PrintToConsole(indentation + "  ");
            }
        }
    }
}