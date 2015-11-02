using System;
using System.Collections.Generic;
using Trapl.Diagnostics;


namespace Trapl.Semantics
{
    public class DeclStruct : Decl
    {
        public class Field
        {
            public Grammar.ASTNode nameASTNode;
            public Type type;
            public Diagnostics.Span declSpan;


            public Field Clone()
            {
                return (Field)this.MemberwiseClone();
            }
        }


        public List<Field> fields = new List<Field>();
        public bool resolving = false;


        public DeclStruct()
        {

        }


        public DeclStruct Clone()
        {
            var def = (DeclStruct)this.MemberwiseClone();
            def.fields = new List<Field>();
            foreach (var field in this.fields)
                def.fields.Add(field.Clone());
            return def;
        }


        public override void Resolve(Infrastructure.Session session)
        {
            if (this.resolved)
                return;

            if (this.resolving)
            {
                session.diagn.Add(MessageKind.Error, MessageCode.StructRecursion,
                    "infinite struct recursion", this.nameASTNode.Span());
                throw new Semantics.CheckException();
            }

            this.resolving = true;

            foreach (var fieldNode in this.defASTNode.EnumerateChildren())
            {
                if (fieldNode.kind != Grammar.ASTNodeKind.StructField)
                    throw new InternalException("node is not a StructField");

                var field = new DeclStruct.Field();
                field.nameASTNode = fieldNode.Child(0);
                field.declSpan = fieldNode.Span();

                for (int i = 0; i < fields.Count; i++)
                {
                    if (PathASTUtil.Compare(fields[i].nameASTNode.Child(0), field.nameASTNode.Child(0)))
                    {
                        session.diagn.Add(MessageKind.Error, MessageCode.DuplicateDecl,
                            "duplicate field '" + PathASTUtil.GetString(field.nameASTNode.Child(0)) + "'",
                            field.nameASTNode.Span(), fields[i].nameASTNode.Span());
                        break;
                    }
                }

                try
                {
                    field.type = TypeASTUtil.Resolve(session, fieldNode.Child(1), true);
                    fields.Add(field);
                }
                catch (Semantics.CheckException) { }
            }

            this.resolved = true;
            this.resolving = false;
        }


        public override void PrintToConsole(Infrastructure.Session session, int indentLevel)
        {
            foreach (var member in this.fields)
            {
                Console.Out.WriteLine(
                    new string(' ', indentLevel * 2) +
                    PathASTUtil.GetString(member.nameASTNode.Child(0)) + ": " +
                    member.type.GetString(session));
            }
        }
    }
}
