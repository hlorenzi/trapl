using System;
using Trapl.Diagnostics;


namespace Trapl.Semantics
{
    public class CheckTopDecl
    {
        public static void Check(Interface.Session session, Grammar.AST ast, Interface.SourceCode source)
        {
            AddPrimitive(session, "Void");
            AddPrimitive(session, "Bool");
            AddPrimitive(session, "Int8");
            AddPrimitive(session, "Int16");
            AddPrimitive(session, "Int32");
            AddPrimitive(session, "Int64");
            AddPrimitive(session, "UInt8");
            AddPrimitive(session, "UInt16");
            AddPrimitive(session, "UInt32");
            AddPrimitive(session, "UInt64");
            AddPrimitive(session, "Float32");
            AddPrimitive(session, "Float64");

            foreach (var node in ast.topDecls)
            {
                try
                {
                    EnsureKind(session, node, Grammar.ASTNodeKind.TopLevelDecl);
                    EnsureKind(session, node.Child(0), Grammar.ASTNodeKind.Identifier);
                    EnsureKind(session, node.Child(0).Child(0), Grammar.ASTNodeKind.Name);

                    var qualifiedNameNode = node.Child(0).Child(0);
                    var qualifiedName = qualifiedNameNode.GetExcerpt();

                    var patternNode = new Grammar.ASTNode(Grammar.ASTNodeKind.ParameterPattern);

                    if (node.Child(0).ChildIs(1, Grammar.ASTNodeKind.ParameterPattern) ||
                        node.Child(0).ChildIs(1, Grammar.ASTNodeKind.VariadicParameterPattern))
                    {
                        patternNode = node.Child(0).Child(1);
                    }

                    var defNode = node.Child(1);

                    var topDecl = new TopDecl();
                    topDecl.declASTNode = node;
                    topDecl.qualifiedName = qualifiedName;
                    topDecl.qualifiedNameASTNode = qualifiedNameNode;
                    topDecl.patternASTNode = patternNode;
                    topDecl.generic = ASTPatternUtil.IsGeneric(patternNode);
                    topDecl.defASTNode = defNode;
                    session.topDecls.Add(topDecl);

                    if (defNode.kind != Grammar.ASTNodeKind.StructDecl &&
                        defNode.kind != Grammar.ASTNodeKind.FunctDecl)
                        throw ErrorAt(session, "Decl", defNode);
                }
                catch (CheckException) { }
            }
        }


        private static void AddPrimitive(Interface.Session session, string name)
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
