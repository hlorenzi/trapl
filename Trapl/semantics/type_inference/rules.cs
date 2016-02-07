namespace Trapl.Semantics
{
    public partial class TypeInferencer
    {
        public abstract class Rule
        {

        }


        public class RuleIsSame : Rule
        {
            public int slot1, slot2;


            public static RuleIsSame For(int slot1, int slot2)
            {
                return new RuleIsSame { slot1 = slot1, slot2 = slot2 };
            }
        }


        public class RuleIsFunct : Rule
        {
            public int slot;


            public static RuleIsFunct For(int slot)
            {
                return new RuleIsFunct { slot = slot };
            }
        }
    }
}
