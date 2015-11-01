using Trapl.Diagnostics;
using System.Collections.Generic;


namespace Trapl.Semantics
{
    public static class TopDeclFinder
    {
        public static TopDecl FindStruct(Infrastructure.Session session, Grammar.ASTNode nameASTNode)
        {
            // Find the TopDecls that match the name.
            var candidateTopDecls = session.topDecls.FindAll(
                decl => PathASTUtil.Compare(decl.pathASTNode, nameASTNode.Child(0)));

            if (candidateTopDecls.Count == 0)
            {
                session.diagn.Add(MessageKind.Error, MessageCode.UndeclaredIdentifier,
                    "'" + PathASTUtil.GetString(nameASTNode.Child(0)) + "' is not declared", nameASTNode.GetOriginalSpan());
                throw new Semantics.CheckException();
            }

            // Refine matching TopDecls by template compatibility.
            var template = TemplateASTUtil.ResolveTemplateFromName(session, nameASTNode);
            candidateTopDecls = candidateTopDecls.FindAll((decl) => template.IsMatch(decl.template));

            if (candidateTopDecls.Count == 0)
            {
                session.diagn.Add(MessageKind.Error, MessageCode.UndeclaredTemplate,
                    "no '" + PathASTUtil.GetString(nameASTNode.Child(0)) + "' declaration accepts this template", nameASTNode.GetOriginalSpan());
                throw new Semantics.CheckException();
            }
            else if (candidateTopDecls.Count > 1)
            {
                session.diagn.Add(MessageKind.Error, MessageCode.InferenceFailed,
                    "multiple '" + PathASTUtil.GetString(nameASTNode.Child(0)) + "' declarations accept this template", nameASTNode.GetOriginalSpan());
                throw new Semantics.CheckException();
            }

            // Ask the matching TopDecl to parse and resolve its definition, if not yet done.
            var matchingTopDecl = candidateTopDecls[0];
            matchingTopDecl.Resolve(session);

            // Check that the matching TopDecl is a struct.
            if (!(matchingTopDecl.def is DefStruct))
            {
                session.diagn.Add(MessageKind.Error, MessageCode.WrongTopDeclKind,
                    "'" + PathASTUtil.GetString(nameASTNode.Child(0)) + "' is not a struct", nameASTNode.GetOriginalSpan());
                throw new Semantics.CheckException();
            }

            return matchingTopDecl;
        }


        public static bool IsFunct(Infrastructure.Session session, Grammar.ASTNode nameASTNode)
        {
            // Find the TopDecls that match the name.
            var candidateTopDecls = session.topDecls.FindAll(
                decl => PathASTUtil.Compare(decl.pathASTNode, nameASTNode.Child(0)));

            if (candidateTopDecls.Count == 0)
                return false;

            if (!(candidateTopDecls[0].def is DefFunct))
                return false;

            return true;
        }


        public static List<TopDecl> FindFunctsNamed(Infrastructure.Session session, Grammar.ASTNode nameASTNode)
        {
            if (nameASTNode.kind != Grammar.ASTNodeKind.Name)
                throw new InternalException("node is not a Name");

            // Find the TopDecls that match the name.
            var candidateTopDecls = session.topDecls.FindAll(
                decl => PathASTUtil.Compare(decl.pathASTNode, nameASTNode.Child(0)));

            return candidateTopDecls.FindAll(d => d.def is DefFunct);
        }
    }
}
