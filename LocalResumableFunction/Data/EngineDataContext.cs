using LocalResumableFunction.Helpers;
using LocalResumableFunction.InOuts;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Diagnostics;
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
        private readonly string _dbConnection;
        public EngineDataContext()
        {
            Debugger.Break();
            string codeBase = Assembly.GetExecutingAssembly().CodeBase;
            UriBuilder uri = new UriBuilder(codeBase);
            string path = Uri.UnescapeDataString(uri.Path);
            _dbConnection = $"Data Source={Path.GetDirectoryName(path)}.sqlite";
        }

        public DbSet<ResumableFunctionState> FunctionStates { get; set; }
        public DbSet<MethodIdentifier> MethodIdentifiers { get; set; }
        public DbSet<Wait> Waits { get; set; }
        public DbSet<MethodWait> MethodWaits { get; set; }
        public DbSet<ManyMethodsWait> ManyMethodsWaits { get; set; }
        public DbSet<FunctionWait> FunctionWaits { get; set; }
        public DbSet<ManyFunctionsWait> ManyFunctionsWaits { get; set; }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder.UseSqlite(_dbConnection);
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<ResumableFunctionState>()
            .HasMany(x => x.Waits)
            .WithOne(wait => wait.FunctionState)
            .HasForeignKey(x => x.FunctionStateId)
            .HasConstraintName("FK_Waits_For_FunctionState");

            modelBuilder.Entity<MethodIdentifier>()
            .HasMany(x => x.ActiveFunctionsStates)
            .WithOne(wait => wait.ResumableFunctionIdentifier)
            .HasForeignKey(x => x.ResumableFunctionIdentifierId)
            .HasConstraintName("FK_FunctionsStates_For_ResumableFunction");

            modelBuilder.Entity<MethodIdentifier>()
            .HasMany(x => x.WaitsCreatedByFunction)
            .WithOne(wait => wait.RequestedByFunction)
            .HasForeignKey(x => x.RequestedByFunctionId)
            .HasConstraintName("FK_Waits_In_ResumableFunction");

            modelBuilder.Entity<MethodIdentifier>()
                .HasMany(x => x.WaitsRequestsForMethod)
                .WithOne(wait => wait.WaitMethodIdentifier)
                .HasForeignKey(x => x.WaitMethodIdentifierId)
                .HasConstraintName("FK_Waits_RequestedForMethod");

            modelBuilder.Entity<Wait>()
            .HasMany(x => x.ChildWaits)
            .WithOne(wait => wait.ParentWait)
            .HasForeignKey(x => x.ParentWaitId)
            .HasConstraintName("FK_ChildWaits_For_Wait");

            modelBuilder.Entity<ManyMethodsWait>()
            .HasMany(x => x.WaitingMethods)
            .WithOne(wait => wait.ParentWaitsGroup)
            .HasForeignKey(x => x.ParentWaitsGroupId)
            .HasConstraintName("FK_MethodsWaits_For_WaitsGroup");

            modelBuilder.Entity<ManyFunctionsWait>()
                .HasMany(x => x.WaitingFunctions)
                .WithOne(wait => wait.ParentFunctionGroup)
                .HasForeignKey(x => x.ParentFunctionGroupId)
                .HasConstraintName("FK_FunctionsWaits_For_FunctionGroup");

            modelBuilder.Entity<ResumableFunctionState>()
              .Property(x => x.StateObject)
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
