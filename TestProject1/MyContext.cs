using System;
using System.ComponentModel.DataAnnotations;
using LinqToDB.Data;
using LinqToDB.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Xunit.Abstractions;

namespace TestProject1
{
    public class MyContext : DbContext
    {
        public MyContext(DbContextOptions<MyContext> options)
            : base(options)
        {
        }
        
        public DbSet<TestFolder> TestFolders { get; set; }
        
        public void EnsureCreatedAndConnectedWithLinq2Db(ITestOutputHelper output)
        {
            Database.OpenConnection();
            Database.EnsureCreated();

            // Linq2Db
            LinqToDBForEFTools.Initialize();
        }
    }
    
    public class TestFolder
    {
        public Guid Id { get; set; }
        [MaxLength(50)]
        public string? Label { get; set; }
        public TestFolder? Parent { get; set; }
        public Guid? ParentId { get; set; }
    }
}