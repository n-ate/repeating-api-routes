using System;
using System.Collections.Generic;
using System.Linq;

namespace RepeatingApiRoutes.Segments
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
            var parts = new List<SegmentPart>();
            var previousIndex = 0;
            var index = value.IndexOfAny(ValueBounds, 0);
            if (index == -1) parts.Add(new SegmentPart(value, true));
            while (index != -1)
            {
                var isLiteral = value[index] != ValueStop;//if value bounds stop then it is a value segment
                if (index != previousIndex)
                {
                    var part = new SegmentPart(value.Substring(previousIndex, index - previousIndex), isLiteral);
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
        public SegmentPart[] Parts { get; }
        public string RepeatAsKey { get; }
        public string Value { get; }

        public RequestData[] GetMatchingRequestSegments(string[] requestSegments)
        {
            var result = new List<RequestData>();
            var tryToMatchNumber = this.IsRepeating ? requestSegments.Length : 1;
            for (var i = 0; i < tryToMatchNumber; i++)
            {
                if (this.IsMatch(requestSegments[i], out RequestData requestData))
                {
                    result.Add(requestData);
                }
                else break;
            }
            return result.ToArray();
        }

        public bool IsMatch(string segment, out RequestData data)
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
            data = new RequestData(result);
            return isMatch;
        }
    }
}
