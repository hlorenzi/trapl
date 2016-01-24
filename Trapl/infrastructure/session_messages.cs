using System.Collections.Generic;


namespace Trapl.Infrastructure
{
    public partial class Session
    {
        public List<Diagnostics.Message> messages = new List<Diagnostics.Message>();


        public void AddMessage(
            Diagnostics.MessageKind kind, Diagnostics.MessageCode code,
            string text, params Diagnostics.Span[] spans)
        {
            var msg = Diagnostics.Message.Make(code, text, kind, spans);
            //msg.SetContext(this.contextStack);
            this.messages.Add(msg);
        }


        public void AddInnerMessageToLast(
            Diagnostics.MessageKind kind, Diagnostics.MessageCode code,
            string text, params Diagnostics.Span[] spans)
        {
            var msg = Diagnostics.Message.Make(code, text, kind, spans);
            this.messages[this.messages.Count - 1].SetInner(msg);
        }


        public void PrintMessagesToConsole()
        {
            foreach (var message in this.messages)
                message.PrintToConsole(this);
        }
    }
}
