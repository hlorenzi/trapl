namespace Trapl.Core
{
    public class Name
    {
        public string[] identifiers;


        public static Name FromPath(params string[] identifiers)
        {
            var name = new Name();
            name.identifiers = identifiers;
            return name;
        }


        public Name Concatenate(Name other)
        {
            var name = new Name();
            name.identifiers = new string[this.identifiers.Length + other.identifiers.Length];
            this.identifiers.CopyTo(name.identifiers, 0);
            other.identifiers.CopyTo(name.identifiers, this.identifiers.Length);
            return name;
        }


        public Name ConcatenateIdentifier(string identifier)
        {
            var name = new Name();
            name.identifiers = new string[this.identifiers.Length + 1];
            this.identifiers.CopyTo(name.identifiers, 0);
            name.identifiers[this.identifiers.Length] = identifier;
            return name;
        }


        public bool Compare(Name other)
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
