namespace Trapl.Extraction
{
    public class Name
    {
        public int pathIndex;


        public static Name WithPath(int pathIndex)
        {
            return new Name { pathIndex = pathIndex };
        }


        public string GetString()
        {
            return "path" + pathIndex;
        }
    }
}
