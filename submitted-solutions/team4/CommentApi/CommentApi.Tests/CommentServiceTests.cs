using System;
using System.Threading.Tasks;
using FluentAssertions;
using CommentApi.NaiveImplementation;
using Xunit;
using Npgsql;
using CommentApi.Repositories;
using CommentApi.Sql;

namespace CommentApi.Tests
{
    public class CommentServiceTests :IAsyncLifetime
    {
        private const string TestLocation = "www.notino.com";
        private NpgsqlConnection _connection;

        [Fact]
        public async Task GetCommentByIdAsyncWithExistingCommentReturnsIt()
        {
            //Arrange
            var commentService = await CreateCommentService();
            var createdComment = await commentService.CreateNewCommentAsync(null, "Autor", "Text", "location");

            //Act
            var comment = await commentService.GetByIdAsync(createdComment.Id);

            //Assert
            comment!.Author.Should().Be(createdComment.Author);
            comment!.Text.Should().Be(createdComment.Text);
            comment!.Parent.Should().Be(createdComment.Parent);
        }

        [Fact]
        public async Task SearchCommentsCanFindSingleTopLevelComment()
        {
            //Arrange
            var commentService = await CreateCommentService();
            await commentService.CreateNewCommentAsync(null, "A", "Adam", TestLocation);

            //Act
            var result = await commentService.SearchAsync(TestLocation, 10, null, 0, 0, 0);

            //Assert
            result.Edges
                .Should()
                .SatisfyRespectively(x => x.Node.Text.Should().Be("Adam"));

            result.PageInfo
                .HasNextPage
                .Should()
                .BeFalse();

            result.PageInfo
                .EndCursor
                .Should()
                .Be("1");
        }

        [Fact]
        public async Task SearchCommentsCanFindMultipleTopLevelCommentsWithinLimit()
        {
            //Arrange
            var commentService = await CreateCommentService();
            await commentService.CreateNewCommentAsync(null, "A", "Adam", TestLocation);
            await commentService.CreateNewCommentAsync(null, "A", "Tomas", TestLocation);
            await commentService.CreateNewCommentAsync(null, "A", "Michal", TestLocation);
            

            //Act
            var result = await commentService.SearchAsync(
                TestLocation,
                2,
                null,
                0,
                0,
                0);

            //Assert
            result.Edges
                .Should()
                .SatisfyRespectively(
                    c => c.Node.Text.Should().Be("Adam"),
                    c => c.Node.Text.Should().Be("Tomas"));

            result.PageInfo.EndCursor
                .Should()
                .Be("2");

            result.PageInfo.HasNextPage
                .Should()
                .BeTrue();
        }

        [Fact]
        public async Task SearchCommentsCanFindMultipleTopLevelCommentsWithinLimitWithAfter()
        {
            //Arrange
            //Arrange
            var commentService = await CreateCommentService();
            await commentService.CreateNewCommentAsync(null, "A", "Adam", TestLocation);
            await commentService.CreateNewCommentAsync(null, "A", "Tomas", TestLocation);
            await commentService.CreateNewCommentAsync(null, "A", "Michal", TestLocation);

            //Act
            var result = await commentService.SearchAsync(
                TestLocation,
                2,
                "1",
                0,
                0,
                0);

            //Assert
            result.Edges
                .Should()
                .SatisfyRespectively(
                    c => c.Node.Text.Should().Be("Tomas"),
                    c => c.Node.Text.Should().Be("Michal"));
            
            result.PageInfo.EndCursor
                .Should()
                .Be("3");

            result.PageInfo.HasNextPage
                .Should()
                .BeFalse();
        }

        [Fact]
        public async Task SearchCommentsCanFindMultipleTopLevelAndOneFirstLevelCommentsWithinLimitWithAfter()
        {
            //Arrange
            var commentService = await CreateCommentService();
            var firstComment = await commentService.CreateNewCommentAsync(null, "A", "Adam", TestLocation);
            await commentService.CreateNewCommentAsync(firstComment.Id, "A", "Tomas", TestLocation);
            await commentService.CreateNewCommentAsync(null, "A", "Michal", TestLocation);
            
            //Act
            var comments = await commentService.SearchAsync(
                TestLocation,
                2,
                null,
                1,
                0,
                0);

            //Assert
            comments.Edges
                .Should()
                .SatisfyRespectively(
                    c =>
                    {
                        c.Node.Text.Should().Be("Adam");
                        c.Node.Replies
                            .Edges
                            .Should()
                            .SatisfyRespectively(x => x.Node.Text.Should().Be("Tomas"));
                    },
                    c => c.Node.Text.Should().Be("Michal"));
        }

        [Fact]
        public async Task SearchCommentsCanFindMultipleTopLevelAndMultipleFirstLevelCommentsWithinLimit()
        {
            //Arrange
            var commentService = await CreateCommentService();
            var firstComment = await commentService.CreateNewCommentAsync(null, "A", "Adam", TestLocation);
            await commentService.CreateNewCommentAsync(firstComment.Id, "A", "Tomas", TestLocation);
            await commentService.CreateNewCommentAsync(firstComment.Id, "A", "Michal Motycka", TestLocation);
            await commentService.CreateNewCommentAsync(null, "A", "Michal", TestLocation);
            
            //Act
            var comments = await commentService.SearchAsync(
                TestLocation,
                2,
                null,
                1,
                0,
                0);

            //Assert
            comments.Edges
                .Should()
                .SatisfyRespectively(
                    c =>
                    {
                        c.Node.Text.Should().Be("Adam");
                        c.Node.Replies
                            .Edges
                            .Should()
                            .SatisfyRespectively(x => x.Node.Text.Should().Be("Tomas"));
                    },
                    c => c.Node.Text.Should().Be("Michal"));
        }
        
        [Fact]
        public async Task SearchCommentsCanFindMultipleTopLevelAndMultipleFirstLevelCommentsWithinLimitWithAfter()
        {
            //Arrange
            var commentService = await CreateCommentService();
            var firstComment = await commentService.CreateNewCommentAsync(null, "A", "Adam", TestLocation);
            await commentService.CreateNewCommentAsync(firstComment.Id, "A", "Tomas", TestLocation);
            await commentService.CreateNewCommentAsync(firstComment.Id, "A", "Michal Motycka", TestLocation);
            var secondComment = await commentService.CreateNewCommentAsync(null, "A", "Michal", TestLocation);
            await commentService.CreateNewCommentAsync(secondComment.Id, "A", "Michal Moticka", TestLocation);
            await commentService.CreateNewCommentAsync(secondComment.Id, "A", "Prdik", TestLocation);
            
            //Act
            var comments = await commentService.SearchAsync(
                TestLocation,
                2,
                "1/1",
                1,
                0,
                0);

            //Assert
            comments.Edges
                .Should()
                .SatisfyRespectively(
                    c => c.Node.Text.Should().Be("Michal Motycka"));
        }
        
        [Fact]
        public async Task SearchCommentsCanFindMultipleTopLevelAndMultipleFirstLevelCommentsAndSecondLevelCommentsWithinLimit()
        {
            //Arrange
            var commentService = await CreateCommentService();
            var firstComment = await commentService.CreateNewCommentAsync(null, "A", "Adam", TestLocation);
            var firstReplyToFirstComment = await commentService.CreateNewCommentAsync(firstComment.Id, "A", "Tomas", TestLocation);
            var secondReplyToFirstComment = await commentService.CreateNewCommentAsync(firstComment.Id, "A", "Tomas", TestLocation);
            await commentService.CreateNewCommentAsync(secondReplyToFirstComment.Id, "A", "Michal Motycka", TestLocation);
            
            //Act
            var comments = await commentService.SearchAsync(
                TestLocation,
                1,
                null,
                1,
                2,
                0);

            //Assert
            comments.Edges
                .Should()
                .SatisfyRespectively(
                    c =>
                    {
                        c.Node.Text.Should().Be("Adam");
                        c.Node.Replies.Edges.Should()
                            .SatisfyRespectively(x =>
                            {
                                x.Node.Text.Should().Be("Tomas");
                            });
                    });
        }
        

        private async Task<CommentService> CreateCommentService()
        {
            return new CommentService(new CommentRepository(_connection));
        }

        public async Task InitializeAsync()
        {
            _connection = new NpgsqlConnection("User ID=postgres;Password=myPassword;Host=localhost;Port=5432;Database=comments;Pooling=false");

            var service = new MigrateSqlService(_connection);
            await service.StartAsync(CancellationToken.None);
        }

        public async Task DisposeAsync()
        {
            await _connection.CloseAsync();
            await _connection.DisposeAsync();
        }
    }
}