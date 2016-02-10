using System.Collections.Generic;


namespace Trapl.Extraction
{
    public partial class Extractor
    {
        public static Extractor Extract(Grammar.ASTNodeDeclGroup topLevelNode)
        {
            var extractor = new Extractor();
            extractor.ExtractGroup(topLevelNode, Path.Empty());
            return extractor;
        }


        public List<Struct> structs = new List<Struct>();
        public List<Funct> functs = new List<Funct>();
        private List<UseDirective> currentUseDirectives = new List<UseDirective>();


        private Extractor()
        {

        }


        private void ExtractGroup(Grammar.ASTNodeDeclGroup declGroup, Path curPath)
        {
            var useDirectiveCountBefore = currentUseDirectives.Count;

            //foreach (var useNode in declGroup.useDirectives)
            //    useDirectives.Add(UseDirectiveResolver.Resolve(useNode));

            foreach (var structNode in declGroup.structDecls)
                this.ExtractStruct(structNode, curPath);

            foreach (var functNode in declGroup.functDecls)
                this.ExtractFunct(functNode, curPath);

            foreach (var namespaceNode in declGroup.namespaceDecls)
                this.ExtractNamespace(namespaceNode, curPath);

            while (currentUseDirectives.Count > useDirectiveCountBefore)
                currentUseDirectives.RemoveAt(currentUseDirectives.Count - 1);
        }


        private void ExtractNamespace(Grammar.ASTNodeDeclNamespace namespaceNode, Path curPath)
        {
            var innerNamespace = curPath.Concatenate(Path.FromASTNode(namespaceNode.path));
            this.ExtractGroup(namespaceNode.innerGroup, innerNamespace);
        }
    }
}
