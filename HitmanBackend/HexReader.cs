using System.Text;
using System.Text.RegularExpressions;

namespace HitmanBackend
{
    public partial class HexReader
    {
        private static readonly Regex valueRegex = getValueRegex();
        public static string ValuesToString(int[]? arr, int valueSize, int linebreak = 16)
        {
            if (arr == null || arr.Length == 0)
                return "";
            string prev = valueSize switch
            {
                3 or 4 => "dw",
                5 or 6 => "dl",
                _ => "db",
            };
            StringBuilder sb = new();
            string comment;
            int valS = valueSize / 2;
            for (int i = 0; i < arr.Length; i++)
            {
                if (i % linebreak == 0)
                {
                    comment = i != 0 ? $"\t;{((i - 16) * valS):X4}-{(i * valS):X4}" : "";
                    sb.Append($"{comment}\n\t{prev} ");
                }
                else
                    sb.Append(',');

                sb.Append($"${arr[i].ToString($"X{valueSize}")}");
            }
            return sb.ToString();
        }
        public static int[] GetValues(string input)
        {
            MatchCollection matches = valueRegex.Matches(input);
            int[] values = new int[matches.Count];
            string currentValue;

            int i = 0;
            foreach(var match in matches)
            {
                currentValue = match.ToString()![1..];
                values[i] = int.Parse(currentValue, System.Globalization.NumberStyles.HexNumber);
                i++;
            }

            return values;
        }

        [GeneratedRegex("\\$[A-Fa-z0-9]+", RegexOptions.Multiline)]
        private static partial Regex getValueRegex();
    }
}
