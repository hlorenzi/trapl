using System;
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


        public void Add(MessageKind kind, MessageCode code, string text, params Diagnostics.Span[] spans)
        {
            var msg = Message.Make(code, text, kind, spans);
            msg.SetContext(this.contextStack);
            this.messages.Add(msg);
        }


        public void AddInnerToLast(MessageKind kind, MessageCode code, string text, params Diagnostics.Span[] spans)
        {
            var msg = Message.Make(code, text, kind, spans);
            this.messages[this.messages.Count - 1].SetInner(msg);
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


        public void PrintToConsole(Infrastructure.Session session)
        {
            foreach (var msg in messages)
            {
                msg.PrintToConsole(session);
            }
        }
    }
}
