using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace RepeatingApiRoutes.Routing.Segments
{
    public class RequestData : ICollection<KeyValuePair<string, object>>
    {
        private List<KeyValuePair<string, object>> value = new List<KeyValuePair<string, object>>();

        public RequestData(IEnumerable<KeyValuePair<string, object>> range)
        {
            this.value = range.ToList();
        }

        public int Count => value.Count;

        public bool IsReadOnly => false;

        public void Add(KeyValuePair<string, object> item)
        {
            value.Add(item);
        }

        public void Clear()
        {
            value.Clear();
        }

        public bool Contains(KeyValuePair<string, object> item)
        {
            return value.Contains(item);
        }

        public void CopyTo(KeyValuePair<string, object>[] array, int arrayIndex)
        {
            value.CopyTo(array, arrayIndex);
        }

        public IEnumerator GetEnumerator()
        {
            return value.GetEnumerator();
        }

        IEnumerator<KeyValuePair<string, object>> IEnumerable<KeyValuePair<string, object>>.GetEnumerator()
        {
            return value.GetEnumerator();
        }

        public bool Remove(KeyValuePair<string, object> item)
        {
            return value.Remove(item);
        }
    }
}
