using System;
using System.Collections;
using System.Collections.Generic;
using System.Data.Linq;
using System.Linq.Expressions;

namespace FastLinq
{
    public static class MyQueries
    {
        private static Dictionary<string, Delegate> compiledQueries =
            new Dictionary<string, Delegate>();

        public static Func<TArg0, TResult> Get<TArg0, TResult>(string key, Expression<Func<TArg0, TResult>> query) where TArg0 : DataContext
        {
            return (Func<TArg0, TResult>)InternalGet(key, () => CompiledQuery.Compile(query));
        }
        public static Func<TArg0, TArg1, TResult> Get<TArg0, TArg1, TResult>(string key, Expression<Func<TArg0, TArg1, TResult>> query) where TArg0 : DataContext
        {
            return (Func<TArg0, TArg1, TResult>)InternalGet(key, () => CompiledQuery.Compile(query));
        }
        public static Func<TArg0, TArg1, TArg2, TResult> Get<TArg0, TArg1, TArg2, TResult>(string key, Expression<Func<TArg0, TArg1, TArg2, TResult>> query) where TArg0 : DataContext
        {
            return (Func<TArg0, TArg1, TArg2, TResult>)InternalGet(key, () => CompiledQuery.Compile(query));
        }
        public static Func<TArg0, TArg1, TArg2, TArg3, TResult> Get<TArg0, TArg1, TArg2, TArg3, TResult>(string key, Expression<Func<TArg0, TArg1, TArg2, TArg3, TResult>> query) where TArg0 : DataContext
        {
            return (Func<TArg0, TArg1, TArg2, TArg3, TResult>)InternalGet(key, () => CompiledQuery.Compile(query));
        }

        private static Delegate InternalGet(string key, Func<Delegate> queryProvider)
        {
            if (string.IsNullOrEmpty(key))
                throw new ArgumentNullException("key");

            var expression = queryProvider.Target.ToString();

            lock ((compiledQueries.Keys as ICollection).SyncRoot)
            {
                Delegate d = null;
                if (compiledQueries.TryGetValue(key, out d))
                    return d;
                else
                {
                    var result = queryProvider();
                    compiledQueries.Add(key, result);
                    return result;
                }
            }
        }
    }
}