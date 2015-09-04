using System.Collections.Generic;


namespace Trapl.Diagnostics
{
    public class Collection
    {
        private List<Message> messages;


        public Collection()
        {
            this.messages = new List<Message>();
        }


        public void Add(Message msg)
        {
            this.messages.Add(msg);
        }


        public void Add(MessageKind kind, MessageCode code, string text, SourceCode source, Diagnostics.Span span)
        {
            this.messages.Add(Message.Make(code, text, kind, MessageCaret.Primary(source, span)));
        }


        public void Add(MessageKind kind, MessageCode code, string text, params MessageCaret[] carets)
        {
            this.messages.Add(Message.Make(code, text, kind, carets));
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
