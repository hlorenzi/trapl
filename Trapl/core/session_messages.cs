using System.Collections.Generic;


namespace Trapl.Core
{
    public partial class Session
    {
        private List<Diagnostics.Message> messages = new List<Diagnostics.Message>();
        private Stack<Diagnostics.MessageContext> contextStack = new Stack<Diagnostics.MessageContext>();


        public void AddMessage(
            Diagnostics.MessageKind kind, Diagnostics.MessageCode code,
            string text, params Diagnostics.Span[] spans)
        {
            var msg = Diagnostics.Message.Make(code, text, kind, spans);
            msg.SetContext(this.contextStack);
            this.messages.Add(msg);
        }


        public void AddInnerMessageToLast(
            Diagnostics.MessageKind kind, Diagnostics.MessageCode code,
            string text, params Diagnostics.Span[] spans)
        {
            var msg = Diagnostics.Message.Make(code, text, kind, spans);
            this.messages[this.messages.Count - 1].SetInner(msg);
        }


        public void PushContext(string text, Diagnostics.Span span)
        {
            this.contextStack.Push(new Diagnostics.MessageContext(text, span));
        }


        public void PopContext()
        {
            this.contextStack.Pop();
        }


        public void PrintMessagesToConsole()
        {
            foreach (var message in this.messages)
                message.PrintToConsole(this);
        }


        public bool HasInternalErrors()
        {
            foreach (var message in this.messages)
            {
                if (message.GetKind() == Diagnostics.MessageKind.Internal)
                    return true;
            }
            return false;
        }


        public bool HasErrors()
        {
            foreach (var message in this.messages)
            {
                if (message.GetKind() == Diagnostics.MessageKind.Error)
                    return true;
            }
            return false;
        }


        public bool HasMessagesWithCode(Diagnostics.MessageCode code)
        {
            foreach (var message in this.messages)
            {
                if (message.GetCode() == code)
                    return true;
            }
            return false;
        }
    }
}
