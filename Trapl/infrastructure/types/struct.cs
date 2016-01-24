namespace Trapl.Infrastructure
{
    public class TypeStruct : Type
    {
        public int structIndex;


        public TypeStruct(int structIndex)
        {
            this.structIndex = structIndex;
        }


        public override string GetString(Infrastructure.Session session)
        {
            return session.GetStructName(this.structIndex).GetString();
        }
    }
}
