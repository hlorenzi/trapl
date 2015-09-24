using System;
using Trapl.Diagnostics;


namespace Trapl.Semantics
{
    public class CheckTopDecl
    {
        public static void Check(Interface.Session session, Grammar.AST ast, Interface.SourceCode source)
        {
            AddPrimitiveStruct(session, "Bool");
            AddPrimitiveStruct(session, "Int8");
            AddPrimitiveStruct(session, "Int16");
            AddPrimitiveStruct(session, "Int32");
            AddPrimitiveStruct(session, "Int64");
            AddPrimitiveStruct(session, "UInt8");
            AddPrimitiveStruct(session, "UInt16");
            AddPrimitiveStruct(session, "UInt32");
            AddPrimitiveStruct(session, "UInt64");
            AddPrimitiveStruct(session, "Float32");
            AddPrimitiveStruct(session, "Float64");

            var numTypes = new string[] { "Int8", "Int16", "Int32", "Int64", "UInt8", "UInt16", "UInt32", "UInt64", "Float32", "Float64" };
            var arithBinOps = new string[] { "add", "sub", "mul", "div", "rem", "and", "or", "xor" };
            var arithUnOps = new string[] { "neg" };
            var relOps = new string[] { "eq", "noteq", "less", "lesseq", "greater", "greatereq"};

            foreach (var op in arithBinOps)
            {
                foreach (var type in numTypes)
                    AddPrimitiveFunct(session,
                        op + "::<" + type + ", " + type + ">",
                        "funct(x: " + type + ", y: " + type + " -> " + type + ")");
            }

            foreach (var op in arithUnOps)
            {
                foreach (var type in numTypes)
                    AddPrimitiveFunct(session,
                        op + "::<" + type + ">",
                        "funct(x: " + type + " -> " + type + ")");
            }

            AddPrimitiveFunct(session, "not::<Bool>", "funct(x: Bool -> Bool)");
            AddPrimitiveFunct(session, "and::<Bool, Bool>", "funct(x: Bool, y: Bool -> Bool)");
            AddPrimitiveFunct(session, "or::<Bool, Bool>", "funct(x: Bool, y: Bool -> Bool)");
            AddPrimitiveFunct(session, "xor::<Bool, Bool>", "funct(x: Bool, y: Bool -> Bool)");

            foreach (var op in relOps)
            {
                foreach (var type in numTypes)
                    AddPrimitiveFunct(session,
                        op + "::<" + type + ", " + type + ">",
                        "funct(x: " + type + ", y: " + type + " -> Bool)");
            }

            foreach (var node in ast.topDecls)
            {
                try
                {
                    EnsureKind(session, node, Grammar.ASTNodeKind.TopLevelDecl);
                    AddTopDecl(session, node, node.Child(0), node.Child(1));
                }
                catch (CheckException) { }
            }
        }


        private static void AddTopDecl(Interface.Session session, Grammar.ASTNode topDeclASTNode, Grammar.ASTNode nameASTNode, Grammar.ASTNode defASTNode)
        {
            var qualifiedNameNode = nameASTNode.Child(0);
            var qualifiedName = qualifiedNameNode.GetExcerpt();

            var patternNode = new Grammar.ASTNode(Grammar.ASTNodeKind.ParameterPattern);

            if (nameASTNode.ChildIs(1, Grammar.ASTNodeKind.ParameterPattern) ||
                nameASTNode.ChildIs(1, Grammar.ASTNodeKind.VariadicParameterPattern))
            {
                patternNode = nameASTNode.Child(1);
            }

            var topDecl = new TopDecl();
            topDecl.declASTNode = topDeclASTNode;
            topDecl.qualifiedName = qualifiedName;
            topDecl.qualifiedNameASTNode = qualifiedNameNode;
            topDecl.patternASTNode = patternNode;
            topDecl.generic = ASTPatternUtil.IsGeneric(patternNode);
            topDecl.defASTNode = defASTNode;
            session.topDecls.Add(topDecl);

            if (defASTNode.kind != Grammar.ASTNodeKind.StructDecl &&
                defASTNode.kind != Grammar.ASTNodeKind.FunctDecl)
                throw ErrorAt(session, "Decl", defASTNode);
        }


        private static void AddPrimitiveStruct(Interface.Session session, string name)
        {
            var topDecl = new TopDecl();
            topDecl.qualifiedName = name;
            topDecl.qualifiedNameASTNode = null;
            topDecl.patternASTNode = new Grammar.ASTNode(Grammar.ASTNodeKind.ParameterPattern);
            topDecl.defASTNode = null;
            topDecl.def = new DefStruct();
            topDecl.resolved = true;
            session.topDecls.Add(topDecl);
        }


        private static void AddPrimitiveFunct(Interface.Session session, string name, string functHeader)
        {
            var nameTokens = Grammar.Tokenizer.Tokenize(session, Interface.SourceCode.MakeFromString(name));
            var nameAST = Grammar.ASTParser.ParseType(session, nameTokens);

            var headerTokens = Grammar.Tokenizer.Tokenize(session, Interface.SourceCode.MakeFromString(functHeader + "{ }"));
            var headerAST = Grammar.ASTParser.ParseFunctDecl(session, headerTokens);

            AddTopDecl(session, null, nameAST, headerAST);
        }


        private static void EnsureKind(Interface.Session session, Grammar.ASTNode node, Grammar.ASTNodeKind kind)
        {
            if (node.kind != kind)
                throw ErrorAt(session, Enum.GetName(typeof(Grammar.ASTNodeKind), kind), node);
        }


        private static CheckException ErrorAt(Interface.Session session, string msg, Grammar.ASTNode node)
        {
            session.diagn.Add(MessageKind.Error, MessageCode.Internal, "expecting '" + msg + "' node", node.Span());
            return new CheckException();
        }
    }
}
