A simple test to evaluate the overhead of various ways of accessing sqlite.

The test opens connections to an in-memory sqlite database and uses no tables to ensure the bottleneck is not the disk.
The query `select 1 as A, 2 as B, 'test' as C union all select 2,3,'test2' union all select 3,4,'test71'` is run in batches of 100000, and that's repeated 10 times.  The mean duration of the fastest 5 batches is reported.

As a baseline, the performance of Sql Server LocalDb in the same test is also included (which is not an in-proc db, and is thus expected to incur more overhead).

Results from January 2018 on an i7-4770k using .net core 2.0 (net471 appears slightly slower overall), dapper 1.60.6, Microsoft.Data.Sqlite.Core 3.0.0-preview6.19304.10, Microsoft.Data.SqlClient 1.0.19128.1-Preview, SQLitePCLRaw.bundle_e_sqlite3 2.0.0-pre20190628101813, System.Data.SQLite.Core 1.0.111:

```
SQLitePCL.raw:
    59.923us to open and run 1 query;
    0.659us/query marginal query cost over the next 100000 queries;

SQLitePCL.raw (no statement caching):
    41.449us to open and run 1 query;
    4.167us/query marginal query cost over the next 100000 queries;

System.Data.SQLite.SqliteConnection:
    524.112us to open and run 1 query;
    2.481us/query marginal query cost over the next 100000 queries;

System.Data.SQLite.SqliteConnection(Dapper):
    523.559us to open and run 1 query;
    9.746us/query marginal query cost over the next 100000 queries;

Microsoft.Data.Sqlite.SqliteConnection:
    30.285us to open and run 1 query;
    4.720us/query marginal query cost over the next 100000 queries;

Sql Server LocalDb:
    53.278us to open and run 1 query;
    36.943us/query marginal query cost over the next 100000 queries;

Sql Server LocalDb(Dapper):
    56.597us to open and run 1 query;
    42.007us/query marginal query cost over the next 100000 queries;
```


Results from January 2024 on a Ryzen 7950x using .net 8.0, Dapper 2.0.123, Microsoft.Data.Sqlite.Core 7.0.0, Microsoft.Data.SqlClient 5.0.1, SQLitePCLRaw.bundle_e_sqlite3 2.1.2, System.Data.SQLite.Core 1.0.116


```
SQLitePCL.raw:
    9.535us to open and run 1 query;
    0.343us/query marginal query cost over the next 100000 queries;

SQLitePCL.raw (no statement caching):
    9.498us to open and run 1 query;
    2.147us/query marginal query cost over the next 100000 queries;

SQLitePCL.raw (no connection caching):
    8.956us to open and run 1 query;
    8.927us/query marginal query cost over the next 100000 queries;

System.Data.SQLite.SqliteConnection:
    112.339us to open and run 1 query;
    0.715us/query marginal query cost over the next 100000 queries;

System.Data.SQLite.SqliteConnection(Dapper):
    118.749us to open and run 1 query;
    3.344us/query marginal query cost over the next 100000 queries;

Microsoft.Data.Sqlite.SqliteConnection:
    11.403us to open and run 1 query;
    0.601us/query marginal query cost over the next 100000 queries;

Microsoft.Data.Sqlite.SqliteConnection(Dapper):
    11.457us to open and run 1 query;
    3.449us/query marginal query cost over the next 100000 queries;

Sql Server LocalDb:
    42.139us to open and run 1 query;
    23.076us/query marginal query cost over the next 100000 queries;

Sql Server LocalDb(Dapper):
    44.974us to open and run 1 query;
    24.127us/query marginal query cost over the next 100000 queries;
```