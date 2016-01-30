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


        public void ConvertTopLevelDeclGroup(ASTNodeDeclGroup topLevelGroupNode)
        {
            this.ConvertDeclGroup(topLevelGroupNode, Core.Name.FromPath(), new List<Core.UseDirective>());
        }


        private void ConvertDeclGroup(ASTNodeDeclGroup declGroup, Core.Name curNamespace, List<Core.UseDirective> useDirectives)
        {
            var useDirectiveCountBefore = useDirectives.Count;

            foreach (var useNode in declGroup.useDirectives)
                useDirectives.Add(ConvertUseDirective(useNode));

            foreach (var structNode in declGroup.structDecls)
                this.ConvertStructDecl(structNode, curNamespace, useDirectives);

            foreach (var functNode in declGroup.functDecls)
                this.ConvertFunctDecl(functNode, curNamespace, useDirectives);

            foreach (var namespaceNode in declGroup.namespaceDecls)
                this.ConvertNamespaceDecl(namespaceNode, curNamespace, useDirectives);

            while (useDirectives.Count > useDirectiveCountBefore)
                useDirectives.RemoveAt(useDirectives.Count - 1);
        }


        private void ConvertNamespaceDecl(ASTNodeDeclNamespace namespaceNode, Core.Name curNamespace, List<Core.UseDirective> useDirectives)
        {
            var innerNamespace = curNamespace.Concatenate(this.ConvertName(namespaceNode.path));

            for (var i = 0; i < namespaceNode.path.identifiers.Count; i++)
                useDirectives.Add(new Core.UseDirectiveAll { name = curNamespace.ConcatenateIdentifier(namespaceNode.path.identifiers[i].GetExcerpt()) });

            this.ConvertDeclGroup(namespaceNode.innerGroup, innerNamespace, useDirectives);

            for (var i = 0; i < namespaceNode.path.identifiers.Count; i++)
                useDirectives.RemoveAt(useDirectives.Count - 1);
        }


        private void ConvertStructDecl(ASTNodeDeclStruct structNode, Core.Name curNamespace, List<Core.UseDirective> useDirectives)
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


        private void ConvertFunctDecl(ASTNodeDeclFunct functNode, Core.Name curNamespace, List<Core.UseDirective> useDirectives)
        {
            var name = curNamespace.Concatenate(ConvertName(functNode.name));
            if (!ValidateName(name, functNode.name.GetSpan()))
                return;

            var functIndex = this.session.CreateFunct(name);

            this.functBindings.Add(new Binding<ASTNodeDeclFunct>
            {
                name = name,
                declNode = functNode,
                declIndex = functIndex,
                useDirectives = useDirectives.ToArray()
            });
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
