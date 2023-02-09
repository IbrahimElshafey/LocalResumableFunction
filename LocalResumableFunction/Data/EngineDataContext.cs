using LocalResumableFunction.Helpers;
using LocalResumableFunction.InOuts;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace LocalResumableFunction.Data
{
    internal class EngineDataContext : DbContext
    {
        private readonly string _dbConnection = "Data Source=DataTemplate.db";

        public EngineDataContext()
        {
            
        }
        public EngineDataContext(string assemblyName)
        {
            _dbConnection = $"{assemblyName}.db";
        }

        public DbSet<FunctionRuntimeInfo> FunctionRuntimeInfos { get; set; }
        public DbSet<Wait> Waits { get; set; }
        public DbSet<MethodWait> EventWaits { get; set; }
        public DbSet<ManyMethodsWait> ManyEventsWaits { get; set; }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder.UseSqlite(_dbConnection);
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {

            modelBuilder.Entity<FunctionRuntimeInfo>()
              .Property(x => x.FunctionState)
              .HasConversion<ObjectToJsonConverter>();

            base.OnModelCreating(modelBuilder);
        }

        protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
        {
            configurationBuilder
                .Properties<Expression>()
                .HaveConversion<ExpressionToJsonConverter>();
            configurationBuilder
               .Properties<LambdaExpression>()
               .HaveConversion<LambdaExpressionToJsonConverter>();
            configurationBuilder
                .Properties<Type>()
                .HaveConversion<TypeToStringConverter>();
        }

    }
}
