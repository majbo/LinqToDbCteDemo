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
            File.Delete("../../../testing1.db");
            
            var optionsBuilder = new DbContextOptionsBuilder<MyContext>();
            optionsBuilder.UseSqlite("DataSource=../../../testing1.db");
            Context = new MyContext(optionsBuilder.Options);
            Context.EnsureCreatedAndConnectedWithLinq2Db(_output);
        }
        
        private MyContext Context { get;  }
        
        [Fact]
        public async Task Test1()
        {
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
            Context.Set<TestFolder>().AddRange(testFolderRoot, testFolderA, testFolderB);
            await Context.SaveChangesAsync();
            
            await using var db = Context.CreateLinq2DbConnectionDetached();
            var q = db.GetCte<CteEntity<TestFolder>>("CTE", cte =>
            {
                return (Context.Set<TestFolder>()
                        .ToLinqToDBTable()
                        .Where(c => c.ParentId == null)
                        .Select(c =>
                            new CteEntity<TestFolder>() {Level = 0, Id = c.Id, ParentId = c.ParentId, Label = c.Label, Entity = c}))
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
                                    Entity = c
                                })
                    );
            });
            
            Assert.Equal(3, q.Count());
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