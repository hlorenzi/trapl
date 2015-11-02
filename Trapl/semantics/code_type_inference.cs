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
                InferTypeLocal,
                InferTypeAssignment,
                InferTypeAddress,
                InferTypeDereference,
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


        private void TryInference(Type typeFrom, ref Type typeTo)
        {
            if (typeTo is TypePlaceholder && !(typeFrom is TypePlaceholder))
            {
                this.appliedAnyRule = true;
                typeTo = typeFrom;
            }
            else if (typeTo is TypeReference && typeFrom is TypeReference)
            {
                var refTo = (TypeReference)typeTo;
                var refFrom = (TypeReference)typeFrom;
                TryInference(refFrom.referencedType, ref refTo.referencedType);
                TryInference(refTo.referencedType, ref refFrom.referencedType);
            }
            else if (typeTo is TypeStruct && typeFrom is TypeStruct)
            {
                var structTo = (TypeStruct)typeTo;
                var structFrom = (TypeStruct)typeFrom;

                if (structTo.potentialStructs.Count > 1 && structFrom.potentialStructs.Count == 1)
                {
                    TryTemplateInference(structFrom.nameInference.template, ref structTo.nameInference.template);
                    typeTo = typeFrom;
                }
            }
            else if (typeTo is TypeFunct && typeFrom is TypeFunct)
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
            else if (typeTo is TypeTuple && typeFrom is TypeTuple)
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


        private void TryTemplateInference(Template fromTemplate, ref Template toTemplate)
        {
            if (toTemplate.unconstrained)
            {
                if (!fromTemplate.unconstrained)
                {
                    toTemplate = fromTemplate;
                    this.appliedAnyRule = true;
                }
            }
            else if (toTemplate.parameters.Count == fromTemplate.parameters.Count)
            {
                for (int i = 0; i < toTemplate.parameters.Count; i++)
                {
                    if (toTemplate.parameters[i] is Template.ParameterType &&
                        fromTemplate.parameters[i] is Template.ParameterType)
                    {
                        var param = (Template.ParameterType)toTemplate.parameters[i];
                        TryInference(((Template.ParameterType)fromTemplate.parameters[i]).type, ref param.type);
                        toTemplate.parameters[i] = param;
                    }
                }
            }
        }


        private void InferTypeLocal(CodeNode code)
        {
            var codeLocalValue = code as CodeNodeLocal;
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


        private void InferTypeAddress(CodeNode code)
        {
            var codeReference = code as CodeNodeAddress;
            if (codeReference == null)
                return;

            var outputRefType = (TypeReference)codeReference.outputType;
            TryInference(codeReference.children[0].outputType, ref outputRefType.referencedType);
            TryInference(outputRefType.referencedType, ref codeReference.children[0].outputType);
        }


        private void InferTypeDereference(CodeNode code)
        {
            var codeDereference = code as CodeNodeDereference;
            if (codeDereference == null)
                return;

            if (codeDereference.children[0].outputType is TypePlaceholder)
                codeDereference.children[0].outputType = new TypeReference(new TypePlaceholder());

            var childRefType = (codeDereference.children[0].outputType as TypeReference);
            if (childRefType != null)
            {
                TryInference(childRefType.referencedType, ref codeDereference.outputType);
                TryInference(codeDereference.outputType, ref childRefType.referencedType);
            }
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
                    if (!def.template.IsMatch(codeFunct.nameInference.template))
                    {
                        codeFunct.potentialFuncts.RemoveAt(i);
                        this.appliedAnyRule = true;
                    }
                }

                var functType = codeFunct.outputType as TypeFunct;
                if (functType != null)
                {
                    // Disregard functs whose return types don't match.
                    for (int i = codeFunct.potentialFuncts.Count - 1; i >= 0; i--)
                    {
                        var def = codeFunct.potentialFuncts[i];
                        if (!def.returnType.IsMatch(functType.returnType))
                        {
                            codeFunct.potentialFuncts.RemoveAt(i);
                            this.appliedAnyRule = true;
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
                            if (!def.arguments[arg].type.IsMatch(functType.argumentTypes[arg]))
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
            else if (!codeCall.children[0].outputType.IsResolved())
            {
                functType = new TypeFunct();
                functType.returnType = new TypePlaceholder();
                for (var i = 0; i < codeCall.children.Count - 1; i++)
                    functType.argumentTypes.Add(new TypePlaceholder());
                codeCall.children[0].outputType = functType;
                this.appliedAnyRule = true;
            }
        }
    }
}
