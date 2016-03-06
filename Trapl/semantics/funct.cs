namespace Trapl.Semantics
{
    public partial class DeclResolver
    {
        private class FunctWorkData
        {
            public Core.Name name;
            public Grammar.ASTNodeDeclFunct declNode;
            public int declIndex;
            public Core.UseDirective[] useDirectives;
        }


        public void ResolveFunctHeaders()
        {
            foreach (var binding in this.functWorkData)
            {
                session.PushContext(
                    "in funct '" + binding.name.GetString() + "'",
                    binding.declNode.GetSpan());

                var funct = session.GetFunct(binding.declIndex);

                funct.CreateRegister(
                    TypeResolver.Resolve(session, binding.declNode.returnType, binding.useDirectives, true));

                foreach (var paramNode in binding.declNode.parameters)
                {
                    var paramName = NameResolver.Resolve(paramNode.name);
                    var paramType = TypeResolver.Resolve(session, paramNode.type, binding.useDirectives, true);
                    var paramReg = funct.CreateRegister(paramType);
                    funct.CreateBinding(paramName, paramReg, paramNode.name.GetSpan());
                }

                funct.SetParameterNumber(binding.declNode.parameters.Count);

                session.PopContext();
            }
        }


        public void ResolveFunctBodies()
        {
            foreach (var binding in this.functWorkData)
            {
                session.PushContext(
                    "in funct '" + binding.name.GetString() + "'",
                    binding.declNode.GetSpan());

                var funct = session.GetFunct(binding.declIndex);

                var foundErrors = FunctBodyResolver.Resolve(
                    this.session, funct, binding.useDirectives, binding.declNode.bodyExpression);

                if (!foundErrors)
                    FunctTypeInferencer.DoInference(this.session, funct);

                if (!foundErrors)
                    foundErrors = FunctTypeChecker.Check(this.session, funct);

                if (!foundErrors)
                    foundErrors = FunctInitChecker.Check(this.session, funct);

                session.PopContext();
            }
        }
    }
}
