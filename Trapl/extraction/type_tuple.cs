namespace Trapl.Extraction
{
    public class TypeTuple : Type
    {
        public int[] elementTypeIndices;


        public static TypeTuple Empty()
        {
            return new TypeTuple { elementTypeIndices = new int[0] };
        }


        public static TypeTuple Of(params int[] elementTypeIndices)
        {
            return new TypeTuple { elementTypeIndices = elementTypeIndices };
        }


        public override string GetString()
        {
            var result = "(";
            for (var i = 0; i < this.elementTypeIndices.Length; i++)
            {
                result += "type" + this.elementTypeIndices[i];
                if (i < this.elementTypeIndices.Length - 1)
                    result += ", ";
            }
            return result + ")";
        }
    }
}
