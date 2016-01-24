namespace Trapl.Infrastructure
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
