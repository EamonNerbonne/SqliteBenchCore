using SQLitePCL;
using System;
using System.Data.Common;
using System.Data.SqlClient;
using System.Data.SQLite;
using System.Diagnostics;
using Dapper;

namespace SQLiteBench
{
    class Program
    {
        const int testIterationCount = 100000;
        static void Main(string[] args)
        {
            Batteries_V2.Init();

            PerfTest("Sql Server LocalDb", n => testADO(new SqlConnection("Data Source=(LocalDb)\\MSSQLLocalDB"), n));
            PerfTest("Microsoft.Data.Sqlite.SqliteConnection", n => testADO(new Microsoft.Data.Sqlite.SqliteConnection("Data Source=:memory:;Mode=Memory"), n));
            PerfTest("System.Data.SQLite.SqliteConnection", n => testADO(new SQLiteConnection("Data Source=:memory:;New=True;Synchronous=Off"), n));
            PerfTest("Sql Server LocalDb(Dapper)", n => testDapper(new SqlConnection("Data Source=(LocalDb)\\MSSQLLocalDB"), n));
            PerfTest("System.Data.SQLite.SqliteConnection(Dapper)", n => testDapper(new SQLiteConnection("Data Source=:memory:;New=True;Synchronous=Off"), n));
            PerfTest("SQLitePCL.raw", n => testRAW(n));
        }

        private static void PerfTest(string name, Func<int, int> countToSum)
        {
            var count = testIterationCount;
            countToSum(2);//avoid cold caches
            var sw1 = Stopwatch.StartNew();
            var sum1 = countToSum(1);//startup-cost
            sw1.Stop();
            if (sum1 != 30) throw new Exception("Invalid engine result");

            var sw = Stopwatch.StartNew();
            var sum = countToSum((count+1));
            sw.Stop();
            if (sum != 30*(count+1)) throw new Exception("Invalid engine result");
            var firstRowUS = sw1.Elapsed.TotalMilliseconds * 1000;
            var manyRowsUS = sw.Elapsed.TotalMilliseconds * 1000;
            var extraRowsUS = manyRowsUS - firstRowUS;


            Console.WriteLine($"{name}:\r\n    {extraRowsUS / count:f3}us/query marginal query cost over {count} queries;\r\n    {firstRowUS:f3}us to open and run 1 query;\r\n    overall mean: {manyRowsUS / (count + 1):f3}us/query over {count + 1} queries.\r\n");
        }

        const string query = "select 1 as A, 2 as B, 'test' as C union all select 2,3, 'test2' union all select 3,4,'test71' ";

        struct Row {
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
                for (int i = 0; i < count; i++)
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
                    cmd.CommandText = "select 1,2, 'test' union all select 2,3, 'test2' union all select 3,4,'test71' ";
                    for (int i = 0; i < count; i++)
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
            int sum = 0;

            var rc = raw.sqlite3_open(":memory:", out var db);
            using (db)
            {
                if (rc != raw.SQLITE_OK)
                {
                    throw new Exception();
                }

                var errStmt = raw.sqlite3_prepare_v2(db, "select 1,2, 'test' union all select 2,3, 'test2' union all select 3,4,'test71'", out var stmt);
                using (stmt)
                {
                    if (errStmt != raw.SQLITE_OK)
                    {
                        throw new Exception();
                    }
                    for (int i = 0; i < count; i++)
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
                                sum += col0+col1+col2.Length;
                                //Console.WriteLine($" {col0}  / {col1}  / {col2} ");
                            }
                            else if (code == raw.SQLITE_DONE)
                            {
                                break;
                            }
                            else { throw new Exception(code.ToString()); }
                        }
                        //break;
                    }
                }
                
            }
            return sum;
        }
    }
}
