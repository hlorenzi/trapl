using System.Collections.Generic;


namespace Trapl.Semantics
{
    public abstract class Type
    {
        public bool addressable;

        public virtual bool IsSame(Type other) { return false; }
    }


    public class ResolvedTypePointer : Type
    {
        public Type pointeeType;

        public ResolvedTypePointer(Type pointeeType) { this.pointeeType = pointeeType; }
        public override bool IsSame(Type other)
        {
            if (!(other is ResolvedTypePointer)) return false;
            return (((ResolvedTypePointer)other).pointeeType.IsSame(this.pointeeType));
        }
    }


    public class ResolvedTypeStruct : Type
    {
        public DefinitionStruct structDef;

        public ResolvedTypeStruct(DefinitionStruct structDef) { this.structDef = structDef; }
        public override bool IsSame(Type other)
        {
            if (!(other is ResolvedTypeStruct)) return false;
            return (((ResolvedTypeStruct)other).structDef == this.structDef);
        }
    }


    public class ResolvedTypeFunct : Type
    {
        public List<Type> argumentTypes = new List<Type>();
        public Type returnType;

        public override bool IsSame(Type other)
        {
            var otherf = other as ResolvedTypeFunct;
            if (otherf == null) return false;
            if (this.argumentTypes.Count != otherf.argumentTypes.Count) return false;
            for (int i = 0; i < argumentTypes.Count; i++)
                if (!this.argumentTypes[i].IsSame(otherf.argumentTypes[i])) return false;
            return true;
        }
    }
}
