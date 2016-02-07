namespace Trapl.Semantics
{
    public partial class DeclResolver
    {
        private class StructWorkData
        {
            public Core.Name name;
            public Grammar.ASTNodeDeclStruct declNode;
            public int declIndex;
            public Core.UseDirective[] useDirectives;
        }


        public void ResolveStructFields()
        {
            foreach (var binding in this.structWorkData)
            {
                session.PushContext(
                    "in struct '" + binding.name.GetString() + "'",
                    binding.declNode.GetSpan());

                foreach (var fieldNode in binding.declNode.fields)
                {
                    var fieldName = NameResolver.Resolve(fieldNode.name);
                    var fieldType = TypeResolver.Resolve(session, fieldNode.type, binding.useDirectives);
                    session.AddStructField(binding.declIndex, fieldName, fieldType);
                }

                session.PopContext();
            }
        }
    }
}
