using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.Common;
using System.Diagnostics;
using System.Linq;
using FastLinq.Data.EF;
using FastLinq.Data.LS;
using Mjollnir.Data.SqlClient;

namespace FastLinq
{
    class Program
    {
        private static int MAX = 100;

        static void Test(string type, Action testMethod)
        {
            var stopwatch = new Stopwatch();
            stopwatch.Start();
            for(var i=0;i<MAX;i++)
            {
                testMethod();
            }
            stopwatch.Stop();
            Console.WriteLine(" {0} \t\t {1}ms.", type, stopwatch.ElapsedMilliseconds);
        }

        static void TestLinqToSql(int loopCounter)
        {
            LinqToSql.Query<AdventureWorksDataContext>.Clear();
            var options = new { Colors = new List<string> { "Red" }, City = "Bothell", CompanyName = "Bike" };

            var query = new LinqToSql.Query<AdventureWorksDataContext>();
            var exp1a = To.Expression((AdventureWorksDataContext db, int option) => 
                from m in db.Product where options.Colors.Contains(m.Color) select m);
            var exp1b = To.Expression((AdventureWorksDataContext db, List<string> option) => 
                from m in db.Product where option.Contains(m.Color) select m);
            var exp2 = To.Expression((AdventureWorksDataContext db, string option) => 
                from m in db.Address where m.City == option select m);
            var exp3 = To.Expression((AdventureWorksDataContext db, string option) => 
                from m in db.Customer where m.CompanyName.StartsWith(option) select m);

            using (var connection = new System.Data.SqlClient.SqlConnection(ConfigurationManager.ConnectionStrings["AdventureWorks"].ConnectionString))
            using (var context = new AdventureWorksDataContext(connection))
            {
                context.ObjectTrackingEnabled = false;
                context.DeferredLoadingEnabled = false;
                Test("Ad hoc", () =>
                {
                    (from m in context.Product 
                     where options.Colors.Contains(m.Color) 
                     select m).FirstOrDefault();
                    (from m in context.Product 
                     where new List<string> { "Red" }.Contains(m.Color) 
                     select m).FirstOrDefault();
                    (from m in context.Address 
                     where m.City == options.City 
                     select m).FirstOrDefault();
                    (from m in context.Customer 
                     where m.CompanyName.StartsWith(options.CompanyName) 
                     select m).FirstOrDefault();
                });

                Test("Expression", () =>
                {
                    exp1a.Compile()(context, 0).FirstOrDefault();
                    exp1b.Compile()(context, new List<string> { "Red" }).FirstOrDefault();
                    exp2.Compile()(context, options.City).FirstOrDefault();
                    exp3.Compile()(context, options.CompanyName).FirstOrDefault();
                });

                Test("Fast 1", () =>
                {
                    query.Fast(exp1a)(context, 1).FirstOrDefault();
                    query.Fast(exp1b)(context, new List<string> { "Red" }).FirstOrDefault();
                    query.Fast(exp2)(context, options.City).FirstOrDefault();
                    query.Fast(exp3)(context, options.CompanyName).FirstOrDefault();
                });

                Test("Fast 2", () =>
                {
                    query.Fast(
                        (AdventureWorksDataContext db, int option) => 
                            from m in db.Product where options.Colors.Contains(m.Color) select m
                    )(context, 2).FirstOrDefault();
                    query.Fast(
                        (AdventureWorksDataContext db, List<string> option) => 
                            from m in db.Product where option.Contains(m.Color) select m
                    )(context, new List<string> { "Red" }).FirstOrDefault();
                    query.Fast(
                        (AdventureWorksDataContext db, string option) => 
                            from m in db.Address where m.City == option select m
                    )(context, "Bothell").FirstOrDefault();
                    query.Fast(
                        (AdventureWorksDataContext db, string option) => 
                            from m in db.Customer where m.CompanyName.StartsWith(option) select m
                    )(context, "Bike").FirstOrDefault();
                });
            }
        }

        static void TestLinqToSqlCache(int loopCounter)
        {
            LinqToSql.Query<AdventureWorksDataContext>.Clear();
            var options = new { Colors = new List<string> { "Red" }, City = "Bothell", CompanyName = "Bike" };

            var query = new LinqToSql.Query<AdventureWorksDataContext>();
            var exp1a = To.Expression((AdventureWorksDataContext db, int option) =>
                from m in db.Product where options.Colors.Contains(m.Color) select m);
            var exp1b = To.Expression((AdventureWorksDataContext db, List<string> option) =>
                from m in db.Product where option.Contains(m.Color) select m);
            var exp1c = To.Expression((AdventureWorksDataContext db, string[] option) =>
                from m in db.Product where option.Contains(m.Color) select m);

            using (var connection = new System.Data.SqlClient.SqlConnection(ConfigurationManager.ConnectionStrings["AdventureWorks"].ConnectionString))
            using (var context = new AdventureWorksDataContext(connection))
            {
                context.ObjectTrackingEnabled = false;
                context.DeferredLoadingEnabled = false;
                Test("Cache A", () =>
                {
                    options.Colors.Clear(); options.Colors.Add("Black");
                    var black = query.Fast(exp1a)(context, 11).FirstOrDefault();

                    options.Colors.Clear(); options.Colors.Add("Blue");
                    var blue = query.Fast(exp1a)(context, 12).FirstOrDefault();

                    options.Colors.Clear(); options.Colors.Add("White");
                    var white = query.Fast(exp1a)(context, 13).FirstOrDefault();

                    if (black.Color != "Black" || blue.Color != "Blue" || white.Color != "White")
                        throw new Exception("Bad cache!");
                });

                Test("Cache B", () =>
                {
                    var black = query.Fast(exp1b)(context, new List<string> { "Black" }).FirstOrDefault();
                    var blue = query.Fast(exp1b)(context, new List<string> { "Blue" }).FirstOrDefault();
                    var white = query.Fast(exp1b)(context, new List<string> { "White" }).FirstOrDefault();

                    if (black.Color != "Black" || blue.Color != "Blue" || white.Color != "White")
                        throw new Exception("Bad cache!");
                });

                Test("Cache C", () =>
                {
                    var black = query.Fast(exp1c)(context, new[] { "Black" }).FirstOrDefault();
                    var blue = query.Fast(exp1c)(context, new[] { "Blue" }).FirstOrDefault();
                    var white = query.Fast(exp1c)(context, new[] { "White" }).FirstOrDefault();

                    if (black.Color != "Black" || blue.Color != "Blue" || white.Color != "White")
                        throw new Exception("Bad cache!");
                });
            }
        }
        class TestExecuteReader : IExecuteReaderProvider
        {
            Dictionary<int, DataTable> _cache = new Dictionary<int, DataTable>();
            public DbDataReader ExecuteReader(DbCommand command, CommandBehavior behavior)
            {
                var key = command.CommandText;
                foreach (DbParameter parameter in command.Parameters)
                {
                    key += ":" + string.Format("{0}={1}", parameter.ParameterName, parameter.Value);
                }
                var hashCode = key.GetHashCode();
                if (!_cache.ContainsKey(hashCode))
                {
                    var table = new DataTable();
                    table.Load(command.ExecuteReader(behavior));

                    _cache[hashCode] = table;
                }

                return new DataTableReader(_cache[hashCode]);
            }
        }

        static void TestLinqToSql2(int loopCounter)
        {
            LinqToSql.Query<AdventureWorksDataContext>.Clear();
            var options = new { Colors = new List<string> { "Red" }, City = "Bothell", CompanyName = "Bike" };

            var query = new LinqToSql.Query<AdventureWorksDataContext>();
            var exp1a = To.Expression((AdventureWorksDataContext db, int option) => from m in db.Product where options.Colors.Contains(m.Color) select m);
            var exp1b = To.Expression((AdventureWorksDataContext db, List<string> option) => from m in db.Product where option.Contains(m.Color) select m);
            var exp1c = To.Expression((AdventureWorksDataContext db, string[] option) => from m in db.Product where option.Contains(m.Color) select m);
            var exp2 = To.Expression((AdventureWorksDataContext db, string option) => from m in db.Address where m.City == option select m);
            var exp3 = To.Expression((AdventureWorksDataContext db, string option) => from m in db.Customer where m.CompanyName.StartsWith(option) select m);

            var reader = new TestExecuteReader();
            using(var connection = new SqlConnection(ConfigurationManager.ConnectionStrings["AdventureWorks"].ConnectionString, reader))
            using(var context = new AdventureWorksDataContext(connection))
            {
                context.ObjectTrackingEnabled = false;
                context.DeferredLoadingEnabled = false;
                Test("Ad hoc", () =>
                {
                    (from m in context.Product where options.Colors.Contains(m.Color) select m).ToArray();
                    (from m in context.Product where new List<string> { "Red" }.Contains(m.Color) select m).ToArray();
                    (from m in context.Address where m.City == options.City select m).ToArray();
                    (from m in context.Customer where m.CompanyName.StartsWith(options.CompanyName) select m).ToArray();
                });

                Test("Expression", () =>
                {
                    exp1a.Compile()(context, 0).ToArray();
                    exp1b.Compile()(context, new List<string> { "Red" }).ToArray();
                    exp2.Compile()(context, options.City).ToArray();
                    exp3.Compile()(context, options.CompanyName).ToArray();
                });

                Test("Fast 1", () =>
                {
                    query.Fast(exp1a)(context, 1).ToArray();
                    query.Fast(exp1b)(context, new List<string> { "Red" }).ToArray();
                    query.Fast(exp2)(context, options.City).ToArray();
                    query.Fast(exp3)(context, options.CompanyName).ToArray();
                });

                Test("Fast 2", () =>
                {
                    query.Fast(
                        (AdventureWorksDataContext db, int option) => from m in db.Product where options.Colors.Contains(m.Color) select m
                    )(context, 2).ToArray();
                    query.Fast(
                        (AdventureWorksDataContext db, List<string> option) => from m in db.Product where option.Contains(m.Color) select m
                    )(context, new List<string> { "Red" }).ToArray();
                    query.Fast(
                        (AdventureWorksDataContext db, string option) => from m in db.Address where m.City == option select m
                    )(context, "Bothell").ToArray();
                    query.Fast(
                        (AdventureWorksDataContext db, string option) => from m in db.Customer where m.CompanyName.StartsWith(option) select m
                    )(context, "Bike").ToArray();
                });

                Test("Cache A", () =>
                {
                    options.Colors.Clear(); options.Colors.Add("Black");
                    var black = query.Fast(exp1a)(context, 11).FirstOrDefault();

                    options.Colors.Clear(); options.Colors.Add("Blue");
                    var blue = query.Fast(exp1a)(context, 12).FirstOrDefault();

                    options.Colors.Clear(); options.Colors.Add("White");
                    var white = query.Fast(exp1a)(context, 13).FirstOrDefault();

                    if (black.Color != "Black" || blue.Color != "Blue" || white.Color != "White")
                        throw new Exception("Bad cache!");
                });

                Test("Cache B", () =>
                {
                    var black = query.Fast(exp1b)(context, new List<string> { "Black" }).ToArray();
                    var blue = query.Fast(exp1b)(context, new List<string> { "Blue" }).ToArray();
                    var white = query.Fast(exp1b)(context, new List<string> { "White" }).ToArray();

                    if (black.First().Color != "Black" || blue.First().Color != "Blue" || white.First().Color != "White")
                        throw new Exception("Bad cache!");
                });

                Test("Cache C", () =>
                {
                    var black = query.Fast(exp1c)(context, new[] { "Black" }).ToArray();
                    var blue = query.Fast(exp1c)(context, new[] { "Blue" }).ToArray();
                    var white = query.Fast(exp1c)(context, new[] { "White" }).ToArray();

                    if (black.First().Color != "Black" || blue.First().Color != "Blue" || white.First().Color != "White")
                        throw new Exception("Bad cache!");
                });
            }
        }
        /// <summary>
        /// Sneak Preview: Entity Framework 5.0 Performance Improvements - ADO.NET team blog - Site Home - MSDN Blogs 
        /// http://blogs.msdn.com/b/adonet/archive/2012/02/14/sneak-preview-entity-framework-5-0-performance-improvements.aspx
        /// </summary>
        /// <param name="loopCounter"></param>
        static void TestLinqToEntity(int loopCounter)
        {
            var options = new {Colors = new List<string> {"Red"}, City = "Bothell", CompanyName = "Bike"};

            var query = new LinqToEntity.Query<AdventureWorksEntities>();
            var exp1a = To.Expression((AdventureWorksEntities db, int option) => from m in db.Product where options.Colors.Contains(m.Color) select m);
            var exp1b = To.Expression((AdventureWorksEntities db, List<string> option) => from m in db.Product where option.Contains(m.Color) select m);
            var exp1c = To.Expression((AdventureWorksEntities db, string[] option) => from m in db.Product where option.Contains(m.Color) select m);
            var exp2 = To.Expression((AdventureWorksEntities db, string option) => from m in db.Address where m.City == option select m);
            var exp3 = To.Expression((AdventureWorksEntities db, string option) => from m in db.Customer where m.CompanyName.StartsWith(option) select m);

            var context = new AdventureWorksEntities();
            //context.ContextOptions.LazyLoadingEnabled = false;
            //context.ContextOptions.ProxyCreationEnabled = false;

            Test("Ad hoc", () =>
            {
                (from m in context.Product where options.Colors.Contains(m.Color) select m).FirstOrDefault();
                (from m in context.Product where new List<string> { "Red" }.Contains(m.Color) select m).FirstOrDefault();
                (from m in context.Address where m.City == options.City select m).FirstOrDefault();
                (from m in context.Customer where m.CompanyName.StartsWith(options.CompanyName) select m).FirstOrDefault();
            });

            Test("Expression", () =>
            {
                exp1a.Compile()(context, 0).FirstOrDefault();
                exp1b.Compile()(context, new List<string> { "Red" }).FirstOrDefault();
                exp2.Compile()(context, options.City).FirstOrDefault();
                exp3.Compile()(context, options.CompanyName).FirstOrDefault();
            });

            Test("Fast 1", () =>
            {
                query.Fast(exp1a)(context, 0).FirstOrDefault();
                query.Fast(exp1b)(context, new List<string> { "Red" }).FirstOrDefault();
                query.Fast(exp2)(context, options.City).FirstOrDefault();
                query.Fast(exp3)(context, options.CompanyName).FirstOrDefault();
            });

            Test("Fast 2", () =>
            {
                query.Fast(
                    (AdventureWorksEntities db, int option) => from m in db.Product where options.Colors.Contains(m.Color) select m
                )(context, 0).FirstOrDefault();
                query.Fast(
                    (AdventureWorksEntities db, List<string> option) => from m in db.Product where option.Contains(m.Color) select m
                )(context, new List<string> { "Red" }).FirstOrDefault();
                query.Fast(
                    (AdventureWorksEntities db, string option) => from m in db.Address where m.City == option select m
                )(context, "Bothell").FirstOrDefault();
                query.Fast(
                    (AdventureWorksEntities db, string option) => from m in db.Customer where m.CompanyName.StartsWith(option) select m
                )(context, "Bike").FirstOrDefault();
            });

            // 束縛の内部処理が不明...
            // LSとも違う実装で。キャプチャしたときのExpressionは何か違う！
            Test("Cache A", () =>
            {
                options.Colors.Clear(); options.Colors.Add("Black");
                var black = query.Fast(exp1a)(context, loopCounter * 10 + 1).FirstOrDefault();

                options.Colors.Clear(); options.Colors.Add("Blue");
                var blue = query.Fast(exp1a)(context, loopCounter * 10 + 2).FirstOrDefault();

                options.Colors.Clear(); options.Colors.Add("White");
                var white = query.Fast(exp1a)(context, loopCounter * 10 + 3).FirstOrDefault();

                if (black.Color != "Black" || blue.Color != "Blue" || white.Color != "White")
                    throw new Exception("Bad cache!");
            });

            Test("Cache B", () =>
            {
                var black = query.Fast(exp1b)(context, new List<string> { "Black" }).FirstOrDefault();
                var blue = query.Fast(exp1b)(context, new List<string> { "Blue" }).FirstOrDefault();
                var white = query.Fast(exp1b)(context, new List<string> { "White" }).FirstOrDefault();

                if (black.Color != "Black" || blue.Color != "Blue" || white.Color != "White")
                    throw new Exception("Bad cache!");
            });

            Test("Cache C", () =>
            {
                var black = query.Fast(exp1c)(context, new[] { "Black" }).FirstOrDefault();
                var blue = query.Fast(exp1c)(context, new[] { "Blue" }).FirstOrDefault();
                var white = query.Fast(exp1c)(context, new[] { "White" }).FirstOrDefault();

                if (black.Color != "Black" || blue.Color != "Blue" || white.Color != "White")
                    throw new Exception("Bad cache!");
            });
        }

        class TestCQ
        {
            public void Test1()
            {
                var context = new AdventureWorksDataContext();
                var cq = MyQueries.Get("Test1", 
                    (AdventureWorksDataContext db) =>
                    from m in db.Product where new[] { "Red" }.Contains(m.Color) select m);
                try
                {
                    Console.WriteLine("Test1:" + cq(context).Count());
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex);
                }
            }

            public void Test2()
            {
                var context = new AdventureWorksDataContext();
                var localArray = new[] { "Red" };
                var cq = MyQueries.Get("Test2", 
                    (AdventureWorksDataContext db) =>
                    from m in db.Product where localArray.Contains(m.Color) select m);
                try
                {
                    Console.WriteLine("Test2:" + cq(context).Count());
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex);
                }
            }

            public void Test3(string[] array)
            {
                var context = new AdventureWorksDataContext();
                var cq = MyQueries.Get("Test3", 
                    (AdventureWorksDataContext db) =>
                    from m in db.Product where array.Contains(m.Color) select m);
                try
                {
                    Console.WriteLine("Test3:" + cq(context).Count());
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex);
                }
            }

            public void Test4(string[] array)
            {
                var context = new AdventureWorksDataContext();
                var cq = MyQueries.Get("Test4", 
                    (AdventureWorksDataContext db, string[] option) =>
                    from m in db.Product where option.Contains(m.Color) select m);
                try
                {
                    Console.WriteLine("Test4:" + cq(context,array).Count());
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex);
                }
            }
        }
        static void Main(string[] args)
        {
            //var cq = new TestCQ();
            //cq.Test1();
            //cq.Test2();
            //cq.Test3(new[] { "Black", "White" });
            //cq.Test3(new[] { "Blue" });
            //cq.Test4(new[] { "Black", "White" });
            //cq.Test4(new[] { "Blue" });

            //Console.ReadLine();
            //return;

            for (var j = 0; j < 5; j++)
            {
                Console.WriteLine("LINQ to SQL {0} * {1}", MAX, j + 1);
                {
                    Console.WriteLine("-Expression cache only");
                    TestLinqToSql(j);
                    TestLinqToSqlCache(j);
                    Console.WriteLine("-With data cache");
                    TestLinqToSql2(j);
                }
                
#if RUNNING_ON_4
                Console.WriteLine("LINQ to Entity 4 {0} * {1}", MAX, j + 1);
                {
                    TestLinqToEntity(j);
                }
#endif
            }

            Console.ReadLine();
        }
    }
}
