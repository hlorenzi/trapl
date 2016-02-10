namespace Trapl.Extraction
{
    public class Path
    {
        public string[] identifiers;
        public bool isAbsolute;


        public static Path Empty()
        {
            return new Path { identifiers = new string[0] };
        }


        public static Path FromIdentifiers(params string[] identifiers)
        {
            return new Path { identifiers = identifiers };
        }


        public static Path FromASTNode(Grammar.ASTNodePath pathNode)
        {
            var path = new Path() { identifiers = new string[pathNode.identifiers.Count] };
            path.isAbsolute = pathNode.isAbsolute;

            for (var i = 0; i < pathNode.identifiers.Count; i++)
                path.identifiers[i] = pathNode.identifiers[i].GetExcerpt();

            return path;
        }


        public Path Concatenate(Path other)
        {
            var name = new Path();
            name.identifiers = new string[this.identifiers.Length + other.identifiers.Length];
            this.identifiers.CopyTo(name.identifiers, 0);
            other.identifiers.CopyTo(name.identifiers, this.identifiers.Length);
            return name;
        }


        public Path ConcatenateIdentifier(string identifier)
        {
            var name = new Path();
            name.identifiers = new string[this.identifiers.Length + 1];
            this.identifiers.CopyTo(name.identifiers, 0);
            name.identifiers[this.identifiers.Length] = identifier;
            return name;
        }


        public bool Compare(Path other)
        {
            if (this.identifiers.Length != other.identifiers.Length)
                return false;

            for (var i = 0; i < this.identifiers.Length; i++)
            {
                if (this.identifiers[i] != other.identifiers[i])
                    return false;
            }

            return true;
        }


        public string GetString()
        {
            var result = "";

            for (var i = 0; i < this.identifiers.Length; i++)
            {
                result += this.identifiers[i];
                if (i < this.identifiers.Length - 1)
                    result += "::";
            }

            return result;
        }
    }
}
