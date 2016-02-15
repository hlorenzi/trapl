using System.Collections.Generic;


namespace Trapl.Semantics
{
    public class TypeInferencer
    {
        public static bool Try(Core.Session session, Core.Type typeFrom, ref Core.Type typeTo)
        {
            if (typeTo is Core.TypePlaceholder && !(typeFrom is Core.TypePlaceholder))
            {
                typeTo = typeFrom;
                return true;
            }

            else if (typeTo is Core.TypePointer && typeFrom is Core.TypePointer)
            {
                var refTo = (Core.TypePointer)typeTo;
                var refFrom = (Core.TypePointer)typeFrom;
                var result = Try(session, refFrom.pointedToType, ref refTo.pointedToType);
                result |= Try(session, refTo.pointedToType, ref refFrom.pointedToType);
                return result;
            }

            /*else if (typeTo is TypeStruct && typeFrom is TypeStruct)
            {
                var structTo = (TypeStruct)typeTo;
                var structFrom = (TypeStruct)typeFrom;

                if (structTo.potentialStructs.Count > 1 && structFrom.potentialStructs.Count == 1)
                {
                    TryInference(session, structFrom.nameInference.template, ref structTo.nameInference.template);
                    typeTo = typeFrom;
                }

                return false;
            }*/

            else if (typeTo is Core.TypeFunct && typeFrom is Core.TypeFunct)
            {
                var functTo = (Core.TypeFunct)typeTo;
                var functFrom = (Core.TypeFunct)typeFrom;
                var result = Try(session, functFrom.returnType, ref functTo.returnType);

                if (functTo.parameterTypes == null && functFrom.parameterTypes != null)
                {
                    functTo.parameterTypes = new Core.Type[functFrom.parameterTypes.Length];
                    System.Array.Copy(functFrom.parameterTypes, functTo.parameterTypes, functFrom.parameterTypes.Length);
                    result = true;
                }

                /*if (functTo.argumentTypes != null &&
                    functFrom.argumentTypes != null &&
                    functTo.argumentTypes.Count == functFrom.argumentTypes.Count)
                {
                    for (var i = 0; i < functTo.argumentTypes.Count; i++)
                    {
                        var argTo = functTo.argumentTypes[i];
                        result |= Try(session, functFrom.argumentTypes[i], ref argTo);
                        functTo.argumentTypes[i] = argTo;
                    }
                }*/

                typeTo = functTo;
                return result;
            }

            else if (typeTo is Core.TypeTuple && typeFrom is Core.TypeTuple)
            {
                var tupleTo = (Core.TypeTuple)typeTo;
                var tupleFrom = (Core.TypeTuple)typeFrom;
                var result = false;

                if (tupleTo.elementTypes.Length == tupleFrom.elementTypes.Length)
                {
                    for (var i = 0; i < tupleTo.elementTypes.Length; i++)
                    {
                        var elemTo = tupleTo.elementTypes[i];
                        result |= Try(session, tupleFrom.elementTypes[i], ref elemTo);
                        tupleTo.elementTypes[i] = elemTo;
                    }
                }

                typeTo = tupleTo;
                return result;
            }

            return false;
        }
    }
}
