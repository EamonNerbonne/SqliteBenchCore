A simple test to evaluate the overhead of various ways of accessing sqlite.

The test opens connections to an in-memory sqlite database and uses no tables to ensure the bottleneck is not the disk.
The query `select 1 as A, 2 as B, 'test' as C union all select 2,3,'test2' union all select 3,4,'test71'` is run in batches of 100000, and that's repeated 10 times.  The mean duration of the fastest 5 batches is reported.

As a baseline, the performance of Sql Server LocalDb in the same test is also included (which is not an in-proc db, and is thus expected to incur more overhead).

Results on an i7-4770k using .net core 2.0 (net471 appears slightly slower overall):

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
