using System.Collections.Generic;


namespace Trapl.Semantics
{
    public abstract class Type
    {
        public bool addressable;

        public virtual bool IsSame(Type other) { return false; }

        public virtual string GetString(Interface.Session session) { return "<error>"; }
    }


    public class TypePointer : Type
    {
        public Type pointeeType;

        public TypePointer(Type pointeeType) { this.pointeeType = pointeeType; }
        public override bool IsSame(Type other)
        {
            if (!(other is TypePointer)) return false;
            return (((TypePointer)other).pointeeType.IsSame(this.pointeeType));
        }

        public override string GetString(Interface.Session session)
        {
            return "&" + pointeeType.GetString(session);
        }
    }


    public class TypeStruct : Type
    {
        public DefStruct structDef;

        public TypeStruct(DefStruct structDef) { this.structDef = structDef; }
        public override bool IsSame(Type other)
        {
            if (!(other is TypeStruct)) return false;
            return (((TypeStruct)other).structDef == this.structDef);
        }
        public override string GetString(Interface.Session session)
        { 
            foreach (var topDecl in session.topDecls)
            {
                if (structDef == topDecl.def)
                {
                    return topDecl.qualifiedName + "::" +
                        ASTPatternUtil.GetString(topDecl.patternASTNode);
                }
            }
            return "<unknown>";
        }
    }


    public class TypeFunct : Type
    {
        public List<Type> argumentTypes = new List<Type>();
        public Type returnType;

        public override bool IsSame(Type other)
        {
            var otherf = other as TypeFunct;
            if (otherf == null) return false;
            if (this.argumentTypes.Count != otherf.argumentTypes.Count) return false;
            for (int i = 0; i < argumentTypes.Count; i++)
                if (!this.argumentTypes[i].IsSame(otherf.argumentTypes[i])) return false;
            return true;
        }
    }
}
