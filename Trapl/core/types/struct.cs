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


        public override bool IsSame(Type other)
        {
            var otherStruct = other as TypeStruct;
            if (otherStruct == null)
                return false;

            return this.structIndex == otherStruct.structIndex;
        }


        public override bool IsResolved()
        {
            return true;
        }


        public override bool IsError()
        {
            return false;
        }


        public override string GetString(Core.Session session)
        {
            return session.GetStructName(this.structIndex).GetString();
        }
    }
}
