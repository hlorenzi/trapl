using Trapl.Diagnostics;


namespace Trapl.Semantics
{
    class TemplateConsistencyChecker
    {
        public static void Check(Interface.Session session)
        {
            foreach (var def in session.structDefs)
            {
                def.mainDef = null;
                foreach (var st in def.defs)
                {
                    if (st.templateList.IsFullyGeneric())
                    {
                        if (def.mainDef != null)
                        {
                            session.diagn.Add(MessageKind.Error, MessageCode.DoubleDecl,
                                "conflicting template declarations",
                                MessageCaret.Primary(st.source, st.nameSpan),
                                MessageCaret.Primary(def.mainDef.source, def.mainDef.nameSpan));
                            continue;
                        }

                        def.mainDef = st;
                    }
                }
            }


            foreach (var def in session.structDefs)
            {
                foreach (var st in def.defs)
                {
                    if (def.mainDef == null)
                    {
                        session.diagn.Add(MessageKind.Error, MessageCode.IncompatibleTemplate,
                            "specialized struct has no template",
                            st.source, st.nameSpan);
                        continue;
                    }

                    if (st.templateList.GetTypeNumber() != def.mainDef.templateList.GetTypeNumber())
                    {
                        session.diagn.Add(MessageKind.Error, MessageCode.IncompatibleTemplate,
                            "specialized struct is incompatible with template",
                            MessageCaret.Primary(st.source, st.nameSpan),
                            MessageCaret.Primary(def.mainDef.source, def.mainDef.nameSpan));
                        continue;
                    }
                }
            }
        }
    }
}
