using System;
using System.Data.Common;
using System.Data.SqlClient;
using System.Data.SQLite;
using System.Diagnostics;
using System.Linq;
using Dapper;
using SQLitePCL;

namespace SqliteBenchCore
{
    static class SqliteBenchCoreProgram
    {
        const int testIterationCount = 100000;
        const int timingRepeats = 10;

        static void Main(string[] args)
        {
            Batteries_V2.Init();

            PerfTest("SQLitePCL.raw", testRAW);
            PerfTest("SQLitePCL.raw (no statement caching)", testRAW_unprepared);
            PerfTest("SQLitePCL.raw (no connection caching)", testRAW_unprepared2);

            PerfTest("System.Data.SQLite.SqliteConnection", n => testADO(new SQLiteConnection("Data Source=:memory:"), n));
            PerfTest("System.Data.SQLite.SqliteConnection(Dapper)", n => testDapper(new SQLiteConnection("Data Source=:memory:"), n));

            PerfTest("Microsoft.Data.Sqlite.SqliteConnection", n => testADO(new Microsoft.Data.Sqlite.SqliteConnection("Data Source=:memory:;Mode=Memory"), n));

            PerfTest("Sql Server LocalDb", n => testADO(new SqlConnection("Data Source=(LocalDb)\\MSSQLLocalDB"), n));
            PerfTest("Sql Server LocalDb(Dapper)", n => testDapper(new SqlConnection("Data Source=(LocalDb)\\MSSQLLocalDB"), n));
            PerfTest("Sql Server LocalDb(Microsoft.Data.SqlClient)", n => testADO(new Microsoft.Data.SqlClient.SqlConnection("Data Source=(LocalDb)\\MSSQLLocalDB"), n));
            PerfTest("Sql Server LocalDb(Microsoft.Data.SqlClient, Dapper)", n => testDapper(new Microsoft.Data.SqlClient.SqlConnection("Data Source=(LocalDb)\\MSSQLLocalDB"), n));
        }

        static int BestTime(int count, Func<int, int> countToSum, int repeats, out double microseconds)
        {
            var times = new double[repeats];
            var sum = 0;
            for (var i = 0; i < repeats; i++)
            {
                var sw = Stopwatch.StartNew();
                sum = countToSum(count);
                sw.Stop();
                times[i] = sw.Elapsed.TotalMilliseconds * 1000;
            }
            Array.Sort(times);
            microseconds = times.Take((repeats + 1) / 2).Average();
            return sum;
        }

        static void PerfTest(string name, Func<int, int> countToSum)
        {
            var count = testIterationCount;
            countToSum(2);//avoid cold caches
            var sum1 = BestTime(1, countToSum, timingRepeats*100, out var openAndOneQueryUS);//startup-cost
            if (sum1 != 30) throw new Exception("Invalid engine result");

            var sum = BestTime(count+1, countToSum, timingRepeats, out var manyQueriesUS);
            if (sum != 30 * (count + 1)) throw new Exception("Invalid engine result");
            var marginalqueryCostUS = (manyQueriesUS - openAndOneQueryUS)/count;

            Console.WriteLine($"{name}:\r\n    {openAndOneQueryUS:f3}us to open and run 1 query;\r\n    {marginalqueryCostUS:f3}us/query marginal query cost over the next {count} queries;\r\n");
        }

        const string query = "select 1 as A, 2 as B, 'test' as C union all select 2,3,'test2' union all select 3,4,'test71'";

        struct Row
        {
            public int A { get; set; }
            public int B { get; set; }
            public string C { get; set; }
        }

        static int testDapper<TConn>(TConn conn, int count) where TConn : DbConnection
        {
            var sum = 0;
            using (conn)
            {
                conn.Open();
                for (var i = 0; i < count; i++)
                {
                    foreach (var row in conn.Query<Row>(query))
                    {
                        sum += row.A + row.B + row.C.Length;
                    }
                }
            }
            return sum;
        }

        static int testADO<TConn>(TConn conn, int count) where TConn : DbConnection
        {
            var sum = 0;
            using (conn)
            {
                conn.Open();
                using (var cmd = conn.CreateCommand())
                {
                    cmd.CommandText = query;
                    for (var i = 0; i < count; i++)
                    {
                        using (var reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                sum += reader.GetInt32(0) + reader.GetInt32(1) + reader.GetString(2).Length;
                            }
                        }
                    }
                }
            }
            return sum;
        }

        static int testRAW(int count)
        {
            var sum = 0;

            var rc = raw.sqlite3_open(":memory:", out var db);
            using (db)
            {
                if (rc != raw.SQLITE_OK)
                {
                    throw new Exception();
                }

                var errStmt = raw.sqlite3_prepare_v2(db, query, out var stmt);
                using (stmt)
                {
                    if (errStmt != raw.SQLITE_OK)
                    {
                        throw new Exception();
                    }

                    for (var i = 0; i < count; i++)
                    {
                        raw.sqlite3_reset(stmt);
                        while (true)
                        {
                            var code = raw.sqlite3_step(stmt);
                            if (code == raw.SQLITE_ROW)
                            {
                                var col0 = raw.sqlite3_column_int(stmt, 0);
                                var col1 = raw.sqlite3_column_int(stmt, 1);
                                var col2 = raw.sqlite3_column_text(stmt, 2);
                                sum += col0 + col1 + col2.utf8_to_string().Length;
                            }
                            else if (code == raw.SQLITE_DONE)
                            {
                                break;
                            }
                            else
                            {
                                throw new Exception(code.ToString());
                            }
                        }
                    }
                }

            }
            return sum;
        }

        static int testRAW_unprepared(int count)
        {
            var sum = 0;

            var rc = raw.sqlite3_open(":memory:", out var db);
            using (db)
            {
                if (rc != raw.SQLITE_OK)
                {
                    throw new Exception();
                }

                for (var i = 0; i < count; i++)
                {
                    var errStmt = raw.sqlite3_prepare_v2(db, query, out var stmt);
                    using (stmt)
                    {
                        if (errStmt != raw.SQLITE_OK)
                        {
                            throw new Exception();
                        }
                        while (true)
                        {
                            var code = raw.sqlite3_step(stmt);
                            if (code == raw.SQLITE_ROW)
                            {
                                var col0 = raw.sqlite3_column_int(stmt, 0);
                                var col1 = raw.sqlite3_column_int(stmt, 1);
                                var col2 = raw.sqlite3_column_text(stmt, 2);
                                sum += col0 + col1 + col2.utf8_to_string().Length;
                            }
                            else if (code == raw.SQLITE_DONE)
                            {
                                break;
                            }
                            else
                            {
                                throw new Exception(code.ToString());
                            }
                        }
                    }
                }

            }
            return sum;
        }


        static int testRAW_unprepared2(int count)
        {
            var sum = 0;

            for (var i = 0; i < count; i++)
            {
                var rc = raw.sqlite3_open(":memory:", out var db);
                using (db)
                {
                    if (rc != raw.SQLITE_OK)
                    {
                        throw new Exception();
                    }

                    var errStmt = raw.sqlite3_prepare_v2(db, query, out var stmt);
                    using (stmt)
                    {
                        if (errStmt != raw.SQLITE_OK)
                        {
                            throw new Exception();
                        }
                        while (true)
                        {
                            var code = raw.sqlite3_step(stmt);
                            if (code == raw.SQLITE_ROW)
                            {
                                var col0 = raw.sqlite3_column_int(stmt, 0);
                                var col1 = raw.sqlite3_column_int(stmt, 1);
                                var col2 = raw.sqlite3_column_text(stmt, 2);
                                sum += col0 + col1 + col2.utf8_to_string().Length;
                            }
                            else if (code == raw.SQLITE_DONE)
                            {
                                break;
                            }
                            else
                            {
                                throw new Exception(code.ToString());
                            }
                        }
                    }
                }

            }
            return sum;
        }
    }
}