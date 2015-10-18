using System;
using System.Collections.Generic;
using Trapl.Diagnostics;


namespace Trapl.Semantics
{
    public class DefStruct : Def
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


        public DefStruct(TopDecl topDecl) : base(topDecl)
        {

        }


        public DefStruct Clone()
        {
            var def = (DefStruct)this.MemberwiseClone();
            def.fields = new List<Field>();
            foreach (var field in this.fields)
                def.fields.Add(field.Clone());
            return def;
        }


        public void Resolve(Infrastructure.Session session, TopDecl topDecl, Grammar.ASTNode defNode)
        {
            foreach (var fieldNode in defNode.EnumerateChildren())
            {
                if (fieldNode.kind != Grammar.ASTNodeKind.StructField)
                    throw new InternalException("node is not a StructFieldDecl");

                var field = new DefStruct.Field();
                field.nameASTNode = fieldNode.Child(0);
                field.declSpan = fieldNode.Span();

                for (int i = 0; i < fields.Count; i++)
                {
                    if (ASTPathUtil.Compare(fields[i].nameASTNode.Child(0), field.nameASTNode.Child(0)))
                    {
                        session.diagn.Add(MessageKind.Error, MessageCode.DuplicateDecl,
                            "duplicate field '" + ASTPathUtil.GetString(field.nameASTNode.Child(0)) + "'",
                            field.nameASTNode.Span(), fields[i].nameASTNode.Span());
                        break;
                    }
                }

                try
                {
                    //session.diagn.PushContext(new MessageContext("while resolving type '" + ASTTypeUtil.GetString(fieldNode.Child(1)) + "'", fieldNode.GetOriginalSpan()));
                    field.type = ASTTypeUtil.Resolve(session, fieldNode.Child(1));
                    fields.Add(field);
                }
                catch (Semantics.CheckException) { }
                finally { /*session.diagn.PopContext();*/ }
            }
        }


        public override void PrintToConsole(Infrastructure.Session session, int indentLevel)
        {
            foreach (var member in this.fields)
            {
                Console.Out.WriteLine(
                    new string(' ', indentLevel * 2) +
                    ASTPathUtil.GetString(member.nameASTNode.Child(0)) + ": " +
                    member.type.GetString(session));
            }
        }
    }
}
