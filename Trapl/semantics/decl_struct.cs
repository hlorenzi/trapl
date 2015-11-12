using System;
using System.Collections.Generic;
using Trapl.Diagnostics;


namespace Trapl.Semantics
{
    public class DeclStruct : Decl
    {
        public class Field
        {
            public Name name;
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


        public int FindField(Grammar.ASTNode pathASTNode, Template template)
        {
            for (int i = 0; i < this.fields.Count; i++)
            {
                if (this.fields[i].name.Compare(pathASTNode, template))
                    return i;
            }
            return -1;
        }


        public override void Resolve(Infrastructure.Session session)
        {
            if (this.resolved || this.primitive)
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
                field.name = new Name(
                    fieldNode.Child(0).Span(),
                    fieldNode.Child(0).Child(0),
                    UtilASTTemplate.ResolveTemplateFromName(session, fieldNode.Child(0), true));
                field.declSpan = fieldNode.Span();

                for (int i = 0; i < fields.Count; i++)
                {
                    if (fields[i].name.Compare(field.name))
                    {
                        session.diagn.Add(MessageKind.Error, MessageCode.DuplicateDecl,
                            "duplicate field '" + field.name.GetString(session) + "'",
                            field.name.span, fields[i].name.span);
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
            foreach (var field in this.fields)
            {
                Console.Out.WriteLine(
                    new string(' ', indentLevel * 2) +
                    field.name.GetString(session) + ": " +
                    field.type.GetString(session));
            }
        }
    }
}
