using System.Collections.Generic;


namespace Trapl.Codegen
{
    public class CGenerator
    {
        public static string Generate(Core.Session session)
        {
            var gen = new CGenerator();
            gen.session = session;

            gen.GatherItems();
            gen.RegisterStructDependencies();
            gen.RegisterFunctDependencies();
            var sortedItems = gen.itemGraph.GetTopologicalSort();

            var result = "";

            for (var i = 0; i < sortedItems.Count; i++)
            {
                var item = sortedItems[i];
                switch (item.kind)
                {
                    case Item.Kind.StructHeader:
                        result += gen.GenerateStructDecl(item.index); break;
                    case Item.Kind.Struct:
                        result += gen.GenerateStructDef(item.index); break;
                    case Item.Kind.FunctHeader:
                        result += gen.GenerateFunctDecl(item.index); break;
                    case Item.Kind.Funct:
                        result += gen.GenerateFunctDef(item.index); break;
                }
            }

            /*result += gen.GenerateFunctDecls() + "\n";
            result += gen.GenerateFunctDefs() + "\n";*/
            return result;
        }


        private Core.Session session;
        private Util.Graph<Item> itemGraph = new Util.Graph<Item>();
        private List<Item> itemStructs = new List<Item>();
        private List<Item> itemStructHeaders = new List<Item>();
        private List<Item> itemFuncts = new List<Item>();
        private List<Item> itemFunctHeaders = new List<Item>();


        private class Item
        {
            public enum Kind { Struct, StructHeader, Funct, FunctHeader };

            public Kind kind;
            public int index;
        }


        private void GatherItems()
        {
            var structs = session.GetStructs();
            for (var i = 0; i < structs.Count; i++)
            {
                var st = new Item { kind = Item.Kind.Struct, index = i };
                var sth = new Item { kind = Item.Kind.StructHeader, index = i };
                itemGraph.AddNode(st);
                itemGraph.AddNode(sth);
                itemStructs.Add(st);
                itemStructHeaders.Add(sth);
            }

            var functs = session.GetFuncts();
            for (var i = 0; i < functs.Count; i++)
            {
                var fn = new Item { kind = Item.Kind.Funct, index = i };
                var fnh = new Item { kind = Item.Kind.FunctHeader, index = i };
                itemGraph.AddNode(fn);
                itemGraph.AddNode(fnh);
                itemFuncts.Add(fn);
                itemFunctHeaders.Add(fnh);
            }
        }


        private void RegisterStructDependencies()
        {
            var structs = session.GetStructs();
            for (var i = 0; i < structs.Count; i++)
            {
                foreach (var fieldType in structs[i].fieldTypes)
                    AddDependencyFromType(itemStructs[i], fieldType, false);
            }
        }


        private void RegisterFunctDependencies()
        {
            var functs = session.GetFuncts();
            for (var i = 0; i < functs.Count; i++)
            {
                for (var p = 0; p < functs[i].registerTypes.Count; p++)
                {
                    AddDependencyFromType(itemFuncts[i], functs[i].registerTypes[p], false);
                    if (p < functs[i].parameterNum + 1)
                        AddDependencyFromType(itemFunctHeaders[i], functs[i].registerTypes[p], false);
                }
            }
        }


        private void AddDependencyFromType(Item forItem, Core.Type type, bool onlyHeader)
        {
            var typeStruct = type as Core.TypeStruct;
            if (typeStruct != null)
            {
                itemGraph.AddEdge(
                    forItem,
                    (onlyHeader ?
                        itemStructHeaders[typeStruct.structIndex] :
                        itemStructs[typeStruct.structIndex]));
                return;
            }

            var typePtr = type as Core.TypePointer;
            if (typePtr != null)
            {
                AddDependencyFromType(forItem, typePtr.pointedToType, true);
                return;
            }

            var typeTuple = type as Core.TypeTuple;
            if (typeTuple != null)
            {
                foreach (var elem in typeTuple.elementTypes)
                    AddDependencyFromType(forItem, elem, onlyHeader);
                return;
            }
        }


        private string GenerateStructDecl(int structIndex)
        {
            var st = session.GetStruct(structIndex);

            var result = "struct " + MangleName(session.GetStructName(structIndex)) + ";\n\n";

            return result;
        }


        private string GenerateStructDef(int structIndex)
        {
            var st = session.GetStruct(structIndex);

            var result = "struct " + MangleName(session.GetStructName(structIndex));

            if (st.fieldTypes.Count == 0)
                result += " { ";
            else
                result += "\n{\n";

            for (var i = 0; i < st.fieldTypes.Count; i++)
            {
                Core.Name name;
                st.fieldNames.FindByValue(i, out name);

                result += "\t" + ConvertFieldDecl(st.fieldTypes[i], name.GetString()) + ";\n";
            }
            result += "}\n\n";

            return result;
        }


        private string GenerateFunctHeader(int functIndex)
        {
            var result = "";

            var fn = session.GetFunct(functIndex);

            result += MangleName(session.GetFunctName(functIndex)) + "(";

            if (fn.parameterNum > 0)
                result += "\n";

            for (int i = 0; i < fn.parameterNum; i++)
            {
                result += "\t" +
                    ConvertFieldDecl(fn.registerTypes[i + 1], "var" + (i + 1));

                if (i < fn.parameterNum - 1)
                    result += ",\n";
            }

            result += ")";
            result = ConvertFieldDecl(fn.GetReturnType(), result);

            return result;
        }


        private string GenerateFunctDecl(int functIndex)
        {
            return GenerateFunctHeader(functIndex) + ";\n\n";
        }


        private string GenerateFunctDef(int functIndex)
        {
            var result = GenerateFunctHeader(functIndex) + "\n{\n";

            var fn = this.session.GetFunct(functIndex);
            for (var i = 0; i < fn.registerTypes.Count; i++)
            {
                result += "\t" +
                    ConvertFieldDecl(fn.registerTypes[i], "var" + i) + ";\n";
            }

            result += "\n";

            for (var i = 0; i < fn.segments.Count; i++)
            {
                result += "\tseg" + i + ":\n";
                foreach (var inst in fn.segments[i].instructions)
                {
                    var instMoveInt = inst as Core.InstructionMoveLiteralInt;
                    var instMoveBool = inst as Core.InstructionMoveLiteralBool;
                    var instMoveFunct = inst as Core.InstructionMoveLiteralFunct;
                    var instMoveStruct = inst as Core.InstructionMoveLiteralStruct;
                    var instMoveTuple = inst as Core.InstructionMoveLiteralTuple;
                    var instMoveData = inst as Core.InstructionMoveData;
                    var instMoveAddr = inst as Core.InstructionMoveAddr;
                    var instMoveCall = inst as Core.InstructionMoveCallResult;
                    var instDeinit = inst as Core.InstructionDeinit;

                    if (instMoveInt != null)
                    {
                        result += "\t" +
                            GenerateDataAccess(instMoveInt.destination, fn) +
                            " = " + instMoveInt.value + ";\n";
                    }

                    else if (instMoveBool != null)
                    {
                        result += "\t" +
                            GenerateDataAccess(instMoveBool.destination, fn) +
                            " = " + (instMoveBool.value ? "1; /* true */\n" : "0; /* false */\n");
                    }

                    else if (instMoveFunct != null)
                    {
                        result += "\t" +
                            GenerateDataAccess(instMoveFunct.destination, fn) +
                            " = " + MangleName(this.session.GetFunctName(instMoveFunct.functIndex)) +
                            ";\n";
                    }

                    else if (instMoveStruct != null)
                    {
                        var structType =
                            Semantics.TypeResolver.GetDataAccessType(this.session, fn, instMoveStruct.destination);

                        for (var p = 0; p < instMoveStruct.fieldSources.Length; p++)
                        {
                            result += "\t(" +
                                GenerateDataAccess(instMoveStruct.destination, fn) +
                                ")." +
                                Semantics.TypeResolver.GetFieldName(this.session, structType, p).GetString() +
                                " = " +
                                GenerateDataAccess(instMoveStruct.fieldSources[p], fn) +
                                ";\n";
                        }
                    }

                    else if (instMoveTuple != null)
                    {
                        for (var p = 0; p < instMoveTuple.sourceElements.Length; p++)
                        {
                            result += "\t(" +
                                GenerateDataAccess(instMoveTuple.destination, fn) +
                                ").elem" + p + " = " +
                                GenerateDataAccess(instMoveTuple.sourceElements[p], fn) +
                                ";\n";
                        }
                    }

                    else if (instMoveData != null)
                    {
                        result += "\t" +
                            GenerateDataAccess(instMoveData.destination, fn) +
                            " = " + GenerateDataAccess(instMoveData.source, fn) + ";\n";
                    }

                    else if (instMoveAddr != null)
                    {
                        result += "\t" +
                            GenerateDataAccess(instMoveAddr.destination, fn) +
                            " = &(" + GenerateDataAccess(instMoveAddr.source, fn) + ");\n";
                    }

                    else if (instMoveCall != null)
                    {
                        result += "\t" +
                            GenerateDataAccess(instMoveCall.destination, fn) +
                            " = " + GenerateDataAccess(instMoveCall.callTargetSource, fn) +
                            "(";

                        for (var p = 0; p < instMoveCall.argumentSources.Length; p++)
                        {
                            result += GenerateDataAccess(instMoveCall.argumentSources[p], fn);
                            if (p < instMoveCall.argumentSources.Length - 1)
                                result += ", ";
                        }

                        result += ");\n";
                    }

                    else if (instDeinit != null)
                    {

                    }

                    else
                        result += "\t/* inst */\n";
                }

                var flowReturn = fn.segments[i].outFlow as Core.SegmentFlowEnd;
                var flowGoto = fn.segments[i].outFlow as Core.SegmentFlowGoto;
                var flowBranch = fn.segments[i].outFlow as Core.SegmentFlowBranch;

                if (flowReturn != null)
                {
                    result += "\treturn var0;\n";
                }

                else if (flowGoto != null)
                {
                    result += "\tgoto seg" + flowGoto.destinationSegment + ";\n";
                }

                else if (flowBranch != null)
                {
                    result += "\tif (" +
                        GenerateDataAccess(flowBranch.conditionReg, fn) +
                        ") { goto seg" + flowBranch.destinationSegmentIfTaken + "; } " +
                        "else { goto seg" + flowBranch.destinationSegmentIfNotTaken + "; }\n";
                }

                else
                    result += "\t/* flow */\n";

                if (i < fn.segments.Count - 1)
                    result += "\n";
            }

            result += "}\n\n";
            return result;
        }


        private string GenerateDataAccess(Core.DataAccess access, Core.DeclFunct funct)
        {
            var accessReg = access as Core.DataAccessRegister;
            if (accessReg != null)
                return "var" + accessReg.registerIndex;

            var accessDeref = access as Core.DataAccessDereference;
            if (accessDeref != null)
                return "*(" + GenerateDataAccess(accessDeref.innerAccess, funct) + ")";

            var accessField = access as Core.DataAccessField;
            if (accessField != null)
            {
                var baseType =
                    Semantics.TypeResolver.GetDataAccessType(this.session, funct, accessField.baseAccess);
                
                var fieldName =
                    Semantics.TypeResolver.GetFieldName(this.session, baseType, accessField.fieldIndex);

                return "(" + GenerateDataAccess(accessField.baseAccess, funct) +
                    ")." + fieldName.GetString();
            }

            return "??";
        }


        private string MangleName(Core.Name name)
        {
            return name.GetString().Replace("::", "_");
        }


        private string ConvertFieldDecl(Core.Type type, string name)
        {
            var typeStruct = type as Core.TypeStruct;
            if (typeStruct != null)
            {
                var structName = MangleName(session.GetStructName(typeStruct.structIndex));
                return structName + (name == null ? "" : " " + name);
            }

            var typePointer = type as Core.TypePointer;
            if (typePointer != null)
            {
                return ConvertFieldDecl(typePointer.pointedToType, "*" + name);
            }

            var typeTuple = type as Core.TypeTuple;
            if (typeTuple != null)
            {
                if (typeTuple.IsEmptyTuple())
                    return "void" + (name == null ? "" : " " + name);
            }

            var typeFunct = type as Core.TypeFunct;
            if (typeFunct != null)
            {
                var res =
                    ConvertFieldDecl(typeFunct.returnType, null)
                    + "(*" + (name == null ? "" : name) + ")"
                    + "(";

                for (var i = 0; i < typeFunct.parameterTypes.Length; i++)
                {
                    res += ConvertFieldDecl(typeFunct.parameterTypes[i], null);
                    if (i < typeFunct.parameterTypes.Length - 1)
                        res += ", ";
                }

                return res + ")";
            }

            return "??? " + name;
        }
    }
}
