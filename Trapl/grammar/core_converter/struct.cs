using System.Collections.Generic;


namespace Trapl.Grammar
{
    public partial class CoreConverter
    {
        private class StructBinding
        {
            public ASTNodeDeclStruct declNode;
            public int declIndex;
        }


        public static void ConvertStructs(Core.Session session, ASTNode topASTNode)
        {
            var bindings = new List<StructBinding>();
            ConvertStructsInner(session, topASTNode, Core.Name.FromPath(), bindings);

            foreach (var binding in bindings)
            {
                session.PushContext(
                    "in struct '" + ConvertName(binding.declNode.name).GetString() + "'",
                    binding.declNode.GetSpan());

                foreach (var fieldNode in binding.declNode.fields)
                {
                    var fieldName = ConvertName(fieldNode.name);
                    var fieldType = ConvertType(session, fieldNode.type);
                    session.AddStructField(binding.declIndex, fieldName, fieldType);
                }
                session.PopContext();
            }
        }


        private static void ConvertStructsInner(
            Core.Session session,
            ASTNode topASTNode,
            Core.Name curNamespace,
            List<StructBinding> bindings)
        {
            foreach (var node in topASTNode.EnumerateChildren())
            {
                var namespaceNode = node as ASTNodeDeclNamespace;
                if (namespaceNode != null)
                {
                    var innerNamespace = curNamespace.Concatenate(ConvertName(namespaceNode.path));
                    ConvertStructsInner(session, namespaceNode, innerNamespace, bindings);
                    continue;
                }

                var declStructNode = node as ASTNodeDeclStruct;
                if (declStructNode != null)
                {
                    var name = curNamespace.Concatenate(ConvertName(declStructNode.name));
                    var declStructIndex = session.CreateStruct(name);

                    bindings.Add(new StructBinding
                    {
                        declNode = declStructNode,
                        declIndex = declStructIndex
                    });
                    continue;
                }
            }
        }
    }
}
