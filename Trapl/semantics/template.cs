using System.Collections.Generic;
using Trapl.Diagnostics;


namespace Trapl.Semantics
{
    public class Template
    {
        public List<TemplateType> typeList = new List<TemplateType>();


        public void Parse(Interface.SourceCode src, Grammar.ASTNode node)
        {
            foreach (var child in node.EnumerateChildren())
            {
                if (child.kind == Grammar.ASTNodeKind.TemplateType)
                {
                    var t = new TemplateTypeGeneric();
                    t.name = src.GetExcerpt(child.Span());
                    typeList.Add(t);
                }
                else if (child.kind == Grammar.ASTNodeKind.TypeName)
                {
                    var t = new TemplateTypeSpecialized();
                    t.concreteTypeNode = child;
                    typeList.Add(t);
                }
            }
        }


        public void ParseConcrete(Interface.Session session, TemplateSubstitution subst, Interface.SourceCode src, Grammar.ASTNode node)
        {
            foreach (var child in node.EnumerateChildren())
            {
                if (child.kind == Grammar.ASTNodeKind.TemplateType)
                {
                    session.diagn.Add(MessageKind.Error, MessageCode.Expected,
                        "expecting concrete type", src, child.Span());
                    throw new Semantics.CheckException();
                }
                else if (child.kind == Grammar.ASTNodeKind.TypeName)
                {
                    var t = new TemplateTypeSpecialized();
                    t.concreteTypeNode = child;
                    t.concreteType = ResolverType.Resolve(session, subst, src, child);
                    typeList.Add(t);
                }
            }
        }


        public void Resolve(Interface.Session session, TemplateSubstitution subst, Interface.SourceCode src)
        {
            foreach (var type in typeList)
            {
                if (type is TemplateTypeSpecialized)
                {
                    if (((TemplateTypeSpecialized)type).concreteType == null)
                    {
                        ((TemplateTypeSpecialized)type).concreteType =
                            ResolverType.Resolve(session, subst, src, ((TemplateTypeSpecialized)type).concreteTypeNode);
                    }
                }
            }
        }


        public bool IsCompatible(Template other)
        {
            if (this.GetTypeNumber() != other.GetTypeNumber())
                return false;

            return true;
        }


        public int GetCompatibilityScore(Template other)
        {
            var score = 0;
            for (int i = 0; i < typeList.Count; i++)
            {
                if (typeList[i] is TemplateTypeGeneric)
                    continue;

                if (((TemplateTypeSpecialized)typeList[i]).concreteType.IsSame(
                        ((TemplateTypeSpecialized)other.typeList[i]).concreteType))
                    score++;
                else
                    return -1;
            }
            return score;
        }


        public bool IsGeneric()
        {
            foreach (var type in typeList)
            {
                if (type is TemplateTypeGeneric)
                    return true;
            }
            return false;
        }


        public bool IsFullyGeneric()
        {
            foreach (var type in typeList)
            {
                if (!(type is TemplateTypeGeneric))
                    return false;
            }
            return true;
        }


        public int GetTypeNumber()
        {
            return typeList.Count;
        }


        public string GetName(Interface.Session session)
        {
            var result = "<";
            for (int i = 0; i < typeList.Count; i++)
            {
                if (typeList[i] is TemplateTypeGeneric)
                    result += "gen " + ((TemplateTypeGeneric)typeList[i]).name;
                else if (typeList[i] is TemplateTypeSpecialized)
                    result += ResolverType.GetName(session, ((TemplateTypeSpecialized)typeList[i]).concreteType);
                else
                    result += "?";

                if (i < typeList.Count - 1)
                    result += ", ";
            }
            return result + ">";
        }
    }


    public class TemplateSubstitution
    {
        public Dictionary<string, Type> nameToTypeMap = new Dictionary<string, Type>();

        public Type GetTypeByGenericName(string name)
        {
            if (nameToTypeMap.ContainsKey(name))
                return nameToTypeMap[name];
            else
                return null;
        }


        public static TemplateSubstitution CreateFromTemplate(Template genericTempl, Template concreteTempl)
        {
            var subst = new TemplateSubstitution();
            for (int i = 0; i < genericTempl.typeList.Count; i++)
            {
                if (!(genericTempl.typeList[i] is TemplateTypeGeneric))
                    continue;

                if (!(concreteTempl.typeList[i] is TemplateTypeSpecialized))
                    continue;

                subst.nameToTypeMap.Add(
                    ((TemplateTypeGeneric)genericTempl.typeList[i]).name,
                    ((TemplateTypeSpecialized)concreteTempl.typeList[i]).concreteType);
            }
            return subst;
        }
    }


    public abstract class TemplateType
    {

    }


    public class TemplateTypeGeneric : TemplateType
    {
        public string name;
    }


    public class TemplateTypeSpecialized : TemplateType
    {
        public Type concreteType = null;
        public Grammar.ASTNode concreteTypeNode = null;
    }
}
