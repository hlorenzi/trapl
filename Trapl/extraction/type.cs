namespace Trapl.Extraction
{
    public abstract class Type
    {
        public abstract string GetString();
    }


    public class TypeError : Type
    {
        public override string GetString()
        {
            return "<error>";
        }
    }
}
