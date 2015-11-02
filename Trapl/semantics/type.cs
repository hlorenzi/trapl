using System.Collections.Generic;


namespace Trapl.Semantics
{
    public abstract class Type
    {
        public bool addressable;


        public virtual Type Clone() { return (Type)MemberwiseClone(); }


        public virtual bool IsResolved()
        {
            return false;
        }


        public virtual bool IsSame(Type other)
        {
            return false;
        }


        public virtual bool IsMatch(Type other)
        {
            return false;
        }

        public virtual string GetString(Infrastructure.Session session)
        {
            return "(???)";
        }
    }


    public class TypeError : Type
    {
        public override bool IsResolved()
        {
            return true;
        }


        public override string GetString(Infrastructure.Session session)
        {
            return "ERROR";
        }
    }


    public class TypeUnconstrained : Type
    {
        public override bool IsMatch(Type other)
        {
            return true;
        }


        public override string GetString(Infrastructure.Session session)
        {
            return "_";
        }
    }


    public class TypePointer : Type
    {
        public Type pointeeType;


        public TypePointer(Type pointeeType)
        {
            this.pointeeType = pointeeType;
        }


        public override bool IsResolved()
        {
            return this.pointeeType.IsResolved();
        }


        public override bool IsSame(Type other)
        {
            if (!(other is TypePointer))
                return false;

            return (((TypePointer)other).pointeeType.IsSame(this.pointeeType));
        }


        public override bool IsMatch(Type other)
        {
            if (other is TypeUnconstrained)
                return true;

            if (!(other is TypePointer))
                return false;

            return (((TypePointer)other).pointeeType.IsMatch(this.pointeeType));
        }


        public override string GetString(Infrastructure.Session session)
        {
            return "&" + pointeeType.GetString(session);
        }
    }


    public class TypeStruct : Type
    {
        public NameInference nameInference = new NameInference();
        public List<DeclStruct> potentialStructs = new List<DeclStruct>();


        public TypeStruct() { }


        public override bool IsResolved()
        {
            return this.potentialStructs.Count == 1;
        }


        public override bool IsSame(Type other)
        {
            var otherStruct = (other as TypeStruct);
            if (otherStruct == null)
                return false;

            if (this.potentialStructs.Count != 1)
                return false;

            if (otherStruct.potentialStructs.Count != 1)
                return false;

            return (this.potentialStructs[0] == otherStruct.potentialStructs[0]);
        }


        public override bool IsMatch(Type other)
        {
            if (other is TypeUnconstrained)
                return true;

            var otherStruct = (other as TypeStruct);
            if (otherStruct == null)
                return false;

            if (!PathASTUtil.Compare(this.nameInference.pathASTNode, otherStruct.nameInference.pathASTNode))
                return false;

            return (this.nameInference.template.IsMatch(otherStruct.nameInference.template));
        }


        public override string GetString(Infrastructure.Session session)
        {
            return PathASTUtil.GetString(nameInference.pathASTNode) +
                nameInference.template.GetString(session);
        }
    }

    public class TypeTuple : Type
    {
        public List<Type> elementTypes = new List<Type>();


        public override bool IsResolved()
        {
            foreach (var elem in this.elementTypes)
            {
                if (!elem.IsResolved())
                    return false;
            }
            return true;
        }


        public override bool IsSame(Type other)
        {
            var otherf = other as TypeTuple;
            if (otherf == null)
                return false;

            if (this.elementTypes.Count != otherf.elementTypes.Count)
                return false;

            for (int i = 0; i < elementTypes.Count; i++)
            {
                if (!this.elementTypes[i].IsSame(otherf.elementTypes[i]))
                    return false;
            }

            return true;
        }


        public override bool IsMatch(Type other)
        {
            if (other is TypeUnconstrained)
                return true;

            var otherf = other as TypeTuple;
            if (otherf == null)
                return false;

            if (this.elementTypes.Count != otherf.elementTypes.Count)
                return false;

            for (int i = 0; i < elementTypes.Count; i++)
            {
                if (!this.elementTypes[i].IsMatch(otherf.elementTypes[i]))
                    return false;
            }

            return true;
        }


        public override string GetString(Infrastructure.Session session)
        {
            var result = "(";
            for (int i = 0; i < elementTypes.Count; i++)
            {
                result += elementTypes[i].GetString(session);
                if (i < elementTypes.Count - 1)
                    result += ", ";
            }
            return result + ")";
        }
    }


    public class TypeFunct : Type
    {
        public List<Type> argumentTypes = new List<Type>();
        public Type returnType;


        public TypeFunct() { }


        public TypeFunct(DeclFunct f)
        {
            for (int i = 0; i < f.arguments.Count; i++)
            {
                this.argumentTypes.Add(f.arguments[i].type);
            }
            this.returnType = f.returnType;
        }


        public override bool IsResolved()
        {
            if (!this.returnType.IsResolved())
                return false;

            foreach (var arg in this.argumentTypes)
            {
                if (!arg.IsResolved())
                    return false;
            }

            return true;
        }


        public override bool IsSame(Type other)
        {
            var otherf = other as TypeFunct;
            if (otherf == null)
                return false;

            if (this.argumentTypes.Count != otherf.argumentTypes.Count)
                return false;

            for (int i = 0; i < argumentTypes.Count; i++)
            {
                if (!this.argumentTypes[i].IsSame(otherf.argumentTypes[i]))
                    return false;
            }

            return true;
        }


        public override bool IsMatch(Type other)
        {
            if (other is TypeUnconstrained)
                return true;

            var otherf = other as TypeFunct;
            if (otherf == null)
                return false;

            if (this.argumentTypes.Count != otherf.argumentTypes.Count)
                return false;

            for (int i = 0; i < argumentTypes.Count; i++)
            {
                if (!this.argumentTypes[i].IsMatch(otherf.argumentTypes[i]))
                    return false;
            }

            return true;
        }


        public override string GetString(Infrastructure.Session session)
        {
            var result = "(";
            for (int i = 0; i < argumentTypes.Count; i++)
            {
                result += argumentTypes[i].GetString(session);
                if (i < argumentTypes.Count - 1)
                    result += ", ";
            }
            return result + ") -> " + returnType.GetString(session);
        }
    }
}
