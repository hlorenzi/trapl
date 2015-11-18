
namespace Trapl.Infrastructure
{
    public static class DeclASTConverter
    {
        public static void AddToDecls(Infrastructure.Session session, Grammar.ASTNode declASTNode)
        {
            if (declASTNode.ChildIs(1, Grammar.ASTNodeKind.StructDecl))
            {
                var decl = new DeclStruct();
                decl.declASTNode = declASTNode;
                decl.nameASTNode = declASTNode.Child(0);
                decl.name.pathASTNode = declASTNode.Child(0).Child(0);
                decl.defASTNode = declASTNode.Child(1);
                decl.templateASTNode = Semantics.TemplateUtil.GetTemplateASTOrNull(declASTNode.Child(0));
                session.structDecls.Add(decl.name.pathASTNode, decl);
            }
            else if (declASTNode.ChildIs(1, Grammar.ASTNodeKind.FunctDecl))
            {
                var decl = new DeclFunct();
                decl.declASTNode = declASTNode;
                decl.nameASTNode = declASTNode.Child(0);
                decl.name.pathASTNode = declASTNode.Child(0).Child(0);
                decl.defASTNode = declASTNode.Child(1);
                decl.templateASTNode = Semantics.TemplateUtil.GetTemplateASTOrNull(declASTNode.Child(0));
                session.functDecls.Add(decl.name.pathASTNode, decl);
            }
            else if (declASTNode.ChildIs(1, Grammar.ASTNodeKind.TraitDecl))
            {
            }
            else
                throw new InternalException("unreachable");
        }


        public static void AddPrimitives(Infrastructure.Session session)
        {
            MakePrimitiveStruct(session, "Bool", ref session.primitiveBool);
            MakePrimitiveStruct(session, "Int", ref session.primitiveInt);
            MakePrimitiveStruct(session, "Int8", ref session.primitiveInt8);
            MakePrimitiveStruct(session, "Int16", ref session.primitiveInt16);
            MakePrimitiveStruct(session, "Int32", ref session.primitiveInt32);
            MakePrimitiveStruct(session, "Int64", ref session.primitiveInt64);
            MakePrimitiveStruct(session, "UInt", ref session.primitiveUInt);
            MakePrimitiveStruct(session, "UInt8", ref session.primitiveUInt8);
            MakePrimitiveStruct(session, "UInt16", ref session.primitiveUInt16);
            MakePrimitiveStruct(session, "UInt32", ref session.primitiveUInt32);
            MakePrimitiveStruct(session, "UInt64", ref session.primitiveUInt64);
            MakePrimitiveStruct(session, "Float32", ref session.primitiveFloat32);
            MakePrimitiveStruct(session, "Float64", ref session.primitiveFloat64);
        }


        private static void MakePrimitiveStruct(Infrastructure.Session session, string name, ref DeclStruct sessionPrimitive)
        {
            var decl = new DeclStruct();
            decl.primitive = true;

            decl.declASTNode = null;

            decl.name.pathASTNode = new Grammar.ASTNode(Grammar.ASTNodeKind.Path);
            decl.name.pathASTNode.AddChild(new Grammar.ASTNode(Grammar.ASTNodeKind.Identifier));
            decl.name.pathASTNode.children[0].OverwriteExcerpt(name);

            decl.defASTNode = null;
            decl.templateASTNode = null;

            session.structDecls.Add(decl.name.pathASTNode, decl);
            sessionPrimitive = decl;
        }
    }
}
