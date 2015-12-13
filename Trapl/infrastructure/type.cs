using System.Collections.Generic;


namespace Trapl.Infrastructure
{
    public abstract class Type
    {
        public virtual bool IsResolved()
        {
            return false;
        }


        public virtual bool IsError()
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
            return false;
        }


        public override bool IsError()
        {
            return true;
        }


        public override string GetString(Infrastructure.Session session)
        {
            return "ERROR";
        }
    }


    public class TypePlaceholder : Type
    {
        public override bool IsSame(Type other)
        {
            return true;
        }


        public override bool IsMatch(Type other)
        {
            return true;
        }


        public override string GetString(Infrastructure.Session session)
        {
            return "_";
        }
    }


    public class TypeReference : Type
    {
        public Type referencedType;


        public TypeReference(Type referencedType)
        {
            this.referencedType = referencedType;
        }


        public override bool IsResolved()
        {
            return this.referencedType.IsResolved();
        }


        public override bool IsSame(Type other)
        {
            if (!(other is TypeReference))
                return false;

            return (((TypeReference)other).referencedType.IsSame(this.referencedType));
        }


        public override bool IsMatch(Type other)
        {
            if (other is TypePlaceholder)
                return true;

            if (!(other is TypeReference))
                return false;

            return (((TypeReference)other).referencedType.IsMatch(this.referencedType));
        }


        public override string GetString(Infrastructure.Session session)
        {
            return "&" + referencedType.GetString(session);
        }
    }


    public class TypeStruct : Type
    {
        public Name nameInference = new Name();
        public List<DeclStruct> potentialStructs = new List<DeclStruct>();


        public TypeStruct() { }


        public TypeStruct(DeclStruct decl)
        {
            this.nameInference = new Name(new Diagnostics.Span(), decl.name.pathASTNode, decl.name.template);
            this.potentialStructs.Add(decl);
        }


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


        public bool IsStruct(DeclStruct decl)
        {
            if (this.potentialStructs.Count != 1)
                return false;

            return (this.potentialStructs[0] == decl);
        }


        public override bool IsMatch(Type other)
        {
            if (other is TypePlaceholder)
                return true;

            var otherStruct = (other as TypeStruct);
            if (otherStruct == null)
                return false;

            if (!Semantics.PathUtil.Compare(this.nameInference.pathASTNode, otherStruct.nameInference.pathASTNode))
                return false;

            return (this.nameInference.template.IsMatch(otherStruct.nameInference.template));
        }


        public override string GetString(Infrastructure.Session session)
        {
            return Semantics.PathUtil.GetDisplayString(nameInference.pathASTNode) +
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
            if (other is TypePlaceholder)
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
        public List<Type> argumentTypes = null;
        public Type returnType;


        public TypeFunct(Type returnType)
        {
            this.returnType = returnType;
        }


        public TypeFunct(DeclFunct f)
        {
            this.argumentTypes = new List<Type>();

            for (int i = 0; i < f.argumentTypes.Count; i++)
                this.argumentTypes.Add(f.argumentTypes[i]);

            this.returnType = f.returnType;
        }


        public override bool IsResolved()
        {
            if (!this.returnType.IsResolved() || this.argumentTypes == null)
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

            if (!this.returnType.IsMatch(otherf.returnType))
                return false;

            if (this.argumentTypes == null || otherf.argumentTypes == null)
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
            if (other is TypePlaceholder)
                return true;

            var otherf = other as TypeFunct;
            if (otherf == null)
                return false;

            if (!this.returnType.IsMatch(otherf.returnType))
                return false;

            if (this.argumentTypes == null || otherf.argumentTypes == null)
                return true;

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

            if (this.argumentTypes == null)
                result += "...";
            else
            {
                for (int i = 0; i < argumentTypes.Count; i++)
                {
                    result += argumentTypes[i].GetString(session);
                    if (i < argumentTypes.Count - 1)
                        result += ", ";
                }
            }

            return result + ") -> " + returnType.GetString(session);
        }
    }
}
