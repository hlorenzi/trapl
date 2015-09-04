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
        public Grammar.ASTNode templateListNode;
        public Grammar.ASTNode syntaxNode;
        public SourceCode source;
        public Diagnostics.Span nameSpan;
    }


    public class Analyzer
    {
        public static Output Pass(Grammar.AST syn, SourceCode source, Diagnostics.Collection diagn)
        {
            var output = new Output();

            foreach (var node in syn.topDecls)
            {
                try
                {
                    EnsureKind(node, Grammar.ASTNodeKind.TopLevelDecl, source, diagn);
                    EnsureKind(node.Child(0), Grammar.ASTNodeKind.Identifier, source, diagn);
                    EnsureKind(node.Child(0).Child(0), Grammar.ASTNodeKind.Name, source, diagn);

                    var nameNode = node.Child(0).Child(0);
                    var declNode = node.Child(1);
                    Grammar.ASTNode templNode = null;
                    if (node.Child(0).ChildNumber() > 1)
                    {
                        EnsureKind(node.Child(0).Child(1), Grammar.ASTNodeKind.TemplateList, source, diagn);
                        templNode = node.Child(0).Child(1);
                    }

                    var decl = new Declaration();
                    decl.name = source.GetExcerpt(nameNode.Span());
                    decl.nameSpan = node.Child(0).Span();
                    decl.syntaxNode = declNode;
                    decl.templateListNode = templNode;
                    decl.source = source;

                    if (declNode.kind == Grammar.ASTNodeKind.FunctDecl)
                        output.functDecls.Add(decl);
                    else if (declNode.kind == Grammar.ASTNodeKind.StructDecl)
                        output.structDecls.Add(decl);
                    else if (declNode.kind == Grammar.ASTNodeKind.TraitDecl)
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


        private static ParserException ErrorAt(Grammar.ASTNode node, SourceCode source, Diagnostics.Collection diagn)
        {
            diagn.Add(MessageKind.Error, MessageCode.Internal, "unexpected node", source, node.Span());
            return new ParserException();
        }


        private static void EnsureKind(Grammar.ASTNode node, Grammar.ASTNodeKind kind, SourceCode source, Diagnostics.Collection diagn)
        {
            if (node.kind != kind)
                throw ErrorAt(node, source, diagn);
        }
    }
}
