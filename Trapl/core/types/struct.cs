namespace Trapl.Core
{
    public class TypeStruct : Type
    {
        public int structIndex;


        public static TypeStruct Of(int structIndex)
        {
            var st = new TypeStruct();
            st.structIndex = structIndex;
            return st;
        }


        public override string GetString(Core.Session session)
        {
            return session.GetStructName(this.structIndex).GetString();
        }
    }
}
