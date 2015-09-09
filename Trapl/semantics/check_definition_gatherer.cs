using System;
using Trapl.Diagnostics;


namespace Trapl.Semantics
{
    public class DefinitionGatherer
    {
        public static void Gather(Interface.Session session, Grammar.AST ast, Interface.SourceCode source)
        {
            foreach (var node in ast.topDecls)
            {
                try
                {
                    EnsureKind(session, node, Grammar.ASTNodeKind.TopLevelDecl, source);
                    EnsureKind(session, node.Child(0), Grammar.ASTNodeKind.Identifier, source);
                    EnsureKind(session, node.Child(0).Child(0), Grammar.ASTNodeKind.Name, source);

                    var nameNode = node.Child(0).Child(0);
                    var declNode = node.Child(1);

                    var templateList = new Template();
                    if (node.Child(0).ChildIs(1, Grammar.ASTNodeKind.TemplateList))
                        templateList.Parse(source, node.Child(0).Child(1));

                    var fullName = source.GetExcerpt(nameNode.Span());

                    if (declNode.kind == Grammar.ASTNodeKind.FunctDecl)
                    {
                        var def = session.functDefs.Find(func => func.fullName == fullName);
                        if (def == null)
                        {
                            def = new Definition<DefinitionFunct>();
                            def.fullName = fullName;
                            session.functDefs.Add(def);
                        }

                        var f = new DefinitionFunct();
                        f.source = source;
                        f.nameSpan = node.Child(0).Span();
                        f.declSpan = node.Span();
                        f.declASTNode = declNode;
                        f.templateList = templateList;
                        f.source = source;

                        def.defs.Add(f);
                    }
                    else if (declNode.kind == Grammar.ASTNodeKind.StructDecl)
                    {
                        var def = session.structDefs.Find(s => s.fullName == fullName);
                        if (def == null)
                        {
                            def = new Definition<DefinitionStruct>();
                            def.fullName = fullName;
                            session.structDefs.Add(def);
                        }

                        var st = new DefinitionStruct();
                        st.source = source;
                        st.nameSpan = node.Child(0).Span();
                        st.declSpan = node.Span();
                        st.declASTNode = declNode;
                        st.templateList = templateList;
                        st.source = source;

                        def.defs.Add(st);
                    }
                    else
                        throw ErrorAt(session, "Decl", declNode, source);
                }
                catch (ParserException) { }
            }
        }


        private class ParserException : Exception
        {

        }


        private static ParserException ErrorAt(Interface.Session session, string msg, Grammar.ASTNode node, Interface.SourceCode source)
        {
            session.diagn.Add(MessageKind.Error, MessageCode.Internal, "expecting '" + msg + "' node", source, node.Span());
            return new ParserException();
        }


        private static void EnsureKind(Interface.Session session, Grammar.ASTNode node, Grammar.ASTNodeKind kind, Interface.SourceCode source)
        {
            if (node.kind != kind)
                throw ErrorAt(session, Enum.GetName(typeof(Grammar.ASTNodeKind), kind), node, source);
        }
    }
}
