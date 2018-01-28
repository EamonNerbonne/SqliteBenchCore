A simple test to evaluate the overhead of various ways of accessing sqlite.

The test opens connections to an in-memory sqlite database and uses no tables to ensure the bottleneck is not the disk.
The query `select 1 as A, 2 as B, 'test' as C union all select 2,3,'test2' union all select 3,4,'test71'` is run repeatedly.

As a baseline, the performance of Sql Server LocalDb in the same test is also included (which is not an in-proc db, and is thus expected to incur more overhead).


Sql Server LocalDb:
    37.285us/query marginal query cost over 100000 queries;
    2073.100us to open and run 1 query;
    overall mean: 37.305us/query over 100001 queries.

Microsoft.Data.Sqlite.SqliteConnection:
    4.681us/query marginal query cost over 100000 queries;
    118.900us to open and run 1 query;
    overall mean: 4.682us/query over 100001 queries.

System.Data.SQLite.SqliteConnection:
    2.466us/query marginal query cost over 100000 queries;
    1440.700us to open and run 1 query;
    overall mean: 2.480us/query over 100001 queries.

Sql Server LocalDb(Dapper):
    42.243us/query marginal query cost over 100000 queries;
    229.900us to open and run 1 query;
    overall mean: 42.244us/query over 100001 queries.

System.Data.SQLite.SqliteConnection(Dapper):
    9.835us/query marginal query cost over 100000 queries;
    637.900us to open and run 1 query;
    overall mean: 9.842us/query over 100001 queries.

SQLitePCL.raw:
    0.658us/query marginal query cost over 100000 queries;
    51.700us to open and run 1 query;
    overall mean: 0.659us/query over 100001 queries.
