using System.Collections.Generic;


namespace Trapl.Diagnostics
{
    public class Collection
    {
        private List<Message> messages;
        private Stack<Semantics.DeclPatternSubstitution> substitutionContext;


        public Collection()
        {
            this.messages = new List<Message>();
            this.substitutionContext = new Stack<Semantics.DeclPatternSubstitution>();
        }


        public void Add(Message msg)
        {
            if (substitutionContext.Count > 0)
                msg.substitutionContext = substitutionContext.Peek();
            this.messages.Add(msg);
        }


        public void Add(MessageKind kind, MessageCode code, string text, Interface.SourceCode source, Diagnostics.Span span)
        {
            var msg = Message.Make(code, text, kind, MessageCaret.Primary(source, span));
            if (substitutionContext.Count > 0)
                msg.substitutionContext = substitutionContext.Peek();
            this.messages.Add(msg);
        }


        public void Add(MessageKind kind, MessageCode code, string text, params MessageCaret[] carets)
        {
            var msg = Message.Make(code, text, kind, carets);
            if (substitutionContext.Count > 0)
                msg.substitutionContext = substitutionContext.Peek();
            this.messages.Add(msg);
        }


        public void EnterSubstitutionContext(Semantics.DeclPatternSubstitution subst)
        {
            this.substitutionContext.Push(subst);
        }


        public void ExitSubstitutionContext()
        {
            this.substitutionContext.Pop();
        }


        public bool HasNoError()
        {
            foreach (var msg in this.messages)
            {
                if (msg.GetKind() == MessageKind.Error)
                    return false;
            }
            return true;
        }


        public bool HasErrors()
        {
            foreach (var msg in this.messages)
            {
                if (msg.GetKind() == MessageKind.Error)
                    return true;
            }
            return false;
        }


        public bool ContainsMessageWithCode(MessageCode code)
        {
            foreach (var msg in this.messages)
            {
                if (msg.GetCode() == code)
                    return true;
            }
            return false;
        }


        public void PrintToConsole()
        {
            foreach (var msg in messages)
            {
                msg.PrintToConsole();
            }
        }
    }
}
