using System;
using System.Collections.Generic;
using System.Linq;

namespace RepeatingApiRoutes.Routing
{
    public class Segment
    {
        private const char TemplateRepeat = '~';
        private const char ValueStart = '{';
        private const char ValueStop = '}';
        private static readonly char[] ValueBounds = new char[] { ValueStart, ValueStop };

        public Segment(string value)
        {
            this.Value = value;
            this.IsLiteral = !(value.Contains(ValueStart) || value.Contains(ValueStop));
            this.IsRepeating = value.StartsWith(TemplateRepeat);

            value = value.TrimStart('~');
            if (this.IsRepeating)
            {
                var templateParts = value.Split("as{");
                value = templateParts.First();
                this.RepeatAsKey = templateParts.Last().TrimEnd(ValueStop);
            }
            var parts = new List<Part>();
            var previousIndex = 0;
            var index = value.IndexOfAny(ValueBounds, 0);
            if (index == -1) parts.Add(new Part(value, true));
            while (index != -1)
            {
                var isLiteral = value[index] != ValueStop;//if value bounds stop then it is a value segment
                if (index != previousIndex)
                {
                    var part = new Part(value.Substring(previousIndex, index - previousIndex), isLiteral);
                    if (parts.Any()) parts.Last().Next = part;
                    parts.Add(part);
                }
                previousIndex = index + 1;
                index = previousIndex < value.Length ? value.IndexOfAny(ValueBounds, previousIndex) : -1;
            }
            this.Parts = parts.ToArray();
        }

        public bool IsLiteral { get; }
        public bool IsRepeating { get; }
        public Part[] Parts { get; }
        public string RepeatAsKey { get; }
        public string Value { get; }

        public bool IsMatch(string segment, out KeyValuePair<string, object>[] data)
        {
            var result = new List<KeyValuePair<string, object>>();
            bool isMatch = true;
            if (this.IsLiteral)
            {
                isMatch = segment.Equals(this.Value, StringComparison.OrdinalIgnoreCase);
            }
            else
            {
                var remaining = segment;
                foreach (var part in this.Parts)
                {
                    var matchLength = part.IsMatch(remaining);
                    if (matchLength == -1)
                    {
                        isMatch = false;
                        break;
                    }
                    else
                    {
                        if (!part.IsLiteral)
                        {
                            result.Add(new KeyValuePair<string, object>(part.Value, remaining.Substring(0, matchLength)));
                        }
                        remaining = remaining.Substring(matchLength);
                    }
                }
            }
            data = result.ToArray();
            return isMatch;
        }

        public class Part
        {
            public Part(string value, bool isLiteral)
            {
                this.Value = value;
                this.IsLiteral = isLiteral;
            }

            public bool IsLiteral { get; }
            public Part Next { get; internal set; }
            public string Value { get; }

            public int IsMatch(string segment)
            {
                int length;
                if (this.IsLiteral)//match length is equal to the length of the route part value
                {
                    length = segment.StartsWith(this.Value) ? this.Value.Length : -1;
                }
                else if (this.Next != null)//match length is equal to the length to the next part match
                {
                    length = segment.IndexOf(this.Next.Value);
                }
                else//match length is the entire segment
                {
                    length = segment.Length;
                }
                return length;
            }
        }
    }
}
