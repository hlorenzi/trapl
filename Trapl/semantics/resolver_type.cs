using System.Collections.Generic;
using Trapl.Diagnostics;


namespace Trapl.Semantics
{
    public class ResolverType
    {
        public static Type Resolve(Interface.Session session, TemplateSubstitution subst, Interface.SourceCode src, Grammar.ASTNode typeNode)
        {
            var pointerLevels = 0;
            var currentChild = 0;
            while (typeNode.ChildIs(currentChild, Grammar.ASTNodeKind.Operator))
            {
                pointerLevels++;
                currentChild++;
            }

            if (typeNode.ChildIs(currentChild, Grammar.ASTNodeKind.Identifier))
            {
                // Get the type name and check if it is a generic type that can be substituted.
                var name = src.GetExcerpt(typeNode.Child(currentChild).Span());

                var resolvedType = subst.GetTypeByGenericName(name);
                if (resolvedType == null)
                {
                    // If it is not a generic type name, find the declaration that matches its name.
                    var def = session.structDefs.Find(d => d.fullName == name);
                    if (def == null)
                    {
                        session.diagn.Add(MessageKind.Error, MessageCode.UnknownType,
                            "unknown type", src, typeNode.Child(currentChild).Span());
                        throw new Semantics.CheckException();
                    }

                    // If a template list follows, parse that.
                    currentChild++;
                    var specialization = new Template();
                    if (typeNode.ChildIs(currentChild, Grammar.ASTNodeKind.TemplateList))
                        specialization.ParseConcrete(session, subst, src, typeNode.Child(currentChild));

                    // Find the specialization that best matches the template list given.
                    int maxScore = -1;
                    List<DefinitionStruct> bestCompatibleStructs = new List<DefinitionStruct>();

                    if (def.mainDef.templateList.IsCompatible(specialization))
                    {
                        foreach (var st in def.defs)
                        {
                            st.templateList.Resolve(session, subst, st.source);
                            var score = st.templateList.GetCompatibilityScore(specialization);
                            if (score > maxScore)
                            {
                                maxScore = score;
                                bestCompatibleStructs.Clear();
                                bestCompatibleStructs.Add(st);
                            }
                            else if (score == maxScore)
                            {
                                bestCompatibleStructs.Add(st);
                            }
                        }
                    }

                    if (bestCompatibleStructs.Count == 0)
                    {
                        session.diagn.Add(MessageKind.Error, MessageCode.IncompatibleTemplate,
                            "incompatible specialization",
                            MessageCaret.Primary(src, typeNode.Span()),
                            MessageCaret.Primary(def.mainDef.source, def.mainDef.nameSpan));
                        throw new Semantics.CheckException();
                    }

                    if (bestCompatibleStructs.Count > 1)
                    {
                        var carets = new MessageCaret[bestCompatibleStructs.Count + 1];
                        carets[0] = MessageCaret.Primary(src, typeNode.Span());
                        for (int i = 0; i < bestCompatibleStructs.Count; i++)
                            carets[i + 1] = MessageCaret.Primary(
                                bestCompatibleStructs[i].source,
                                bestCompatibleStructs[i].nameSpan);

                        session.diagn.Add(MessageKind.Error, MessageCode.IncompatibleTemplate,
                            "ambiguous specializations", carets);
                        throw new Semantics.CheckException();
                    }

                    var bestStruct = bestCompatibleStructs[0];

                    // If the best matching struct is not yet resolved, resolve it now.
                    if (!bestStruct.resolved)
                    {
                        var newSubst =
                            TemplateSubstitution.CreateFromTemplate(def.mainDef.templateList, bestStruct.templateList);

                        // Create a new specialization if the best matching struct is generic.
                        if (bestStruct.templateList.IsGeneric())
                        {
                            newSubst =
                                TemplateSubstitution.CreateFromTemplate(bestStruct.templateList, specialization);

                            bestStruct = bestStruct.Clone();
                            bestStruct.templateList = specialization;
                            bestStruct.synthesized = true;
                            def.defs.Add(bestStruct);
                        }
                        ResolverStruct.Resolve(session, newSubst, bestStruct);
                    }

                    resolvedType = new ResolvedTypeStruct(bestStruct);
                }

                for (int i = 0; i < pointerLevels; i++)
                    resolvedType = new ResolvedTypePointer(resolvedType);

                return resolvedType;
            }

            session.diagn.Add(MessageKind.Error, MessageCode.UnknownType,
                "unknown type", src, typeNode.Child(currentChild).Span());
            throw new Semantics.CheckException();
        }


        public static string GetName(Interface.Session session, Type type)
        {
            if (type is ResolvedTypePointer)
            {
                return "&" + GetName(session, ((ResolvedTypePointer)type).pointeeType);
            }
            else if (type is ResolvedTypeStruct)
            {
                var typeStruct = ((ResolvedTypeStruct)type).structDef;
                foreach (var def in session.structDefs)
                {
                    foreach (var st in def.defs)
                    {
                        if (st == typeStruct)
                        {
                            return def.fullName + (
                                st.templateList.GetTypeNumber() == 0 ?
                                "" :
                                "::" + st.templateList.GetName(session));
                        }
                    }
                }

                return "<anon struct>" + (
                    typeStruct.templateList.GetTypeNumber() == 0 ?
                    "" :
                    "::" + typeStruct.templateList.GetName(session));
            }

            return "<unknown>";
        }
    }
}
