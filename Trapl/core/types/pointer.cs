namespace Trapl.Core
{
    public class TypePointer : Type
    {
        public bool mutable;
        public Type pointedToType;


        public static TypePointer Of(bool mutable, Type pointedToType)
        {
            var ptr = new TypePointer();
            ptr.mutable = mutable;
            ptr.pointedToType = pointedToType;
            return ptr;
        }


        public static TypePointer ImmutableOf(Type pointedToType)
        {
            var ptr = new TypePointer();
            ptr.mutable = false;
            ptr.pointedToType = pointedToType;
            return ptr;
        }


        public static TypePointer MutableOf(Type pointedToType)
        {
            var ptr = new TypePointer();
            ptr.mutable = true;
            ptr.pointedToType = pointedToType;
            return ptr;
        }


        public override string GetString(Core.Session session)
        {
            return "*" + (this.mutable ? "mut " : "") + this.pointedToType.GetString(session);
        }
    }
}
