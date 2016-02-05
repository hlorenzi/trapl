namespace Trapl.Grammar
{
    public partial class CoreConverter
    {
        private class StructWorkData
        {
            public Core.Name name;
            public ASTNodeDeclStruct declNode;
            public int declIndex;
            public Core.UseDirective[] useDirectives;
        }


        public void ConvertStructFields()
        {
            foreach (var binding in this.structWorkData)
            {
                session.PushContext(
                    "in struct '" + binding.name.GetString() + "'",
                    binding.declNode.GetSpan());

                foreach (var fieldNode in binding.declNode.fields)
                {
                    var fieldName = ConvertName(fieldNode.name);
                    var fieldType = ConvertType(fieldNode.type, binding.useDirectives);
                    session.AddStructField(binding.declIndex, fieldName, fieldType);
                }

                session.PopContext();
            }
        }
    }
}
