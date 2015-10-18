using Trapl.Diagnostics;
using System.Collections.Generic;


namespace Trapl.Semantics
{
    public static class ASTTopDeclFinder
    {
        public static TopDecl FindStruct(Infrastructure.Session session, Grammar.ASTNode nameASTNode)
        {
            // Find the TopDecls that match the name.
            var candidateTopDecls = session.topDecls.FindAll(
                decl => ASTPathUtil.Compare(decl.pathASTNode, nameASTNode.Child(0)));

            if (candidateTopDecls.Count == 0)
            {
                session.diagn.Add(MessageKind.Error, MessageCode.UndeclaredIdentifier,
                    "'" + ASTPathUtil.GetString(nameASTNode.Child(0)) + "' is not declared", nameASTNode.GetOriginalSpan());
                throw new Semantics.CheckException();
            }

            // Ask the matching TopDecl to parse and resolve its definition, if not yet done.
            var matchingTopDecl = candidateTopDecls[0];
            matchingTopDecl.Resolve(session);

            // Check that the matching TopDecl is a struct.
            if (!(matchingTopDecl.def is DefStruct))
            {
                session.diagn.Add(MessageKind.Error, MessageCode.WrongTopDeclKind,
                    "'" + ASTPathUtil.GetString(nameASTNode.Child(0)) + "' is not a struct", nameASTNode.GetOriginalSpan());
                throw new Semantics.CheckException();
            }

            return matchingTopDecl;
        }


        public static bool IsFunct(Infrastructure.Session session, Grammar.ASTNode nameASTNode)
        {
            // Find the TopDecls that match the name.
            var candidateTopDecls = session.topDecls.FindAll(
                decl => ASTPathUtil.Compare(decl.pathASTNode, nameASTNode.Child(0)));

            if (candidateTopDecls.Count == 0)
                return false;

            if (!(candidateTopDecls[0].def is DefFunct))
                return false;

            return true;
        }


        public static List<TopDecl> FindFunctsNamed(Infrastructure.Session session, Grammar.ASTNode nameASTNode)
        {
            // Find the TopDecls that match the name.
            var candidateTopDecls = session.topDecls.FindAll(
                decl => ASTPathUtil.Compare(decl.pathASTNode, nameASTNode.Child(0)));

            return candidateTopDecls.FindAll(d => d.def is DefFunct);
        }
    }
}
