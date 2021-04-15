using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace MPR.Extensions
{
    public static class EnumerableExtensions
    {
        public static IEnumerable<T> DistinctBy<T,K>(this IEnumerable<T> source, Func<T,K> by)
        {
            var seen = new HashSet<K>();
            foreach(var element in source)
            {
                K key = by(element);
                if(!seen.Contains(key))
                {
                    seen.Add(key);
                    yield return element;
                }
            }
        }
    }
} 