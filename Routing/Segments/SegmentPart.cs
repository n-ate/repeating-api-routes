namespace RepeatingApiRoutes.Routing.Segments
{
    public class SegmentPart
    {
        public SegmentPart(string value, bool isLiteral)
        {
            this.Value = value;
            this.IsLiteral = isLiteral;
        }

        public bool IsLiteral { get; }
        public SegmentPart Next { get; internal set; }
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
