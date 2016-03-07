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


        public override bool IsSame(Type other)
        {
            var otherPointer = other as Core.TypePointer;
            if (otherPointer == null)
                return false;

            if (this.mutable != otherPointer.mutable)
                return false;

            return this.pointedToType.IsSame(otherPointer.pointedToType);
        }


        public override bool IsResolved()
        {
            return this.pointedToType.IsResolved();
        }


        public override bool IsError()
        {
            return this.pointedToType.IsError();
        }


        public override string GetString(Core.Session session)
        {
            return "*" + (this.mutable ? "mut " : "") + this.pointedToType.GetString(session);
        }
    }
}
