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
