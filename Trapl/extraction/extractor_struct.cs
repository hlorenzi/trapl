namespace Trapl.Extraction
{
    public partial class Extractor
    {
        private void ExtractStruct(Grammar.ASTNodeDeclStruct structNode, Path curPath)
        {
            var extrStruct = new Struct();
            extrStruct.path = curPath.Concatenate(Path.FromASTNode(structNode.name.path));

            for (var i = 0; i < structNode.fields.Count; i++)
            {
                var extrField = new Struct.Field();

                extrField.nameIndex = extrStruct.dependencies.ExtractName(
                    structNode.fields[i].name, currentUseDirectives.ToArray());

                extrField.typeIndex = extrStruct.dependencies.ExtractType(
                    structNode.fields[i].type, currentUseDirectives.ToArray());

                extrStruct.fields.Add(extrField);
            }

            structs.Add(extrStruct);
        }
    }
}
