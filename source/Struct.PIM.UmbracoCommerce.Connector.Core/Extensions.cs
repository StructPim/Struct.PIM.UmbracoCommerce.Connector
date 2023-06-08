using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Struct.PIM.UmbracoCommerce.Connector.Core
{
    public static class Extensions
    {
        public static List<T> IntersectAll<T>(this IEnumerable<IEnumerable<T>> lists)
        {
            HashSet<T> hashSet = new HashSet<T>(lists.First());
            foreach (var list in lists.Skip(1))
            {
                hashSet.IntersectWith(list);
            }
            return hashSet.ToList();
        }
    }
}
