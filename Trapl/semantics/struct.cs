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

                var struc = session.GetStruct(binding.declIndex);

                foreach (var fieldNode in binding.declNode.fields)
                {
                    var fieldName = NameResolver.Resolve(fieldNode.name);
                    var fieldType = TypeResolver.Resolve(session, fieldNode.type, binding.useDirectives, true);

                    int duplicateFieldIndex;
                    if (struc.fieldNames.FindByName(fieldName, out duplicateFieldIndex))
                    {
                        session.AddMessage(
                            Diagnostics.MessageKind.Error,
                            Diagnostics.MessageCode.DuplicateDeclaration,
                            "duplicate field '" + fieldName.GetString() + "'",
                            fieldNode.name.GetSpan(),
                            struc.GetFieldNameSpan(duplicateFieldIndex));
                    }
                    else
                    {
                        struc.AddField(fieldName, fieldType, fieldNode);
                    }
                }

                session.PopContext();
            }
        }
    }
}
