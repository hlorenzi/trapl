using Trapl.Diagnostics;
using System.Collections.Generic;


namespace Trapl.Semantics
{
    public class CodeTypeInferenceAnalyzer
    {
        public static void Analyze(Infrastructure.Session session, CodeBody body)
        {
            var analyzer = new CodeTypeInferenceAnalyzer(session, body);
            analyzer.ApplyAllRules();
        }


        private Infrastructure.Session session;
        private CodeBody body;

        private bool appliedAnyRule = false;


        private CodeTypeInferenceAnalyzer(Infrastructure.Session session, CodeBody body)
        {
            this.session = session;
            this.body = body;
        }


        private void ApplyAllRules()
        {
            var ruleList = new List<RuleDelegate>
            {
                InferTypeLocalAddress,
                InferTypeLocalValue,
                InferTypeAssignment,
                InferFunctTemplate,
                InferCall
            };

            this.appliedAnyRule = false;
            this.DoFromInnerToOuter(this.body.code, ruleList);
        }


        private delegate void RuleDelegate(CodeNode code);


        private void DoFromInnerToOuter(CodeNode node, List<RuleDelegate> ruleList)
        {
            do
            {
                foreach (var child in node.children)
                {
                    this.DoFromInnerToOuter(child, ruleList);
                }

                this.appliedAnyRule = false;
                foreach (var rule in ruleList)
                    rule(node);
            }
            while (this.appliedAnyRule);
        }


        private bool IsResolved(Type type)
        {
            return !(type is TypeUnconstrained);
        }


        private void TryInference(Type typeFrom, ref Type typeTo)
        {
            if (typeTo is TypeUnconstrained &&
                !(typeFrom is TypeUnconstrained))
            {
                this.appliedAnyRule = true;
                typeTo = typeFrom;
            }
            else if (typeTo is TypeFunct &&
                typeFrom is TypeFunct)
            {
                var functTo = (TypeFunct)typeTo;
                var functFrom = (TypeFunct)typeFrom;
                TryInference(functFrom.returnType, ref functTo.returnType);

                if (functTo.argumentTypes.Count == functFrom.argumentTypes.Count)
                {
                    for (var i = 0; i < functTo.argumentTypes.Count; i++)
                    {
                        var argTo = functTo.argumentTypes[i];
                        TryInference(functFrom.argumentTypes[i], ref argTo);
                        functTo.argumentTypes[i] = argTo;
                    }
                }

                typeTo = functTo;
            }
            else if (typeTo is TypeTuple &&
                typeFrom is TypeTuple)
            {
                var tupleTo = (TypeTuple)typeTo;
                var tupleFrom = (TypeTuple)typeFrom;

                if (tupleTo.elementTypes.Count == tupleFrom.elementTypes.Count)
                {
                    for (var i = 0; i < tupleTo.elementTypes.Count; i++)
                    {
                        var elemTo = tupleTo.elementTypes[i];
                        TryInference(tupleFrom.elementTypes[i], ref elemTo);
                        tupleTo.elementTypes[i] = elemTo;
                    }
                }

                typeTo = tupleTo;
            }
        }


        private void InferTypeLocalAddress(CodeNode code)
        {
            var codeLocalAddr = code as CodeNodeLocalAddress;
            if (codeLocalAddr == null)
                return;

            if (codeLocalAddr.localIndex >= 0)
            {
                TryInference(this.body.localVariables[codeLocalAddr.localIndex].type, ref codeLocalAddr.outputType);
                TryInference(codeLocalAddr.outputType, ref this.body.localVariables[codeLocalAddr.localIndex].type);
            }
        }


        private void InferTypeLocalValue(CodeNode code)
        {
            var codeLocalValue = code as CodeNodeLocalValue;
            if (codeLocalValue == null)
                return;

            if (codeLocalValue.localIndex >= 0)
            {
                TryInference(this.body.localVariables[codeLocalValue.localIndex].type, ref codeLocalValue.outputType);
                TryInference(codeLocalValue.outputType, ref this.body.localVariables[codeLocalValue.localIndex].type);
            }
        }


        private void InferTypeAssignment(CodeNode code)
        {
            var codeAssign = code as CodeNodeAssign;
            if (codeAssign == null)
                return;

            TryInference(codeAssign.children[1].outputType, ref codeAssign.children[0].outputType);
            TryInference(codeAssign.children[0].outputType, ref codeAssign.children[1].outputType);
        }


        private void InferFunctTemplate(CodeNode code)
        {
            var codeFunct = code as CodeNodeFunct;
            if (codeFunct == null)
                return;

            if (codeFunct.potentialFuncts.Count > 1)
            {
                // Disregard functs whose templates don't match.
                for (int i = codeFunct.potentialFuncts.Count - 1; i >= 0; i--)
                {
                    var def = codeFunct.potentialFuncts[i];
                    if (!def.topDecl.template.IsMatch(codeFunct.nameInference.template))
                    {
                        codeFunct.potentialFuncts.RemoveAt(i);
                        this.appliedAnyRule = true;
                    }
                }

                var functType = codeFunct.outputType as TypeFunct;
                if (functType != null)
                {
                    // Disregard functs whose return types don't match.
                    if (IsResolved(functType.returnType))
                    {
                        for (int i = codeFunct.potentialFuncts.Count - 1; i >= 0; i--)
                        {
                            var def = codeFunct.potentialFuncts[i];
                            if (!def.returnType.IsSame(functType.returnType))
                            {
                                codeFunct.potentialFuncts.RemoveAt(i);
                                this.appliedAnyRule = true;
                            }
                        }
                    }

                    // Disregard functs whose argument numbers don't match.
                    for (int i = codeFunct.potentialFuncts.Count - 1; i >= 0; i--)
                    {
                        var def = codeFunct.potentialFuncts[i];
                        if (def.arguments.Count != functType.argumentTypes.Count)
                        {
                            codeFunct.potentialFuncts.RemoveAt(i);
                            this.appliedAnyRule = true;
                        }
                    }

                    // Disregard functs whose argument types don't match.
                    for (int i = codeFunct.potentialFuncts.Count - 1; i >= 0; i--)
                    {
                        var def = codeFunct.potentialFuncts[i];
                        for (int arg = 0; arg < functType.argumentTypes.Count; arg++)
                        {
                            if (IsResolved(functType.argumentTypes[arg]) &&
                                !def.arguments[arg].type.IsSame(functType.argumentTypes[arg]))
                            {
                                codeFunct.potentialFuncts.RemoveAt(i);
                                this.appliedAnyRule = true;
                                break;
                            }
                        }
                    }
                }
            }
            else if (codeFunct.potentialFuncts.Count == 1)
            {
                TryInference(new TypeFunct(codeFunct.potentialFuncts[0]), ref codeFunct.outputType);
            }
        }


        private void InferCall(CodeNode code)
        {
            var codeCall = code as CodeNodeCall;
            if (codeCall == null)
                return;

            var functType = (codeCall.children[0].outputType as TypeFunct);
            if (functType != null)
            {
                TryInference(functType.returnType, ref codeCall.outputType);
                TryInference(codeCall.outputType, ref functType.returnType);

                if (functType.argumentTypes.Count == codeCall.children.Count - 1)
                {
                    for (var i = 0; i < functType.argumentTypes.Count; i++)
                    {
                        var codeCallArg = codeCall.children[i + 1].outputType;
                        TryInference(functType.argumentTypes[i], ref codeCallArg);
                        codeCall.children[i + 1].outputType = codeCallArg;

                        var functArg = functType.argumentTypes[i];
                        TryInference(codeCall.children[i + 1].outputType, ref functArg);
                        functType.argumentTypes[i] = functArg;
                    }
                }
            }
            else
            {
                functType = new TypeFunct();
                functType.returnType = new TypeUnconstrained();
                for (var i = 0; i < codeCall.children.Count - 1; i++)
                    functType.argumentTypes.Add(new TypeUnconstrained());
                codeCall.children[0].outputType = functType;
                this.appliedAnyRule = true;
            }
        }
    }
}
