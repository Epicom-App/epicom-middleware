using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text.Json.Serialization;

namespace FL.Ebolapp.FunctionsApp.Fetch.Domain.Extensions.TypeExtensions
{
    public static class TypeExtensions
    {
        public static IEnumerable<TSelectorValue> GetAttributeValues<TAttribute, TSelectorValue>(this Type type, Func<TAttribute, TSelectorValue> valueSelector) 
            => type
                .GetProperties(BindingFlags.Public | BindingFlags.Instance)
                .Select(x => x
                    .GetCustomAttributes(false)
                    .OfType<TAttribute>()
                    .FirstOrDefault())
                .Where(x => x != null)
                .Select(valueSelector);
    }
}