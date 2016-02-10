namespace Trapl.Extraction
{
    public partial class Extractor
    {
        private void ExtractFunct(Grammar.ASTNodeDeclFunct functNode, Path curPath)
        {
            var useDirs = currentUseDirectives.ToArray();

            var extrFunct = new Funct();
            extrFunct.path = curPath.Concatenate(Path.FromASTNode(functNode.name.path));

            for (var i = 0; i < functNode.parameters.Count; i++)
            {
                var registerIndex = extrFunct.registerTypes.Count;

                extrFunct.registerTypes.Add(extrFunct.dependencies.ExtractType(
                    functNode.parameters[i].type, useDirs));

                extrFunct.bindings.Add(new Funct.Binding
                {
                    nameIndex = extrFunct.dependencies.ExtractName(functNode.parameters[i].name, useDirs),
                    registerIndex = registerIndex
                });
            }

            functs.Add(extrFunct);
        }
    }
}
