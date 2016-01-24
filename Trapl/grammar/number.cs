
namespace Trapl.Grammar
{
    public static class Integer
    {
        public enum Type
        {
            None,
            Int, Int8, Int16, Int32, Int64,
            UInt, UInt8, UInt16, UInt32, UInt64
        }


        public static bool Parse(string numStr, out int radix, out string value, out Type type)
        {
            radix = 0;
            value = null;
            type = Type.None;

            var index = 0;

            // Parse radix prefix.
            if (numStr.StartsWith("0b") || numStr.StartsWith("0o") || numStr.StartsWith("0x"))
                index += 2;

            if (!ParseRadix(numStr.Substring(0, index), out radix))
                return false;
        
            // Parse value.
            var lastLetterIndex = numStr.Length;
            for (int i = numStr.Length - 1; i >= index; i--)
            {
                if (numStr[i] == 'i' || numStr[i] == 'u')
                {
                    lastLetterIndex = i;
                    break;
                }
            }

            value = CleanValue(numStr.Substring(index, lastLetterIndex - index));

            var possibleDigits = new char[] {
                '0', '1', '2', '3', '4', '5', '6', '7', '8', '9',
                'a', 'b', 'c', 'd', 'e', 'f' };

            foreach (var c in value)
            {
                var isValid = false;
                for (int d = 0; d < radix; d++)
                {
                    if (char.ToLower(c) == possibleDigits[d])
                    {
                        isValid = true;
                        break;
                    }
                }

                if (!isValid)
                    return false;
            }

            // Parse format type suffix.
            if (!ParseType(numStr.Substring(lastLetterIndex, numStr.Length - lastLetterIndex), out type))
                return false;

            return true;
        }


        public static bool ParseRadix(string prefix, out int radix)
        {
            if (prefix == "")
                radix = 10;
            else if (prefix == "0b")
                radix = 2;
            else if (prefix == "0o")
                radix = 8;
            else if (prefix == "0x")
                radix = 16;
            else
            {
                radix = 0;
                return false;
            }

            return true;
        }


        public static bool ParseType(string suffix, out Type type)
        {
            if (suffix == "")
                type = Type.None;
            else if (suffix == "i")
                type = Type.Int;
            else if (suffix == "i8")
                type = Type.Int8;
            else if (suffix == "i16")
                type = Type.Int16;
            else if (suffix == "i32")
                type = Type.Int32;
            else if (suffix == "i64")
                type = Type.Int64;
            else if (suffix == "u")
                type = Type.UInt;
            else if (suffix == "u8")
                type = Type.UInt8;
            else if (suffix == "u16")
                type = Type.UInt16;
            else if (suffix == "u32")
                type = Type.UInt32;
            else if (suffix == "u64")
                type = Type.UInt64;
            else
            {
                type = Type.None;
                return false;
            }

            return true;
        }


        public static string CleanValue(string value)
        {
            return value.Replace("_", "");
        }
    }
}
