using System.Collections.Generic;


namespace Trapl.Semantics
{
    public class StructRecursionChecker
    {
        public static bool Check(Core.Session session)
        {
            var foundErrors = false;

            var structs = session.GetStructs();
            for (var i = 0; i < structs.Count; i++)
            {
                var seenStructs = new Stack<int>();
                foundErrors |= CheckStruct(session, i, seenStructs);
            }

            return foundErrors;
        }


        private static bool CheckStruct(Core.Session session, int structIndex, Stack<int> seenStructs)
        {
            var st = session.GetStruct(structIndex);
            var err = false;

            seenStructs.Push(structIndex);

            for (var i = 0; i < st.fieldTypes.Count; i++)
            {
                err |= CheckField(session, structIndex, i, st.fieldTypes[i], seenStructs);

                var fieldTuple = st.fieldTypes[i] as Core.TypeTuple;
                if (fieldTuple != null)
                {
                    for (var j = 0; j < fieldTuple.elementTypes.Length; j++)
                        err |= CheckField(session, structIndex, i, fieldTuple.elementTypes[j], seenStructs);
                }
            }

            seenStructs.Pop();
            return err;
        }


        private static bool CheckField(
            Core.Session session, int structIndex, int fieldIndex,
            Core.Type innerType, Stack<int> seenStructs)
        {
            var fieldStruct = innerType as Core.TypeStruct;
            if (fieldStruct == null)
                return false;

            var st = session.GetStruct(structIndex);

            Core.Name fieldName;
            st.fieldNames.FindByValue(fieldIndex, out fieldName);

            session.PushContext(
                "in struct '" + session.GetStructName(structIndex).GetString() + "', " +
                "field '" + fieldName.GetString() + "'",
                st.GetFieldNameSpan(fieldIndex));

            var err = false;
            if (seenStructs.Contains(fieldStruct.structIndex))
            {
                err = true;
                session.AddMessage(
                    Diagnostics.MessageKind.Error,
                    Diagnostics.MessageCode.StructRecursion,
                    "struct recursion",
                    session.GetStruct(fieldStruct.structIndex).GetNameSpan(),
                    st.GetFieldNameSpan(fieldIndex));
            }

            if (!err)
                CheckStruct(session, fieldStruct.structIndex, seenStructs);

            session.PopContext();
            return err;
        }
    }
}
