using SQLitePCL;
using System;
using System.Data.Common;
using System.Data.SqlClient;
using System.Data.SQLite;
using System.Diagnostics;

namespace SQLiteBench
{
    class Program
    {
        static void Main(string[] args)
        {
            const int count = 100000;
            PerfTest(count, n => testADO(new SqlConnection("Data Source=(LocalDb)\\MSSQLLocalDB"), n));
            PerfTest(count, n => testADO(new Microsoft.Data.Sqlite.SqliteConnection("Data Source=:memory:;Mode=Memory"), n));
            PerfTest(count, n => testADO(new SQLiteConnection("Data Source=:memory:;New=True;Synchronous=Off"), n));
            PerfTest(count, n => testRAW(n));
        }

        private static void PerfTest(int count, Func<int, (int, TimeSpan)> countToSum)
        {
            var sw0 = Stopwatch.StartNew();
            var (sum2, elInner0) = countToSum(2);//avoid cold caches
            sw0.Stop();
            if (sum2 != 60) throw new Exception("Invalid engine result");

            var sw = Stopwatch.StartNew();
            var (sum, elInner) = countToSum(count);
            sw.Stop();
            if (sum != 30*count) throw new Exception("Invalid engine result");
            Console.WriteLine($"{sw.Elapsed.TotalMilliseconds / count * 1000}us({elInner.TotalMilliseconds / count * 1000}) per query over {count} queries; {sw0.Elapsed.TotalMilliseconds * 1000}us({elInner0.TotalMilliseconds * 1000}) for first 2 queries.");
        }

        static (int, TimeSpan) testADO<TConn>(TConn conn, int count) where TConn : DbConnection
        {
            var innerTime = new Stopwatch();
            var sum = 0;
            using (conn)
            {
                conn.Open();
                using (var cmd = conn.CreateCommand())
                {
                    cmd.CommandText = "select 1,2, 'test' union all select 2,3, 'test2' union all select 3,4,'test71' ";
                    innerTime.Start();
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
                    innerTime.Stop();
                }
            }
            return (sum, innerTime.Elapsed);
        }

        static (int, TimeSpan) testRAW(int count)
        {
            var innerTime = new Stopwatch();

            int sum = 0;
            Batteries_V2.Init();

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
                    innerTime.Start();
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
                    innerTime.Stop();
                }
                
            }
            return (sum, innerTime.Elapsed);
        }
    }
}
