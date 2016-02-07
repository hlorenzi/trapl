namespace Trapl.Semantics
{
    public partial class DeclResolver
    {
        public DeclResolver(Core.Session session)
        {
            this.session = session;
        }


        private Core.Session session;
    }
}
