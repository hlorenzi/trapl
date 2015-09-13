using System.Collections.Generic;


namespace Trapl.Diagnostics
{
    public class Collection
    {
        private List<Message> messages;
        private Stack<MessageContext> contextStack;


        public Collection()
        {
            this.messages = new List<Message>();
            this.contextStack = new Stack<MessageContext>();
        }


        public void Add(Message msg)
        {
            msg.SetContext(this.contextStack);
            this.messages.Add(msg);
        }


        public void Add(MessageKind kind, MessageCode code, string text, Diagnostics.Span span)
        {
            var msg = Message.Make(code, text, kind, MessageCaret.Primary(span));
            msg.SetContext(this.contextStack);
            this.messages.Add(msg);
        }


        public void Add(MessageKind kind, MessageCode code, string text, params MessageCaret[] carets)
        {
            var msg = Message.Make(code, text, kind, carets);
            msg.SetContext(this.contextStack);
            this.messages.Add(msg);
        }


        public void PushContext(MessageContext ctx)
        {
            this.contextStack.Push(ctx);
        }


        public void PopContext()
        {
            this.contextStack.Pop();
        }


        public bool ContainsNoError()
        {
            foreach (var msg in this.messages)
            {
                if (msg.GetKind() == MessageKind.Error)
                    return false;
            }
            return true;
        }


        public bool ContainsErrors()
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
