using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using CommentApi.NaiveImplementation;
using Dapper;
using Npgsql;
using Prometheus;

namespace CommentApi.Repositories;

public partial class CommentRepository
{
    private readonly Func<NpgsqlConnection> _connectionFactory;

    public CommentRepository(Func<NpgsqlConnection> connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }
    public Task<NewComment> FindAsync(Guid id)
    {
        return RunAndMeasureTimeAndEnsureSuccess(connection =>
            connection.QueryFirstOrDefaultAsync<NewComment>(
                $"SELECT * FROM Comment WHERE Id = @Id",
                new
                {
                    Id = id
                }));
    }

    public Task<NewComment> FindAsync(Guid id, string locationHash)
    {
        return RunAndMeasureTimeAndEnsureSuccess((connection) => connection.QueryFirstOrDefaultAsync<NewComment>(
            "SELECT Id, OrderInDirectParent, Parent0, Parent1, Cursor, Level FROM Comment WHERE Id = @Id and LocationHash = @LocationHash",
            new {Id = id, LocationHash = locationHash}));
    }

    public Task<int?> FindLastInParentAsync(Guid parentId)
    {
        return RunAndMeasureTimeAndEnsureSuccess((connection) => connection.QueryFirstOrDefaultAsync<int?>(
            "SELECT OrderInDirectParent FROM Comment WHERE ParentId = @ParentId ORDER BY OrderInDirectParent DESC",
            new {ParentId = parentId}));
    }

    public Task<IEnumerable<CommentListDto>> Get(string locationHash, string cursorPrefix, int minLevel, int maxLevel)
    {
        return RunAndMeasureTimeAndEnsureSuccess((connection) => connection.QueryAsync<CommentListDto>(
            "SELECT Id, Author, Created, Text, ParentId, Cursor, Level, OrderInDirectParent, IsLastInDirectParent FROM Comment WHERE LocationHash = @LocationHash AND Cursor LIKE @cursorPrefix AND Level BETWEEN @MinLevel AND @MaxLevel",
            new
            {
                LocationHash = locationHash, CursorPrefix = $"{cursorPrefix}%", MinLevel = minLevel, MaxLevel = maxLevel
            }));
    }

    public Task<int?> FindLastInDirectParentAsync(string locationHash)
    {
        return RunAndMeasureTimeAndEnsureSuccess((connection) => connection.QueryFirstOrDefaultAsync<int?>(
            "SELECT OrderInDirectParent FROM Comment WHERE Level = @Level and LocationHash = @LocationHash ORDER BY OrderInDirectParent DESC",
            new {LocationHash = locationHash, Level = 1}));
    }

    public Task Create(NewComment comment)
    {
        return RunAndMeasureTimeAndEnsureSuccess((connection) => connection.ExecuteAsync(
            @"
UPDATE Comment SET IsLastInDirectParent = false 
WHERE LocationHash = @LocationHash AND ((@ParentId IS NULL AND ParentId IS NULL) OR ParentId = @ParentId) AND IsLastInDirectParent = true;
INSERT INTO 
            Comment(Id, Author, Text, Created, ParentId, Cursor, LocationHash, Level,Parent0, Parent1, Parent2, OrderInDirectParent, IsLastInDirectParent) 
            VALUES(@Id, @Author, @Text, @Created, @ParentId, @Cursor, @LocationHash, @Level,@Parent0, @Parent1, @Parent2, @OrderInDirectParent, @IsLastInDirectParent)",
            comment));
    }

    public Task DeleteAsync()
    {
        return RunAndMeasureTimeAndEnsureSuccess(connection => connection.ExecuteAsync("TRUNCATE TABLE Comment"));
    }


    private async Task<TResponse> RunAndMeasureTimeAndEnsureSuccess<TResponse>(
        Func<NpgsqlConnection, Task<TResponse>> call, [CallerMemberName] string callName = null)
    {
        var stopwatch = new Stopwatch();
        stopwatch.Start();
        await using var connection = _connectionFactory();
        await connection.OpenAsync();
        var response = await call(connection);
        stopwatch.Stop();
        MetricsSQL.MeasureSentRequestToDatabreakers(callName, stopwatch.Elapsed.TotalSeconds);
        return response;
    }
}