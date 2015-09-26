
namespace Trapl.Codegen
{
    public class CGenerator
    {
        public static string Generate(Interface.Session session)
        {
            var gen = new CGenerator();
            gen.session = session;

            var result = "";
            result += gen.GenerateStructDecls() + "\n";
            result += gen.GenerateFunctDecls() + "\n";
            return result;
        }


        private Interface.Session session;


        private string GenerateStructDecls()
        {
            var result = "";
            foreach (var topDecl in session.topDecls)
            {
                if (topDecl.generic || topDecl.primitive || !topDecl.resolved)
                    continue;

                if (!(topDecl.def is Semantics.DefStruct))
                    continue;

                result += "struct " + MangleTopDeclName(topDecl) + ";\n";
            }
            return result;
        }
        

        private string GenerateFunctDecls()
        {
            var result = "";
            foreach (var topDecl in session.topDecls)
            {
                if (topDecl.generic || topDecl.primitive || !topDecl.resolved)
                    continue;

                if (!(topDecl.def is Semantics.DefFunct))
                    continue;

                var funct = (Semantics.DefFunct)topDecl.def;

                result +=
                    ConvertType(funct.returnType) + " " +
                    MangleTopDeclName(topDecl) + "(";

                for (int i = 0; i < funct.arguments.Count; i++)
                {
                    result += 
                        ConvertType(funct.arguments[i].type) + " " +
                        funct.arguments[i].name;

                    if (i < funct.arguments.Count - 1)
                        result += ", ";
                }

                result += ");\n";
            }
            return result;
        }


        private string MangleTopDeclName(Semantics.TopDecl topDecl)
        {
            var result = topDecl.qualifiedName + ManglePattern(topDecl.patternASTNode);
            return result;
        }


        private string ManglePattern(Grammar.ASTNode node)
        {
            if (node.kind == Grammar.ASTNodeKind.ParameterPattern)
            {
                if (node.ChildNumber() == 0)
                    return "";

                var result = "__b";
                for (int i = 0; i < node.ChildNumber(); i++)
                {
                    result += ManglePattern(node.Child(i));
                    if (i < node.ChildNumber() - 1)
                        result += "__";
                }
                return result + "e";
            }
            else
            {
                return MangleType(node);
            }
        }


        private string MangleType(Grammar.ASTNode node)
        {
            var result = "";
            
            for (int i = 2; i < node.ChildNumber(); i++)
            {
                if (node.Child(i).kind == Grammar.ASTNodeKind.Operator)
                    result += "Ptr__";
            }

            result += node.Child(0).GetExcerpt();
            result += ManglePattern(node.Child(1));
            return result;
        }


        private string ConvertType(Semantics.Type type)
        {
            if (type is Semantics.TypePointer)
                return ConvertType(((Semantics.TypePointer)type).pointeeType) + "*";
            else if (type is Semantics.TypeVoid)
                return "void";
            else if (type is Semantics.TypeStruct)
            {
                Semantics.TopDecl topDecl = null;
                string name = null;
                foreach (var decl in session.topDecls)
                {
                    if (((Semantics.TypeStruct)type).structDef == decl.def)
                    {
                        topDecl = decl;
                        name = decl.GetString();
                    }
                }

                if (name == null)
                    return "???";
                else if (name == "Int32")
                    return "int";
                else
                    return MangleTopDeclName(topDecl);
            }
            else
                return "???";
        }
    }
}
