using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using CommentApi.Models;
using CommentApi.NaiveImplementation;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting.Internal;
using Xunit;
using Xunit.Abstractions;

namespace CommentApi.Tests
{
    public class IntegrationTests
    {

        public ITestOutputHelper testOutputHelper;

        public IntegrationTests(ITestOutputHelper testOutputHelper)
        {
            this.testOutputHelper = testOutputHelper;
        }
        
        public const string CommentText =
            @"Lorem ipsum dolor sit amet, consectetur adipiscing elit. Quisque iaculis dui et mi scelerisque, eget blandit diam condimentum. Vestibulum arcu turpis, aliquam molestie massa sit amet, commodo pretium dui. Curabitur id aliquet justo. Mauris tincidunt nibh in dui viverra, vel ultrices ex malesuada. Proin pellentesque arcu non nibh consequat, eget pulvinar nisl sodales. Duis imperdiet cursus sem, sit amet venenatis diam iaculis id. Suspendisse lacinia lectus ut sapien pellentesque, at ultrices risus cursus. Praesent tincidunt, nulla in hendrerit gravida, magna diam hendrerit leo, eu scelerisque justo lacus vel neque. Curabitur fermentum ex cursus erat feugiat scelerisque. Cras pellentesque ante non dui ultricies, vel dignissim enim rhoncus.
Phasellus eleifend justo id urna rutrum laoreet. Donec aliquam scelerisque lacus. Vivamus id gravida nulla. Ut quis mauris nibh. Donec et turpis sit amet felis tincidunt tincidunt at a massa. Mauris porta, magna in consequat ornare, est libero malesuada massa, eget tempus tortor sapien et massa. Donec vel accumsan diam. Quisque lacinia sodales eros, in mollis orci pellentesque sed. Duis facilisis nec arcu vitae ultricies. Ut convallis venenatis pellentesque. Nullam pretium magna id nulla rhoncus elementum. Phasellus id erat nisi. Proin lorem ligula, aliquet placerat elit pellentesque, semper laoreet sem. Duis lobortis mauris id risus mollis ornare. Vestibulum maximus feugiat turpis in tempor.";


        /*
        [Fact]
        public async Task RunBenchmark()
        {
            //Act
            var serviceCollection = new ServiceCollection();
            var startup = new Startup(new HostingEnvironment());
            startup.ConfigureServices(serviceCollection);
            var serviceProvider = serviceCollection.BuildServiceProvider(true);
            var sut = serviceProvider.GetRequiredService<CommentService>();
            var locations = new[]
            {
                "A",
                "B",
                "C"
            };

            //Run
            try
            {
                var stopwatch = Stopwatch.StartNew();
                var commentsRange = Enumerable.Range(0, 100);
                foreach (var index in commentsRange)
                {
                    var location = locations[index % 3];
                    var comment = await sut.CreateNewCommentAsync(null, index.ToString(), CommentText, location);
                    await GenerateSubComments(1, 3,sut, comment, index, location);
                }

                var creationTIme = stopwatch.ElapsedMilliseconds;
                testOutputHelper.WriteLine($"CreationTime {creationTIme}");
                stopwatch.Restart();
                foreach (var location in locations)
                {
                    await sut.SearchAsync(location, 1000, null, 100, 20, 30);
                    await sut.SearchAsync(location, 1000, "1/1", 100, 20, 30);
                    await sut.SearchAsync(location, 10, "1/1", 100, 20, 30);
                    await sut.SearchAsync(location, 1000, "1/1/5", 0, 0, 0);
                }
                stopwatch.Stop();
                var searchTime = stopwatch.ElapsedMilliseconds;
                testOutputHelper.WriteLine($"SearchTime {searchTime}");
            }
            finally
            {
                await sut.DeleteAllAsync();
            }
        }
        */

        private static async Task GenerateSubComments(int level, int maxLevel, CommentService sut, CommentDto comment, int index,
            string location)
        {
            if(level == maxLevel) return;
            
            var subCommentsRange = Enumerable.Range(0, 10);
            foreach (var i in subCommentsRange)
            {
                var subComment = await sut.CreateNewCommentAsync(comment.Id, $"{index}_i", CommentText,
                    location);
                await GenerateSubComments(level + 1, maxLevel, sut, subComment, i, location);
            }
        }
    }
}
