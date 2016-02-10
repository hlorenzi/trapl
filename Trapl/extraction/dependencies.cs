using System;
using System.Collections.Generic;


namespace Trapl.Extraction
{
    public class Dependencies
    {
        public List<Path> paths = new List<Path>();
        public List<Name> names = new List<Name>();
        public List<Type> types = new List<Type>();


        public int ExtractName(Grammar.ASTNodeName nameNode, UseDirective[] useDirectives)
        {
            var pathIndex = this.paths.Count;
            this.paths.Add(Path.FromASTNode(nameNode.path));

            this.names.Add(Name.WithPath(pathIndex));
            return this.names.Count - 1;
        }


        public int ExtractType(Grammar.ASTNodeType typeNode, UseDirective[] useDirectives)
        {
            var typeStructNode = typeNode as Grammar.ASTNodeTypeStruct;
            if (typeStructNode != null)
            {
                var nameIndex = this.ExtractName(typeStructNode.name, useDirectives);
                var typeIndex = this.types.Count;
                this.types.Add(TypeStruct.WithName(nameIndex));
                return typeIndex;
            }

            var typeTupleNode = typeNode as Grammar.ASTNodeTypeTuple;
            if (typeTupleNode != null)
            {
                var elementTypeIndices = new int[typeTupleNode.elements.Count];
                for (var i = 0; i < typeTupleNode.elements.Count; i++)
                    elementTypeIndices[i] =ExtractType(typeTupleNode.elements[i], useDirectives);

                var typeIndex = this.types.Count;
                this.types.Add(TypeTuple.Of(elementTypeIndices));
                return typeIndex;
            }

            throw new NotImplementedException();
        }


        public void PrintToConsole(string indentation = "")
        {
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine(indentation + "dependant paths");
            Console.ResetColor();
            for (var i = 0; i < this.paths.Count; i++)
            {
                Console.Write(indentation + "  path" + i + " ");
                Console.ForegroundColor = ConsoleColor.DarkGray;
                Console.WriteLine(this.paths[i].GetString());
                Console.ResetColor();
            }

            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine(indentation + "dependant names");
            Console.ResetColor();
            for (var i = 0; i < this.names.Count; i++)
            {
                Console.Write(indentation + "  name" + i + " ");
                Console.ForegroundColor = ConsoleColor.DarkGray;
                Console.WriteLine(this.names[i].GetString());
                Console.ResetColor();
            }

            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine(indentation + "dependant types");
            Console.ResetColor();
            for (var i = 0; i < this.types.Count; i++)
            {
                Console.Write(indentation + "  type" + i + " ");
                Console.ForegroundColor = ConsoleColor.DarkGray;
                Console.WriteLine(this.types[i].GetString());
                Console.ResetColor();
            }
        }
    }
}
