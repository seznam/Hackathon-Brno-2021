using System;
using System.Threading.Tasks;
using System;
using CommentApi;
using CommentApi.NaiveImplementation;
using CommentApi.Sql;
using CommentApi.Repositories;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Mvc.Formatters;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Npgsql;
using Prometheus;
using CommentApi.Models;
using Microsoft.AspNetCore.Mvc;
using CommentApi.Extensions;
using Microsoft.AspNetCore.Http;
using Utf8Json;
using Utf8Json.Resolvers;

public class Startup
{
    private readonly IHostEnvironment _hostEnvironment;
    private readonly IConfiguration _configuration;

    public Startup(IHostEnvironment hostEnvironment, IConfiguration configuration)
    {
        _hostEnvironment = hostEnvironment;
        _configuration = configuration;
    }

    public void ConfigureServices(
        IServiceCollection services)
    {
        services.AddResponseCompression();
        services.AddHostedService<MigrateSqlService>();
        services.AddTransient<Func<NpgsqlConnection>>((_) => () => new NpgsqlConnection(_configuration.GetConnectionString("Postgres")));

        services.AddTransient<CommentService>();
        services.AddTransient<Func<CommentService>>(sp => () => sp.GetRequiredService<CommentService>());
        services.AddTransient<CommentRepository>();
        ; services.AddLogging();
        services.AddControllers()
            .AddControllersAsServices();
        services.AddSwaggerGen(c =>
        {
            c.SwaggerDoc("v1", new() { Title = "CommentApi", Version = "v1" });
        });
    }


    public void Configure(IApplicationBuilder app)
    {
        if (_hostEnvironment.IsDevelopment())
        {
            app.UseDeveloperExceptionPage();
            app.UseSwagger();
            app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "CommentApi v1"));

            //app.UseMiddleware<LogRequestMiddleware>();
            //app.UseMiddleware<LogResponseMiddleware>();
        }

        app.UseMiddleware<GzipRequestMiddleware>();
        app.UseResponseCompression();
        app.UseRouting();

        app.UseMetricServer();
        app.UseHttpMetrics();

        app.Use(next => context =>
        {
            if (context.Request.Path.HasValue && 
                context.Request.Path.Value.Contains("//"))
            {
                context.Response.StatusCode = 400;
                return Task.CompletedTask;
            }

            return next(context);

        });

        app.UseEndpoints(endpoints =>
        {
            endpoints.MapGet("/webPage/{location}/comment", async (HttpContext http,
                [FromServices] CommentService commentService,
                string location,
                [FromQuery] int limit,
                [FromQuery] string? after,
                [FromQuery] int replies1stLevelLimit,
                [FromQuery] int replies2ndLevelLimit,
                [FromQuery] int replies3rdLevelLimit) =>
            {
                if (limit <= 0 ||
                    replies1stLevelLimit < 0 ||
                    replies2ndLevelLimit < 0 ||
                    replies3rdLevelLimit < 0)
                {
                    http.Response.StatusCode = 400;
                }

                var comments = await commentService.SearchAsync(
                    location,
                    limit,
                    after,
                    replies1stLevelLimit,
                    replies2ndLevelLimit,
                    replies3rdLevelLimit);

                await http.Response.WriteAsJsonAsync(comments);
            });

            endpoints.MapPost("/batch", async (HttpContext http, 
                [FromServices] Func<CommentService> commentServiceFactory,
                [FromBody] IEnumerable<BatchInputItem> items) =>
            {
                if (items.Any(i => !i.Validate()))
                {
                    http.Response.StatusCode = 400;
                    return;
                }

                var comment = await BatchOperationImpl(items, commentServiceFactory);

                await http.Response.WriteAsJsonAsync(comment);
            });


            endpoints.MapDelete("/comment", async (HttpContext http, [FromServices] CommentService commentService) =>
            {
                await commentService.DeleteAllAsync();

                http.Response.StatusCode = 204;
            });

            endpoints.MapGet("/comment/{id}", async (HttpContext http, [FromServices] CommentService commentService, Guid id) =>
            {
                var comment = await commentService.GetByIdAsync(id);
                if (comment is not null)
                {
                    await http.Response.WriteAsJsonAsync(comment);
                    return;
                }
                else
                {
                    http.Response.StatusCode = 404;
                }
            });

            endpoints.MapControllers();
        });
    }

    private async Task<IEnumerable<object?>> BatchOperationImpl(IEnumerable<BatchInputItem> batchInput, Func<CommentService> commentServiceFactory)
    {

        var tasks = batchInput.Select(item =>
        {
            var commentsService = commentServiceFactory();
            if (item.Id.HasValue)
            {
                return GetBatchItem(item.Id.Value, commentsService);
            }
            else
            {
                return GetBatchItem(item, commentsService);
            }
        });

        return await Task.WhenAll(tasks);
    }

    private async Task<object?> GetBatchItem(Guid id, CommentService commentService)
    {
        return await commentService.GetByIdAsync(id);
    }

    private async Task<object?> GetBatchItem(BatchInputItem item, CommentService commentService)
    {
        return await commentService.SearchAsync(
        item.Location!,
        item.Limit!.Value,
        item.After,
        item.Replies1stLevelLimit!.Value,
        item.Replies2ndLevelLimit!.Value,
        item.Replies3rdLevelLimit!.Value);
    }
}
