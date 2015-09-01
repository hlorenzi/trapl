using System;
using System.Collections.Generic;
using Trapl.Diagnostics;


namespace Trapl.Structure
{
    public class Output
    {
        public List<Declaration> functDecls = new List<Declaration>();
        public List<Declaration> structDecls = new List<Declaration>();
        public List<Declaration> traitDecls = new List<Declaration>();


        public void Merge(Output other)
        {
            this.functDecls.AddRange(other.functDecls);
            this.structDecls.AddRange(other.structDecls);
            this.traitDecls.AddRange(other.traitDecls);
        }
    }


    public class Declaration
    {
        public string name;
        public Syntax.Node syntaxNode;
        public Source source;
    }


    public class Analyzer
    {
        public static Output Pass(Syntax.Output syn, Source source, Diagnostics.MessageList diagn)
        {
            var output = new Output();

            foreach (var node in syn.topDecls)
            {
                try
                {
                    EnsureKind(node, Syntax.NodeKind.TopLevelDecl, source, diagn);

                    var nameNode = node.Child(0);
                    var declNode = node.Child(1);

                    EnsureKind(nameNode, Syntax.NodeKind.Identifier, source, diagn);

                    var decl = new Declaration();
                    decl.name = source.Excerpt(nameNode.Span());
                    decl.syntaxNode = declNode;
                    decl.source = source;

                    if (declNode.kind == Syntax.NodeKind.FunctDecl)
                        output.functDecls.Add(decl);
                    else if (declNode.kind == Syntax.NodeKind.StructDecl)
                        output.structDecls.Add(decl);
                    else if (declNode.kind == Syntax.NodeKind.TraitDecl)
                        output.traitDecls.Add(decl);
                    else
                        throw ErrorAt(declNode, source, diagn);
                }
                catch (ParserException)
                {

                }
            }

            return output;
        }


        private class ParserException : Exception
        {

        }


        private static ParserException ErrorAt(Syntax.Node node, Source source, Diagnostics.MessageList diagn)
        {
            diagn.Add(MessageKind.Error, MessageCode.Internal, "unexpected node", source, node.Span());
            return new ParserException();
        }


        private static void EnsureKind(Syntax.Node node, Syntax.NodeKind kind, Source source, Diagnostics.MessageList diagn)
        {
            if (node.kind != kind)
                throw ErrorAt(node, source, diagn);
        }
    }
}
