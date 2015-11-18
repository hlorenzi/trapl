
namespace Trapl.Grammar
{
    public static class Number
    {
        public static void GetParts(string numStr, out string prefix, out string value, out string suffix)
        {
            var possibleDigits = new char[] {
                '0', '1', '2', '3', '4', '5', '6', '7', '8', '9',
                'a', 'b', 'c', 'd', 'e', 'f' };

            var index = 0;

            if (numStr.StartsWith("0b") || numStr.StartsWith("0o") || numStr.StartsWith("0x"))
                index += 2;

            prefix = numStr.Substring(0, index);

            var lastLetterIndex = numStr.Length;
            for (int i = numStr.Length - 1; i >= index; i--)
            {
                if (numStr[i] == 'i' || numStr[i] == 'u')
                {
                    lastLetterIndex = i;
                    break;
                }
            }

            value = numStr.Substring(index, lastLetterIndex - index);
            suffix = numStr.Substring(lastLetterIndex, numStr.Length - lastLetterIndex);
        }


        public static int GetBase(string prefix)
        {
            if (prefix == "")
                return 10;
            else if (prefix == "0b")
                return 2;
            else if (prefix == "0o")
                return 8;
            else if (prefix == "0x")
                return 16;
            else
                return 0;
        }


        public static string GetValueWithoutSpecials(string value)
        {
            return value.Replace("_", "");
        }


        public static bool Validate(string prefix, string value, string suffix)
        {
            int numBase;

            if (prefix == "")
                numBase = 10;
            else if (prefix == "0b")
                numBase = 2;
            else if (prefix == "0o")
                numBase = 8;
            else if (prefix == "0x")
                numBase = 16;
            else
                return false;

            var possibleDigits = new char[] {
                '0', '1', '2', '3', '4', '5', '6', '7', '8', '9',
                'a', 'b', 'c', 'd', 'e', 'f' };

            foreach (var c in value)
            {
                if (c == '_')
                    continue;

                var isValid = false;
                for (int d = 0; d < numBase; d++)
                {
                    if (c == possibleDigits[d] || c == char.ToUpper(possibleDigits[d]))
                    {
                        isValid = true;
                        break;
                    }
                }

                if (!isValid)
                    return false;
            }

            if (suffix != "" &&
                suffix != "i" &&
                suffix != "i8" &&
                suffix != "i16" &&
                suffix != "i32" &&
                suffix != "i64" &&
                suffix != "u" &&
                suffix != "u8" &&
                suffix != "u16" &&
                suffix != "u32" &&
                suffix != "u64")
                return false;

            return true;
        }
    }
}
