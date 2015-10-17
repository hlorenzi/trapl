using System.Collections.Generic;


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
            result += gen.GenerateStructDefs() + "\n";
            result += gen.GenerateFunctDefs() + "\n";
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


        private string GenerateStructDefs()
        {
            var result = "";
            foreach (var topDecl in session.topDecls)
            {
                if (topDecl.generic || topDecl.primitive || !topDecl.resolved)
                    continue;

                if (!(topDecl.def is Semantics.DefStruct))
                    continue;

                var structDef = (Semantics.DefStruct)topDecl.def;

                result += "struct " + MangleTopDeclName(topDecl) + "\n";
                result += "{\n";
                foreach (var member in structDef.members)
                {
                    result += "\t" + ConvertType(member.type) + " " + member.name + ";\n";
                }
                result += "}\n\n";
            }
            return result;
        }


        private string GenerateFunctHeader(Semantics.TopDecl topDecl, Semantics.DefFunct f)
        {
            var result = "";

            result += MangleTopDeclName(topDecl) + "(";

            for (int i = 0; i < f.arguments.Count; i++)
            {
                result +=
                    ConvertTypeDecl(f.arguments[i].type).Replace("#", f.arguments[i].name);

                if (i < f.arguments.Count - 1)
                    result += ", ";
            }

            result += ")";
            return ConvertTypeDecl(f.returnType).Replace("#", result);
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

                result += GenerateFunctHeader(topDecl, (Semantics.DefFunct)topDecl.def) + ";\n";
            }
            return result;
        }


        private string GenerateFunctDefs()
        {
            var result = "";
            foreach (var topDecl in session.topDecls)
            {
                if (topDecl.generic || topDecl.primitive || !topDecl.resolved)
                    continue;

                if (!(topDecl.def is Semantics.DefFunct))
                    continue;

                var f = (Semantics.DefFunct)topDecl.def;

                result += GenerateFunctHeader(topDecl, f) + "\n";
                result += "{\n";

                // Generate local variables.
                for (int i = f.arguments.Count; i < f.localVariables.Count; i++)
                {
                    result +=
                        "\t" + ConvertType(f.localVariables[i].type) + " " +
                        f.localVariables[i].name + ";\n";
                }
                result += "\n";

                // Generate code segments.
                var segments = new List<Semantics.CodeSegment>();
                segments.Add(f.body);

                var curTempIndex = 0;
                var tempTypeStack = new Stack<Semantics.Type>();
                var tempExcerptStack = new Stack<string>();

                for (int i = 0; i < segments.Count; i++)
                {
                    result += "\t__segment" + i + ":;\n";

                    foreach (var c in segments[i].nodes)
                    {
                        if (c is Semantics.CodeNodePushLocal)
                        {
                            var code = (Semantics.CodeNodePushLocal)c;
                            tempExcerptStack.Push(f.localVariables[code.localIndex].name);
                            tempTypeStack.Push(f.localVariables[code.localIndex].type);
                        }
                        else if (c is Semantics.CodeNodePushLiteral)
                        {
                            var code = (Semantics.CodeNodePushLiteral)c;
                            tempExcerptStack.Push(code.literalExcerpt);
                            tempTypeStack.Push(code.type);
                        }
                        else if (c is Semantics.CodeNodePushFunct)
                        {
                            var code = (Semantics.CodeNodePushFunct)c;
                            var fType = new Semantics.TypeFunct((Semantics.DefFunct)code.topDecl.def);
                            tempExcerptStack.Push(MangleTopDeclName(code.topDecl));
                            tempTypeStack.Push(fType);
                            curTempIndex += 1;
                        }
                        else if (c is Semantics.CodeNodeCall)
                        {
                            var code = (Semantics.CodeNodeCall)c;
                            var fType = (Semantics.TypeFunct)tempTypeStack.Pop();

                            var excerpt = tempExcerptStack.Pop() + "(";

                            for (int j = 0; j < fType.argumentTypes.Count; j++)
                            {
                                excerpt += tempExcerptStack.Pop();
                                tempTypeStack.Pop();
                                if (j < fType.argumentTypes.Count - 1)
                                    excerpt += ", ";
                            }

                            excerpt += ")";

                            tempExcerptStack.Push(excerpt);
                            tempTypeStack.Push(fType.returnType);
                            curTempIndex += 1;
                        }
                        else if (c is Semantics.CodeNodeAccess)
                        {
                            var code = (Semantics.CodeNodeAccess)c;
                            var excerpt = tempExcerptStack.Pop();
                            var type = tempTypeStack.Pop();

                            tempExcerptStack.Push(excerpt + "." + code.accessedStruct.members[code.memberIndex].name);
                            tempTypeStack.Push(code.accessedStruct.members[code.memberIndex].type);
                        }
                        else if (c is Semantics.CodeNodeAddress)
                        {
                            var code = (Semantics.CodeNodeAddress)c;
                            var excerpt = tempExcerptStack.Pop();
                            var type = tempTypeStack.Pop();

                            tempExcerptStack.Push("&" + excerpt);
                            tempTypeStack.Push(new Semantics.TypePointer(type));
                        }
                        else if (c is Semantics.CodeNodeDereference)
                        {
                            var code = (Semantics.CodeNodeDereference)c;
                            var excerpt = tempExcerptStack.Pop();
                            var type = (Semantics.TypePointer)tempTypeStack.Pop();

                            tempExcerptStack.Push("(*" + excerpt + ")");
                            tempTypeStack.Push(type.pointeeType);
                        }
                        else if (c is Semantics.CodeNodeStore)
                        {
                            var excerptRhs = tempExcerptStack.Pop();
                            var excerptLhs = tempExcerptStack.Pop();
                            tempTypeStack.Pop();
                            tempTypeStack.Pop();

                            tempExcerptStack.Push(excerptLhs + " = " + excerptRhs);
                            tempTypeStack.Push(new Semantics.TypeVoid());
                        }
                        else if (c is Semantics.CodeNodePop)
                        {
                            if (tempExcerptStack.Count > 0)
                            {
                                result += "\t" + tempExcerptStack.Pop() + ";\n";
                                tempTypeStack.Pop();
                            }
                        }
                        else if (
                            c is Semantics.CodeNodeLocalBegin ||
                            c is Semantics.CodeNodeLocalEnd ||
                            c is Semantics.CodeNodeLocalInit ||
                            c is Semantics.CodeNodeIf)
                        {
                            continue;
                        }
                        else
                        {
                            result += "\t<unimplemented code>;\n";
                        }
                    }

                    if (segments[i].outwardPaths.Count == 0)
                        result += "\tgoto __segment_end;\n\n";
                    else if (segments[i].outwardPaths.Count == 1)
                    {
                        int index = segments.FindIndex(seg => seg == segments[i].outwardPaths[0]);
                        if (index < 0)
                        {
                            segments.Add(segments[i].outwardPaths[0]);
                            index = segments.Count - 1;
                        }
                        result += "\tgoto __segment" + index + ";\n\n";
                    }
                    else if (segments[i].outwardPaths.Count == 2)
                    {
                        int indexTrue = segments.FindIndex(seg => seg == segments[i].outwardPaths[0]);
                        if (indexTrue < 0)
                        {
                            segments.Add(segments[i].outwardPaths[0]);
                            indexTrue = segments.Count - 1;
                        }

                        int indexFalse = segments.FindIndex(seg => seg == segments[i].outwardPaths[1]);
                        if (indexFalse < 0)
                        {
                            segments.Add(segments[i].outwardPaths[1]);
                            indexFalse = segments.Count - 1;
                        }

                        tempTypeStack.Pop();
                        result += 
                            "\tif (__" + tempExcerptStack.Pop() + ") goto __segment" +
                            indexTrue + "; else goto __segment" + indexFalse + ";\n\n";
                    }
                    else throw new System.Exception("unimplemented");
                }

                result += "\t__segment_end:;\n";
                result += "}\n\n";
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
            var indirectionLevels = 0;
            
            for (int i = 2; i < node.ChildNumber(); i++)
            {
                if (node.Child(i).kind == Grammar.ASTNodeKind.Operator)
                {
                    result += "__Ptr__b";
                    indirectionLevels++;
                }
            }

            result += node.Child(0).GetExcerpt();
            result += ManglePattern(node.Child(1));
            for (int i = 0; i < indirectionLevels; i++)
                result += "e";
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
                else if (name == "Bool")
                    return "char";
                else if (name == "Int32")
                    return "int";
                else
                    return MangleTopDeclName(topDecl);
            }
            else if (type is Semantics.TypeFunct)
            {
                var fType = (Semantics.TypeFunct)type;
                var result = ConvertType(fType.returnType) + "(*)(";
                for (int i = 0; i < fType.argumentTypes.Count; i++)
                {
                    result += ConvertType(fType.argumentTypes[i]);
                    if (i < fType.argumentTypes.Count - 1)
                        result += ", ";
                }
                return result + ")";
            }
            else
                return "???";
        }


        private string ConvertTypeDecl(Semantics.Type type)
        {
            if (type is Semantics.TypePointer)
                return ConvertType(((Semantics.TypePointer)type).pointeeType) + "* #";
            else if (type is Semantics.TypeVoid)
                return "void #";
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
                    return "??? #";
                else if (name == "Bool")
                    return "char #";
                else if (name == "Int32")
                    return "int #";
                else
                    return MangleTopDeclName(topDecl) + " #";
            }
            else if (type is Semantics.TypeFunct)
            {
                var fType = (Semantics.TypeFunct)type;
                var result = ConvertType(fType.returnType) + "(*#)(";
                for (int i = 0; i < fType.argumentTypes.Count; i++)
                {
                    result += ConvertType(fType.argumentTypes[i]);
                    if (i < fType.argumentTypes.Count - 1)
                        result += ", ";
                }
                return result + ")";
            }
            else
                return "??? #";
        }
    }
}
