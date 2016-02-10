namespace Trapl.Extraction
{
    public class TypeStruct : Type
    {
        public int nameIndex;


        public static TypeStruct WithName(int nameIndex)
        {
            return new TypeStruct { nameIndex = nameIndex };
        }


        public override string GetString()
        {
            return "struct name" + nameIndex;
        }
    }
}
