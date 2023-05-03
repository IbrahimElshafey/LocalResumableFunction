using System.Diagnostics;
using System.Linq.Expressions;
using System.Reflection.Metadata;
using ResumableFunctions.Handler.Helpers;
using ResumableFunctions.Handler.InOuts;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System.Reflection.Emit;
using System.Text.Json;
using Newtonsoft.Json;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection;

namespace ResumableFunctions.Handler.Data;

public class FunctionDataContext : DbContext
{
    internal readonly MethodIdentifierRepository methodIdentifierRepo;
    internal readonly WaitsRepository waitsRepository;

    public FunctionDataContext(
        IServiceProvider serviceProvider, IResumableFunctionsSettings settings) : base(settings.WaitsDbConfig.Options)
    {
        try
        {
            Database.EnsureCreated();
            methodIdentifierRepo = ActivatorUtilities.CreateInstance<MethodIdentifierRepository>(serviceProvider);
            waitsRepository = ActivatorUtilities.CreateInstance<WaitsRepository>(serviceProvider);
            methodIdentifierRepo._context = this;
            waitsRepository._context = this;
        }
        catch (Exception)
        {
            //Task.Delay(TimeSpan.FromMinutes(3)).Wait();
            Database.EnsureCreated();
        }
    }

    public DbSet<ResumableFunctionState> FunctionStates { get; set; }
    public DbSet<FunctionStateLogRecord> FunctionStateLogs { get; set; }

    public DbSet<MethodIdentifier> MethodIdentifiers { get; set; }
    public DbSet<WaitMethodIdentifier> WaitMethodIdentifiers { get; set; }
    public DbSet<ResumableFunctionIdentifier> ResumableFunctionIdentifiers { get; set; }

    public DbSet<Wait> Waits { get; set; }
    public DbSet<MethodWait> MethodWaits { get; set; }
    public DbSet<MethodsGroup> MethodsGroups { get; set; }
    public DbSet<FunctionWait> FunctionWaits { get; set; }

    public DbSet<PushedCall> PushedCalls { get; set; }
    public DbSet<ServiceData> ServicesData { get; set; }



    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        ConfigureResumableFunctionState(modelBuilder.Entity<ResumableFunctionState>());
        ConfigureMethodIdentifier(modelBuilder);
        ConfigurePushedCalls(modelBuilder.Entity<PushedCall>());
        ConfigureServiceData(modelBuilder.Entity<ServiceData>());
        ConfigureWaits(modelBuilder);
        ConfigurConcurrencyToken(modelBuilder);
        ConfigurSoftDeleteFilter(modelBuilder);

        base.OnModelCreating(modelBuilder);
    }

    private void ConfigurSoftDeleteFilter(ModelBuilder modelBuilder)
    {
        //todo:https://haacked.com/archive/2019/07/29/query-filter-by-interface/
        modelBuilder.Entity<Wait>().HasQueryFilter(p => !p.IsDeleted);
        modelBuilder.Entity<ResumableFunctionState>().HasQueryFilter(p => !p.IsDeleted);
        modelBuilder.Entity<PushedCall>().HasQueryFilter(p => !p.IsDeleted);
    }

    private void ConfigurConcurrencyToken(ModelBuilder modelBuilder)
    {
        foreach (var entityType in modelBuilder.Model.GetEntityTypes())
        {
            if (typeof(IEntityWithUpdate).IsAssignableFrom(entityType.ClrType))
            {
                modelBuilder
                    .Entity(entityType.ClrType)
                    .Property<string>(nameof(IEntityWithUpdate.ConcurrencyToken))
                    .IsConcurrencyToken();
            }
        }
    }

    private void ConfigureServiceData(EntityTypeBuilder<ServiceData> entityTypeBuilder)
    {
        //entityTypeBuilder.Property(x => x.Modified).HasDefaultValue(DateTime.MinValue);
        entityTypeBuilder.HasIndex(x => x.AssemblyName);
    }

    private void ConfigurePushedCalls(EntityTypeBuilder<PushedCall> entityTypeBuilder)
    {
        entityTypeBuilder
          .Property(x => x.Input)
          .HasConversion<ObjectToJsonConverter>();

        entityTypeBuilder
            .Property(x => x.Output)
            .HasConversion<ObjectToJsonConverter>();

        entityTypeBuilder
           .Property(x => x.MethodData)
           .HasConversion(
            v => JsonConvert.SerializeObject(v),
            v => JsonConvert.DeserializeObject<MethodData>(v));
    }

    private void ConfigureWaits(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Wait>()
            .HasMany(x => x.ChildWaits)
            .WithOne(wait => wait.ParentWait)
            .HasForeignKey(x => x.ParentWaitId)
            .HasConstraintName("FK_ChildWaits_For_Wait");

        modelBuilder.Entity<Wait>()
            .Property(x => x.ExtraData)
            .HasConversion<ObjectToJsonConverter>();

        modelBuilder.Entity<MethodWait>()
          .Property(mw => mw.MatchIfExpressionValue)
          .HasColumnName(nameof(MethodWait.MatchIfExpressionValue));

        modelBuilder.Entity<MethodWait>()
            .Property(mw => mw.SetDataExpressionValue)
            .HasColumnName(nameof(MethodWait.SetDataExpressionValue));

        modelBuilder.Entity<WaitsGroup>()
           .Property(mw => mw.GroupMatchExpressionValue)
           .HasColumnName(nameof(WaitsGroup.GroupMatchExpressionValue));

        modelBuilder.Ignore<ReplayRequest>();
        modelBuilder.Ignore<TimeWait>();
    }

    private void ConfigureMethodIdentifier(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<ResumableFunctionIdentifier>()
           .HasMany(x => x.ActiveFunctionsStates)
           .WithOne(wait => wait.ResumableFunctionIdentifier)
           .HasForeignKey(x => x.ResumableFunctionIdentifierId)
           .HasConstraintName("FK_FunctionsStates_For_ResumableFunction");

        modelBuilder.Entity<ResumableFunctionIdentifier>()
            .HasMany(x => x.WaitsCreatedByFunction)
            .WithOne(wait => wait.RequestedByFunction)
            .OnDelete(DeleteBehavior.Restrict)
            .HasForeignKey(x => x.RequestedByFunctionId)
            .HasConstraintName("FK_Waits_In_ResumableFunction");

        modelBuilder.Entity<MethodsGroup>()
            .HasMany(x => x.WaitRequestsForGroup)
            .WithOne(mw => mw.MethodGroupToWait)
            .OnDelete(DeleteBehavior.Restrict)
            .HasForeignKey(x => x.MethodGroupToWaitId)
            .HasConstraintName("FK_WaitsRequestsForGroup");

        modelBuilder.Entity<MethodsGroup>()
          .HasMany(x => x.WaitMethodIdentifiers)
          .WithOne(waitMid => waitMid.ParentMethodGroup)
          .OnDelete(DeleteBehavior.Restrict)
          .HasForeignKey(x => x.ParentMethodGroupId)
          .HasConstraintName("FK_Group_WaitMethodIdentifiers");

        modelBuilder.Entity<WaitMethodIdentifier>()
        .HasMany(x => x.WaitsRequestsForMethod)
        .WithOne(mw => mw.MethodToWait)
        .OnDelete(DeleteBehavior.Restrict)
        .HasForeignKey(x => x.MethodToWaitId)
        .HasConstraintName("FK_WaitsRequestsForMethod");

        modelBuilder.Entity<MethodsGroup>()
           .HasIndex(x => x.MethodGroupUrn)
            .HasDatabaseName("Index_MethodGroupUniqueUrn")
            .IsUnique(true);
    }

    private void ConfigureResumableFunctionState(EntityTypeBuilder<ResumableFunctionState> entityTypeBuilder)
    {
        entityTypeBuilder
            .HasMany(x => x.Waits)
            .WithOne(wait => wait.FunctionState)
            .HasForeignKey(x => x.FunctionStateId)
            .HasConstraintName("FK_Waits_For_FunctionState");

        entityTypeBuilder
            .HasMany(x => x.LogRecords)
            .WithOne(wait => wait.FunctionState)
            .HasForeignKey(x => x.FunctionStateId)
            .HasConstraintName("FK_Logs_For_FunctionState");

        entityTypeBuilder
           .Property(x => x.StateObject)
           .HasConversion<ObjectToJsonConverter>();
    }

    protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
    {
        //configurationBuilder
        //    .Properties<Expression>()
        //    .HaveConversion<ExpressionToJsonConverter>();
        //configurationBuilder
        //    .Properties<LambdaExpression>()
        //    .HaveConversion<LambdaExpressionToJsonConverter>();
        configurationBuilder
            .Properties<Type>()
            .HaveConversion<TypeToStringConverter>();
    }

    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = new CancellationToken())
    {
        SetDates();
        //NeverUpdateFirstWait();
        HandleSoftDelete();
        ExcludeFalseAddEntries();
        return base.SaveChangesAsync(cancellationToken);
    }

    private void NeverUpdateFirstWait()
    {
        foreach (var entityEntry in ChangeTracker.Entries())
        {
            if (entityEntry.State == EntityState.Modified && entityEntry.Entity is Wait wait && wait.IsFirst)
            {
                entityEntry.State = EntityState.Unchanged;
                Entry(wait.FunctionState).State = EntityState.Unchanged;
            }
        }
    }

    private void HandleSoftDelete()
    {
        foreach (var entityEntry in ChangeTracker.Entries())
        {
            switch (entityEntry.State)
            {
                case EntityState.Deleted when entityEntry.Entity is IEntityWithDelete:
                    entityEntry.Property(nameof(IEntityWithDelete.IsDeleted)).CurrentValue = true;
                    entityEntry.State = EntityState.Modified;
                    break;
            }
        }
    }

    private void SetDates()
    {
        foreach (var entityEntry in ChangeTracker.Entries())
        {
            switch (entityEntry.State)
            {
                case EntityState.Modified when entityEntry.Entity is IEntityWithUpdate:
                    entityEntry.Property(nameof(IEntityWithUpdate.Modified)).CurrentValue = DateTime.Now;
                    entityEntry.Property(nameof(IEntityWithUpdate.ConcurrencyToken)).CurrentValue = Guid.NewGuid().ToString();
                    break;
                case EntityState.Added when entityEntry.Entity is IEntityWithUpdate:
                    entityEntry.Property(nameof(IEntityWithUpdate.Created)).CurrentValue = DateTime.Now;
                    entityEntry.Property(nameof(IEntityWithUpdate.ConcurrencyToken)).CurrentValue = Guid.NewGuid().ToString();
                    break;
                case EntityState.Added when entityEntry.Entity is IEntity:
                    entityEntry.Property(nameof(IEntityWithUpdate.Created)).CurrentValue = DateTime.Now;
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