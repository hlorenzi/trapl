using Trapl.Diagnostics;


namespace Trapl.Semantics
{
    public class CodeTypeChecker
    {
        public static void Check(Infrastructure.Session session, CodeBody body)
        {
            var checker = new CodeTypeChecker(session, body);
            checker.Check();
        }


        private Infrastructure.Session session;
        private CodeBody body;


        private CodeTypeChecker(Infrastructure.Session session, CodeBody body)
        {
            this.session = session;
            this.body = body;
        }


        private void Check()
        {
            this.CheckUnresolvedLocals();
            this.PerformCheck(CheckControlLet, this.body.code);
            this.PerformCheck(CheckAssignment, this.body.code);
            this.PerformCheck(CheckDereference, this.body.code);
            this.PerformCheck(CheckStructLiteralInitializers, this.body.code);
            this.PerformCheck(CheckFunctResolution, this.body.code);
            this.PerformCheck(CheckCallArguments, this.body.code);
        }


        private delegate void RuleDelegate(CodeNode code);


        private void PerformCheck(RuleDelegate rule, CodeNode node)
        {
            rule(node);
            foreach (var child in node.children)
                this.PerformCheck(rule, child);
        }


        private bool DoesMismatch(Type type1, Type type2)
        {
            if (type1 is TypePlaceholder ||
                type2 is TypePlaceholder ||
                type1 is TypeError ||
                type2 is TypeError)
                return false;

            return !type1.IsSame(type2);
        }


        private void CheckUnresolvedLocals()
        {
            foreach (var loc in this.body.localVariables)
            {
                if (!loc.type.IsResolved())
                {
                    session.diagn.Add(MessageKind.Error, MessageCode.InferenceFailed,
                        "cannot infer type for '" +
                        loc.GetString(this.session) + "'",
                        loc.declSpan);

                    loc.type = new TypeError();
                }
            }
        }

        private void CheckControlLet(CodeNode code)
        {
            var codeLet = code as CodeNodeControlLet;
            if (codeLet == null)
                return;

            if (codeLet.children.Count == 1 &&
                codeLet.localIndex >= 0 &&
                DoesMismatch(this.body.localVariables[codeLet.localIndex].type, codeLet.children[0].outputType))
            {
                session.diagn.Add(MessageKind.Error, MessageCode.IncompatibleTypes,
                    "assigning '" + codeLet.children[0].outputType.GetString(session) + "' " +
                    "to '" + this.body.localVariables[codeLet.localIndex].type.GetString(session) + "'",
                    codeLet.children[0].span,
                    this.body.localVariables[codeLet.localIndex].declSpan);
            }
        }


        private void CheckAssignment(CodeNode code)
        {
            var codeAssign = code as CodeNodeAssign;
            if (codeAssign == null)
                return;

            if (DoesMismatch(codeAssign.children[0].outputType, codeAssign.children[1].outputType))
            {
                session.diagn.Add(MessageKind.Error, MessageCode.IncompatibleTypes,
                    "assigning '" + codeAssign.children[1].outputType.GetString(session) + "' " +
                    "to '" + codeAssign.children[0].outputType.GetString(session) + "'",
                    codeAssign.children[0].span,
                    codeAssign.children[1].span);
            }
        }


        private void CheckDereference(CodeNode code)
        {
            var codeDereference = code as CodeNodeDereference;
            if (codeDereference == null)
                return;

            if (!(codeDereference.children[0].outputType is TypeReference) &&
                !codeDereference.children[0].outputType.IsError())
            {
                session.diagn.Add(MessageKind.Error, MessageCode.CannotDereference,
                    "cannot dereference '" +
                    codeDereference.children[0].outputType.GetString(session) + "'",
                    codeDereference.children[0].span);
            }
        }


        private void CheckStructLiteralInitializers(CodeNode code)
        {
            var codeStructLiteral = code as CodeNodeStructLiteral;
            if (codeStructLiteral == null)
                return;

            var structType = (TypeStruct)codeStructLiteral.outputType;

            for (int i = 0; i < codeStructLiteral.children.Count; i++)
            {
                if (DoesMismatch(structType.potentialStructs[0].fields[i].type, codeStructLiteral.children[i].outputType))
                {
                    session.diagn.Add(MessageKind.Error, MessageCode.IncompatibleTypes,
                        "'" + codeStructLiteral.children[i].outputType.GetString(this.session) +
                        "' initializer for '" + structType.potentialStructs[0].fields[i].type.GetString(this.session) +
                        "' field",
                        codeStructLiteral.children[i].span);
                }
            }
        }


        private void CheckFunctResolution(CodeNode code)
        {
            var codeFunct = code as CodeNodeFunct;
            if (codeFunct == null)
                return;

            if (codeFunct.potentialFuncts.Count > 1)
            {
                session.diagn.Add(MessageKind.Error, MessageCode.InferenceFailed,
                    "cannot infer which '" + PathASTUtil.GetString(codeFunct.nameInference.pathASTNode) +
                    "' declaration to use",
                    codeFunct.span);
                session.diagn.AddInnerToLast(MessageKind.Info, MessageCode.Info,
                    "ambiguous between the following declarations" +
                    (codeFunct.potentialFuncts.Count > 2 ? " and other " + (codeFunct.potentialFuncts.Count - 2) : ""),
                    codeFunct.potentialFuncts[0].nameASTNode.Span(),
                    codeFunct.potentialFuncts[1].nameASTNode.Span());
            }
            else if (codeFunct.potentialFuncts.Count == 0)
            {
                if (codeFunct.nameInference.template.IsFullyResolved())
                {
                    session.diagn.Add(MessageKind.Error, MessageCode.UndeclaredTemplate,
                        "no '" + PathASTUtil.GetString(codeFunct.nameInference.pathASTNode) +
                        "' declaration accepts this template",
                        codeFunct.span);
                }
                else
                {
                    session.diagn.Add(MessageKind.Error, MessageCode.InferenceFailed,
                        "cannot infer which '" + PathASTUtil.GetString(codeFunct.nameInference.pathASTNode) +
                        "' declaration to use",
                        codeFunct.span);
                }
            }
        }


        private void CheckCallArguments(CodeNode code)
        {
            var codeCall = code as CodeNodeCall;
            if (codeCall == null)
                return;

            var functType = codeCall.children[0].outputType as TypeFunct;

            if (functType == null)
            {
                if (codeCall.children[0].outputType.IsResolved() &&
                    !codeCall.children[0].outputType.IsError())
                {
                    session.diagn.Add(MessageKind.Error, MessageCode.InferenceFailed,
                        "'" + codeCall.children[0].outputType.GetString(this.session) + "' " +
                        "is not callable",
                        codeCall.children[0].span);
                }

                return;
            }

            for (var i = 0; i < codeCall.children.Count - 1; i++)
            {
                if (functType.argumentTypes[i].IsResolved() &&
                    DoesMismatch(functType.argumentTypes[i], codeCall.children[i + 1].outputType))
                {
                    session.diagn.Add(MessageKind.Error, MessageCode.InferenceFailed,
                        "passing '" + codeCall.children[i + 1].outputType.GetString(this.session) +
                        "' to '" + functType.argumentTypes[i].GetString(this.session) + "' argument",
                        codeCall.children[i + 1].span);
                }
            }
        }
    }
}
