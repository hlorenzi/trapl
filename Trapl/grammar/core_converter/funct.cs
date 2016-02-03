namespace Trapl.Grammar
{
    public partial class CoreConverter
    {
        public void ConvertFunctHeaders()
        {
            foreach (var binding in this.functBindings)
            {
                session.PushContext(
                    "in funct '" + binding.name.GetString() + "'",
                    binding.declNode.GetSpan());

                foreach (var paramNode in binding.declNode.parameters)
                {
                    var paramName = ConvertName(paramNode.name);
                    var paramType = ConvertType(paramNode.type, binding.useDirectives);
                    session.AddFunctParameter(binding.declIndex, paramName, paramType);
                }

                session.SetFunctReturnType(binding.declIndex,
                    ConvertType(binding.declNode.returnType, binding.useDirectives));

                session.PopContext();
            }
        }


        public void ConvertFunctBodies()
        {
            foreach (var binding in this.functBindings)
            {
                session.PushContext(
                    "in funct '" + binding.name.GetString() + "'",
                    binding.declNode.GetSpan());

                // Todo...

                session.PopContext();
            }
        }
    }
}
