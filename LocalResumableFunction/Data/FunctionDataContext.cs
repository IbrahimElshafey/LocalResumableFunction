using System.Diagnostics;
using System.Linq.Expressions;
using System.Reflection.Metadata;
using LocalResumableFunction.Helpers;
using LocalResumableFunction.InOuts;
using Microsoft.EntityFrameworkCore;

namespace LocalResumableFunction.Data;

internal class FunctionDataContext : DbContext
{

    public FunctionDataContext()
    {
        Database.EnsureCreated();
    }

    public DbSet<ResumableFunctionState> FunctionStates { get; set; }
    public DbSet<MethodIdentifier> MethodIdentifiers { get; set; }
    public DbSet<Wait> Waits { get; set; }
    public DbSet<MethodWait> MethodWaits { get; set; }
    public DbSet<FunctionWait> FunctionWaits { get; set; }
    //public DbSet<TimeWait> TimeWaits { get; set; }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        //todo: resolve at runtime to enable multiple providers
        optionsBuilder.LogTo(s => Debug.WriteLine(s));
        optionsBuilder.UseSqlServer(
            $"Server=(localdb)\\MSSQLLocalDB;Database=ResumableFunctionsData;");

        //var _dbConnection = $"Data Source={AppContext.BaseDirectory}LocalResumableFunctionsData.db";
        //optionsBuilder.UseSqlite(_dbConnection);
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
            .OnDelete(DeleteBehavior.Restrict)
            .HasForeignKey(x => x.RequestedByFunctionId)
            .HasConstraintName("FK_Waits_In_ResumableFunction");

        modelBuilder.Entity<MethodIdentifier>()
            .HasMany(x => x.WaitsRequestsForMethod)
            .WithOne(wait => wait.WaitMethodIdentifier)
            .OnDelete(DeleteBehavior.Restrict)
            .HasForeignKey(x => x.WaitMethodIdentifierId)
            .HasConstraintName("FK_Waits_RequestedForMethod");

        modelBuilder.Entity<Wait>()
            .HasMany(x => x.ChildWaits)
            .WithOne(wait => wait.ParentWait)
            .HasForeignKey(x => x.ParentWaitId)
            .HasConstraintName("FK_ChildWaits_For_Wait");

        modelBuilder.Entity<ResumableFunctionState>()
            .Property<DateTime>(ConstantValue.LastUpdatedProp);
        modelBuilder.Entity<ResumableFunctionState>()
           .Property<DateTime>(ConstantValue.CreatedProp);
        modelBuilder.Entity<MethodIdentifier>()
           .Property<DateTime>(ConstantValue.CreatedProp);
        modelBuilder.Entity<Wait>()
           .Property<DateTime>(ConstantValue.CreatedProp);

        modelBuilder.Entity<ResumableFunctionState>()
            .Property(x => x.StateObject)
            .HasConversion<ObjectToJsonConverter>();

        modelBuilder.Entity<Wait>()
            .Property(x => x.ExtraData)
            .HasConversion<ObjectToJsonConverter>();

        modelBuilder.Entity<MethodWait>()
            .Property(mw => mw.MatchIfExpressionValue)
            .HasColumnName(nameof(MethodWait.MatchIfExpressionValue));

        modelBuilder.Entity<WaitsGroup>()
            .Property(mw => mw.CountExpressionValue)
            .HasColumnName(nameof(WaitsGroup.CountExpressionValue));

        modelBuilder.Entity<MethodWait>()
            .Property(mw => mw.SetDataExpressionValue)
            .HasColumnName(nameof(MethodWait.SetDataExpressionValue));

        modelBuilder.Ignore<ReplayRequest>();
        modelBuilder.Ignore<TimeWait>();
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

    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = new CancellationToken())
    {
        SetDates();
        ExcludeFalseAddEntries();

        return base.SaveChangesAsync(cancellationToken);
    }

    private void SetDates()
    {
        foreach (var entityEntry in ChangeTracker.Entries())
        {
            switch (entityEntry.State)
            {
                case EntityState.Modified:
                    bool lastUpdatePropExist = entityEntry.Metadata.FindProperty(ConstantValue.LastUpdatedProp) != null;
                    if (lastUpdatePropExist)
                        entityEntry.Property(ConstantValue.LastUpdatedProp).CurrentValue = DateTime.Now;
                    break;
                case EntityState.Added:
                    bool createdPropExist = entityEntry.Metadata.FindProperty(ConstantValue.CreatedProp) != null;
                    if (createdPropExist)
                        entityEntry.Property(ConstantValue.CreatedProp).CurrentValue = DateTime.Now;
                    break;
            }
        }
    }

    private void ExcludeFalseAddEntries()
    {
        var falseAddEntries =
                    ChangeTracker
                    .Entries()
                    .Where(x => x.State == EntityState.Added && x.IsKeySet)
                    .ToList();

        falseAddEntries
            .ForEach(x => x.State = EntityState.Unchanged);
    }
}