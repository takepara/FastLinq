 
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Linq.Expressions;
using System.Threading;

namespace FastLinq
{
	public static class To
    {
					
        public static Expression<Func<TR>> Expression<TR>(Expression<Func<TR>> expression)
        {
            return expression;
        }
					
        public static Expression<Func<T1,TR>> Expression<T1,TR>(Expression<Func<T1,TR>> expression)
        {
            return expression;
        }
					
        public static Expression<Func<T1,T2,TR>> Expression<T1,T2,TR>(Expression<Func<T1,T2,TR>> expression)
        {
            return expression;
        }
					
        public static Expression<Func<T1,T2,T3,TR>> Expression<T1,T2,T3,TR>(Expression<Func<T1,T2,T3,TR>> expression)
        {
            return expression;
        }
	    }

    public abstract class QueryBase<TDB>
    {
        private static Dictionary<string, object> _cachedQuery = new Dictionary<string, object>();
        private static ReaderWriterLockSlim _locked = new ReaderWriterLockSlim();

					
        public abstract Func<TDB, TR> Compiled<TR>(Expression<Func<TDB, TR>> query);
					
        public abstract Func<TDB, T1,TR> Compiled<T1,TR>(Expression<Func<TDB, T1,TR>> query);
					
        public abstract Func<TDB, T1,T2,TR> Compiled<T1,T2,TR>(Expression<Func<TDB, T1,T2,TR>> query);
					
        public abstract Func<TDB, T1,T2,T3,TR> Compiled<T1,T2,T3,TR>(Expression<Func<TDB, T1,T2,T3,TR>> query);
	
		public static void Clear()
		{
			_cachedQuery.Clear();
		}

        private int GetOptionHash<T>(T option)
        {
            var values = option as IEnumerable;
            if (values != null)
            {
                return string.Join("\r\n", values.OfType<object>().Select(v => v.ToString()).ToArray()).GetHashCode();
            }
            return option.GetHashCode();
        }

		private object GetCache(string key, Func<object> compiledQueryFunctor)
		{
            _locked.EnterUpgradeableReadLock();
            try
            {
                object cachedQuery;
                if (_cachedQuery.TryGetValue(key, out cachedQuery))
                    return cachedQuery;

                var compiedQuery = compiledQueryFunctor();
                try
                {
                    _locked.EnterWriteLock();
                    if (!_cachedQuery.ContainsKey(key))
                    {
                        _cachedQuery[key] = compiedQuery;
                    }
                }
                finally
                {
                    _locked.ExitWriteLock();
                }
                return compiedQuery;
            }
            finally
            {
                _locked.ExitUpgradeableReadLock();
            }
        }

									
		public Func<TDB, TR> Fast<TR>(Expression<Func<TDB, TR>> query) where TR : class
        {
            Func<TDB, TR> wrapper = (context) =>
            {
			
				var key = string.Format("{0}", query.ToString().GetHashCode());
			
				var compiledQuery = GetCache(key, ()=>Compiled(query));
                return (compiledQuery as Func<TDB, TR>)(context);
            };

            return wrapper;
        }
									
		public Func<TDB, T1,TR> Fast<T1,TR>(Expression<Func<TDB, T1,TR>> query) where TR : class
        {
            Func<TDB, T1,TR> wrapper = (context,option1) =>
            {
							
				var replaces = new Dictionary<string, Expression>{
					{query.Parameters[1].Name, Expression.Constant(option1)},
									};
			
                query = new ParameterToConstantVisitor().Replace(query, replaces) as Expression<Func<TDB, T1,TR>>;
				var key = string.Format("{0}:{1}", query.ToString().GetHashCode(),GetOptionHash(option1));
			
				var compiledQuery = GetCache(key, ()=>Compiled(query));
                return (compiledQuery as Func<TDB, T1,TR>)(context,option1);
            };

            return wrapper;
        }
									
		public Func<TDB, T1,T2,TR> Fast<T1,T2,TR>(Expression<Func<TDB, T1,T2,TR>> query) where TR : class
        {
            Func<TDB, T1,T2,TR> wrapper = (context,option1,option2) =>
            {
							
				var replaces = new Dictionary<string, Expression>{
					{query.Parameters[1].Name, Expression.Constant(option1)},
					{query.Parameters[2].Name, Expression.Constant(option2)},
									};
			
                query = new ParameterToConstantVisitor().Replace(query, replaces) as Expression<Func<TDB, T1,T2,TR>>;
				var key = string.Format("{0}:{1}:{2}", query.ToString().GetHashCode(),GetOptionHash(option1),GetOptionHash(option2));
			
				var compiledQuery = GetCache(key, ()=>Compiled(query));
                return (compiledQuery as Func<TDB, T1,T2,TR>)(context,option1,option2);
            };

            return wrapper;
        }
									
		public Func<TDB, T1,T2,T3,TR> Fast<T1,T2,T3,TR>(Expression<Func<TDB, T1,T2,T3,TR>> query) where TR : class
        {
            Func<TDB, T1,T2,T3,TR> wrapper = (context,option1,option2,option3) =>
            {
							
				var replaces = new Dictionary<string, Expression>{
					{query.Parameters[1].Name, Expression.Constant(option1)},
					{query.Parameters[2].Name, Expression.Constant(option2)},
					{query.Parameters[3].Name, Expression.Constant(option3)},
									};
			
                query = new ParameterToConstantVisitor().Replace(query, replaces) as Expression<Func<TDB, T1,T2,T3,TR>>;
				var key = string.Format("{0}:{1}:{2}:{3}", query.ToString().GetHashCode(),GetOptionHash(option1),GetOptionHash(option2),GetOptionHash(option3));
			
				var compiledQuery = GetCache(key, ()=>Compiled(query));
                return (compiledQuery as Func<TDB, T1,T2,T3,TR>)(context,option1,option2,option3);
            };

            return wrapper;
        }
	    }
}
