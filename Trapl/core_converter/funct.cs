namespace Trapl.Grammar
{
    public partial class CoreConverter
    {
        private class FunctWorkData
        {
            public Core.Name name;
            public ASTNodeDeclFunct declNode;
            public int declIndex;
            public Core.UseDirective[] useDirectives;
        }


        public void ConvertFunctHeaders()
        {
            foreach (var binding in this.functWorkData)
            {
                session.PushContext(
                    "in funct '" + binding.name.GetString() + "'",
                    binding.declNode.GetSpan());

                session.CreateFunctRegister(binding.declIndex,
                    ConvertType(binding.declNode.returnType, binding.useDirectives));

                foreach (var paramNode in binding.declNode.parameters)
                {
                    var paramName = ConvertName(paramNode.name);
                    var paramType = ConvertType(paramNode.type, binding.useDirectives);
                    var paramReg = session.CreateFunctRegister(binding.declIndex, paramType);
                    session.CreateFunctBinding(binding.declIndex, paramName, paramReg);
                }

                session.SetFunctParameterNumber(binding.declIndex, binding.declNode.parameters.Count);

                session.PopContext();
            }
        }


        public void ConvertFunctBodies()
        {
            foreach (var binding in this.functWorkData)
            {
                session.PushContext(
                    "in funct '" + binding.name.GetString() + "'",
                    binding.declNode.GetSpan());

                var bodyConverter = new FunctBodyConverter(session, binding.declIndex, binding.useDirectives);
                bodyConverter.Convert(binding.declNode.bodyExpression);

                session.PopContext();
            }
        }
    }
}
