using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using LinqToDB;
using LinqToDB.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Xunit;
using Xunit.Abstractions;

namespace TestProject1
{
    public class UnitTest1
    {
        private readonly ITestOutputHelper _output;

        public UnitTest1(ITestOutputHelper output)
        {
            _output = output;
            var optionsBuilder = new DbContextOptionsBuilder<MyContext>();
            optionsBuilder.UseSqlServer(@"Server=(localdb)\MSSQLLocalDB;Integrated Security=True;");
            //optionsBuilder.UseSqlite("DataSource=../../../testing1.db");
            // File.Delete("../../../testing1.db");
            Context = new MyContext(optionsBuilder.Options);
            Context.EnsureCreatedAndConnectedWithLinq2Db(_output);
        }
        
        private MyContext Context { get;  }
        
        [Fact]
        public async Task Test1()
        {
            var testFolderSet = Context.Set<TestFolder>();
            var testFolderRoot = new TestFolder
            {
                Id = Guid.Parse("10000000-0000-0000-0000-000000000000"), Label = "root", ParentId = null
            };
            var testFolderA = new TestFolder
            {
                Id = Guid.Parse("10000000-0000-0000-0000-000000000001"), Label = "A", ParentId = testFolderRoot.Id
            };
            var testFolderB = new TestFolder
            {
                Id = Guid.Parse("10000000-0000-0000-0000-000000000002"), Label = "B", ParentId = testFolderRoot.Id
            };
            if (!testFolderSet.Any(f => f.Id == testFolderRoot.Id))
                testFolderSet.Add(testFolderRoot);
            if (!testFolderSet.Any(f => f.Id == testFolderA.Id))
                testFolderSet.Add(testFolderA);
            if (!testFolderSet.Any(f => f.Id == testFolderB.Id))
                testFolderSet.Add(testFolderB);

            await Context.SaveChangesAsync();
            
            await using var db = Context.CreateLinq2DbConnectionDetached();
            var q = db.GetCte<CteEntity<TestFolder>>("CTE", cte =>
            {
                return (Context.Set<TestFolder>()
                        .ToLinqToDBTable()
                        .Where(c => c.ParentId == null)
                        .Select(c =>
                            new CteEntity<TestFolder>() {Level = 0, Id = c.Id, ParentId = c.ParentId, Label = (string)c.Label}))
                    .Concat(
                        Context.Set<TestFolder>()
                            .ToLinqToDBTable()
                            .SelectMany(c => cte.InnerJoin(r => c.ParentId == r.Id),
                                (c, r) => new CteEntity<TestFolder>
                                {
                                    Level = r.Level + 1,
                                    Id = c.Id,
                                    ParentId = c.ParentId,
                                    Label = r.Label + '/' + c.Label,
                                })
                    );
            });
            
            var query = q.SelectMany(c => Context.Set<TestFolder>().InnerJoin(p => p.Id == c.Id),
                (c, e) => new CteEntity<TestFolder>
                {
                    Id = c.Id,
                    Level = c.Level,
                    ParentId = c.ParentId,
                    Label = c.Label,
                    Entity = e
                });
            
            
            Assert.Equal(3, query.Count());
            db.Connection.Close();
        }
    }
    
    public class CteEntity<TEntity> where TEntity : class
    {
        public TEntity Entity { get; set; } = null!;
        public Guid Id { get; set; }
        public Guid? ParentId { get; set; }
        public int Level { get; set; }
        public string Label { get; set; } = null!;
    }
}