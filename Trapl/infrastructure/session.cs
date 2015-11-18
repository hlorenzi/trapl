using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Trapl.Infrastructure
{
    public class Session
    {
        public Session()
        {
            this.diagn = new Diagnostics.Collection();
            DeclASTConverter.AddPrimitives(this);
        }


        public static void Compile(Unit unit)
        {
            var session = new Session();
            session.AddUnit(unit);
            session.Resolve();
        }


        public void AddUnit(Unit unit)
        {
            var tokenCollection = Grammar.Tokenizer.Tokenize(this, unit);
            var declNodes = Grammar.ASTParser.Parse(this, tokenCollection);

            //foreach (var decl in declNodes)
            //    decl.PrintDebugRecursive(0, 0);

            foreach (var declNode in declNodes)
                DeclASTConverter.AddToDecls(this, declNode);
        }


        public void Resolve()
        {
            if (this.diagn.ContainsErrors()) return;
            this.structDecls.ForEach(decl =>
            {
                try { decl.ResolveTemplate(this); }
                catch (CheckException) { }
            });

            if (this.diagn.ContainsErrors()) return;
            this.functDecls.ForEach(decl =>
            {
                try { decl.ResolveTemplate(this); }
                catch (CheckException) { }
            });

            if (this.diagn.ContainsErrors()) return;
            this.structDecls.ForEach(decl => decl.Resolve(this));

            if (this.diagn.ContainsErrors()) return;
            this.functDecls.ForEach(decl => decl.Resolve(this));

            if (this.diagn.ContainsErrors()) return;
            this.functDecls.ForEach(decl => decl.ResolveBody(this));
        }


        public Diagnostics.Collection diagn = new Diagnostics.Collection();

        public DeclList<DeclStruct> structDecls = new DeclList<DeclStruct>();
        public DeclList<DeclFunct> functDecls = new DeclList<DeclFunct>();
        public DeclStruct primitiveBool;
        public DeclStruct primitiveInt;
        public DeclStruct primitiveInt8;
        public DeclStruct primitiveInt16;
        public DeclStruct primitiveInt32;
        public DeclStruct primitiveInt64;
        public DeclStruct primitiveUInt;
        public DeclStruct primitiveUInt8;
        public DeclStruct primitiveUInt16;
        public DeclStruct primitiveUInt32;
        public DeclStruct primitiveUInt64;
        public DeclStruct primitiveFloat32;
        public DeclStruct primitiveFloat64;


        public void PrintDefs()
        {
            foreach (var decl in this.structDecls.Enumerate())
            {
                if (decl.primitive)
                    continue;

                Console.ForegroundColor = ConsoleColor.White;
                Console.Out.WriteLine("STRUCT " + decl.GetString(this));
                Console.ResetColor();

                if (!decl.resolved)
                    Console.Out.WriteLine("  unresolved");
                else
                    decl.PrintToConsole(this, 1);

                Console.Out.WriteLine();
            }


            foreach (var decl in this.functDecls.Enumerate())
            {
                if (decl.primitive)
                    continue;

                Console.ForegroundColor = ConsoleColor.White;
                Console.Out.WriteLine("FUNCT " + decl.GetString(this));
                Console.ResetColor();

                if (!decl.resolved)
                    Console.Out.WriteLine("  unresolved");
                else
                    decl.PrintToConsole(this, 1);

                Console.Out.WriteLine();
            }
        }
    }
}
