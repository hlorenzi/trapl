using System.Collections.Generic;


namespace Trapl.Grammar
{
    public partial class CoreConverter
    {
        private class Binding<T> where T : ASTNode
        {
            public Core.Name name;
            public T declNode;
            public int declIndex;
            public Core.UseDirective[] useDirectives;
        }


        private List<Binding<ASTNodeDeclStruct>> structBindings = new List<Binding<ASTNodeDeclStruct>>();
        private List<Binding<ASTNodeDeclFunct>> functBindings = new List<Binding<ASTNodeDeclFunct>>();


        public void ConvertBindings(ASTNodeTopLevel topLevelNode)
        {
            this.ConvertBindingList(topLevelNode.decls, Core.Name.FromPath(), new List<Core.UseDirective>());
        }


        private void ConvertBindingList(List<ASTNode> declNodes, Core.Name curNamespace, List<Core.UseDirective> useDirectives)
        {
            var useDirectiveCountBefore = useDirectives.Count;

            foreach (var node in declNodes)
            {
                var useNode = node as ASTNodeUse;
                if (useNode != null)
                {
                    useDirectives.Add(ConvertUseDirective(useNode));
                    continue;
                }

                var namespaceNode = node as ASTNodeDeclNamespace;
                if (namespaceNode != null)
                {
                    this.ConvertNamespaceBinding(namespaceNode, curNamespace, useDirectives);
                    continue;
                }

                var structNode = node as ASTNodeDeclStruct;
                if (structNode != null)
                {
                    this.ConvertStructBinding(structNode, curNamespace, useDirectives);
                    continue;
                }
            }

            while (useDirectives.Count > useDirectiveCountBefore)
                useDirectives.RemoveAt(useDirectives.Count - 1);
        }


        private void ConvertNamespaceBinding(ASTNodeDeclNamespace namespaceNode, Core.Name curNamespace, List<Core.UseDirective> useDirectives)
        {
            var innerNamespace = curNamespace.Concatenate(this.ConvertName(namespaceNode.path));

            for (var i = 0; i < namespaceNode.path.identifiers.Count; i++)
                useDirectives.Add(new Core.UseDirectiveAll { name = curNamespace.ConcatenateIdentifier(namespaceNode.path.identifiers[i].GetExcerpt()) });

            this.ConvertBindingList(namespaceNode.innerDecls, innerNamespace, useDirectives);

            for (var i = 0; i < namespaceNode.path.identifiers.Count; i++)
                useDirectives.RemoveAt(useDirectives.Count - 1);
        }


        private void ConvertStructBinding(ASTNodeDeclStruct structNode, Core.Name curNamespace, List<Core.UseDirective> useDirectives)
        {
            var name = curNamespace.Concatenate(ConvertName(structNode.name));
            if (!ValidateName(name, structNode.name.GetSpan()))
                return;

            var structIndex = this.session.CreateStruct(name);

            foreach (var structUseNode in structNode.useDirectives)
                useDirectives.Add(ConvertUseDirective(structUseNode));

            this.structBindings.Add(new Binding<ASTNodeDeclStruct>
            {
                name = name,
                declNode = structNode,
                declIndex = structIndex,
                useDirectives = useDirectives.ToArray()
            });

            foreach (var structUseNode in structNode.useDirectives)
                useDirectives.RemoveAt(useDirectives.Count - 1);
        }


        private bool ValidateName(Core.Name name, Diagnostics.Span span)
        {
            int duplicateIndex;
            if (this.session.TryGetDecl(name, out duplicateIndex))
            {
                var duplicateSpan = 
                    this.structBindings.Find(st => st.name.Compare(name))?.declNode.name.GetSpan() ??
                    this.functBindings.Find(st => st.name.Compare(name))?.declNode.name.GetSpan() ??
                    new Diagnostics.Span();

                this.session.AddMessage(
                    Diagnostics.MessageKind.Error,
                    Diagnostics.MessageCode.DuplicateDeclaration,
                    "duplicate declaration of '" + name.GetString() + "'",
                    span);

                this.session.AddInnerMessageToLast(
                    Diagnostics.MessageKind.Info,
                    Diagnostics.MessageCode.DuplicateDeclaration,
                    "first declaration here",
                    duplicateSpan);

                return false;
            }

            return true;
        }
    }
}
