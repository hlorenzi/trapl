using System;
using System.Collections.Generic;


namespace Trapl.Infrastructure
{
    public class Template
    {
        public abstract class Parameter
        {
            public virtual bool IsResolved() { return false; }
            public virtual bool IsMatch(Parameter other) { return false; }
            public virtual string GetString(Infrastructure.Session session) { return "???"; }
        }


        public class ParameterType : Parameter
        {
            public Type type;


            public override bool IsResolved()
            {
                return type.IsResolved();
            }


            public override bool IsMatch(Parameter other)
            {
                var otherType = (other as ParameterType);
                if (otherType == null)
                    return false;

                return this.type.IsMatch(otherType.type);
            }


            public override string GetString(Infrastructure.Session session)
            {
                return type.GetString(session);
            }
        }


        public bool unconstrained = false;
        public List<Parameter> parameters = new List<Parameter>();


        public bool IsFullyResolved()
        {
            if (this.unconstrained)
                return false;

            foreach (var param in this.parameters)
            {
                if (!param.IsResolved())
                    return false;
            }
            return true;
        }


        public bool IsMatch(Template other)
        {
            if (this.unconstrained || other.unconstrained)
                return true;

            if (this.parameters.Count != other.parameters.Count)
                return false;

            for (var i = 0; i < this.parameters.Count; i++)
            {
                if (!this.parameters[i].IsMatch(other.parameters[i]))
                    return false;
            }

            return true;
        }


        public string GetString(Infrastructure.Session session)
        {
            if (this.parameters.Count == 0 || this.unconstrained)
                return "";

            var result = "<";
            for (int i = 0; i < this.parameters.Count; i++)
            {
                result += this.parameters[i].GetString(session);
                if (i < this.parameters.Count - 1)
                    result += ", ";
            }
            return result + ">";
        }
    }
}
