using System.Diagnostics;
using System.Linq.Expressions;
using System.Reflection.Metadata;
using ResumableFunctions.Core.Helpers;
using ResumableFunctions.Core.InOuts;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System.Reflection.Emit;
using System.Text.Json;
using Newtonsoft.Json;

namespace ResumableFunctions.Core.Data;

public class FunctionDataContext : DbContext
{
    public FunctionDataContext(IResumableFunctionSettings settings) : base(settings.WaitsDbConfig.Options)
    {
        Database.EnsureCreated();
    }

    public DbSet<ResumableFunctionState> FunctionStates { get; set; }
    public DbSet<MethodIdentifier> MethodIdentifiers { get; set; }
    public DbSet<Wait> Waits { get; set; }
    public DbSet<MethodWait> MethodWaits { get; set; }
    public DbSet<FunctionWait> FunctionWaits { get; set; }
    public DbSet<PushedMethod> PushedMethodsCalls { get; set; }
    public DbSet<ServiceData> ServicesData { get; set; }
    public DbSet<ExternalMethodRecord> ExternalMethodsRegistry { get; set; }


    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        ConfigureResumableFunctionState(modelBuilder.Entity<ResumableFunctionState>());
        ConfigureMethodIdentifier(modelBuilder.Entity<MethodIdentifier>());
        ConfigurePushedMethod(modelBuilder.Entity<PushedMethod>());
        ConfigureServiceData(modelBuilder.Entity<ServiceData>());
        ConfigureExternalMethodRecord(modelBuilder.Entity<ExternalMethodRecord>());
        ConfigureWaits(modelBuilder);
        base.OnModelCreating(modelBuilder);
    }

    private void ConfigureExternalMethodRecord(EntityTypeBuilder<ExternalMethodRecord> entityTypeBuilder)
    {
        entityTypeBuilder
          .Property(x => x.MethodData)
          .HasConversion(
           v => JsonConvert.SerializeObject(v),
           v => JsonConvert.DeserializeObject<MethodData>(v));

        entityTypeBuilder
            .HasIndex(x => x.MethodHash)
            .HasDatabaseName("Index_ExternalMethodHash")
            .IsUnique(true);

        entityTypeBuilder
          .Property(x => x.MethodHash)
          .HasMaxLength(16);

        entityTypeBuilder
         .Property(x => x.OriginalMethodHash)
         .HasMaxLength(16);
    }

    private void ConfigureServiceData(EntityTypeBuilder<ServiceData> entityTypeBuilder)
    {
        entityTypeBuilder.Property(x => x.LastScanDate).HasDefaultValue(DateTime.MinValue);
        entityTypeBuilder.HasIndex(x => x.AssemblyName);
    }

    private void ConfigurePushedMethod(EntityTypeBuilder<PushedMethod> entityTypeBuilder)
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
           .Property<DateTime>(ConstantValue.CreatedProp);

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
           .Property(mw => mw.CountExpressionValue)
           .HasColumnName(nameof(WaitsGroup.CountExpressionValue));

        modelBuilder.Ignore<ReplayRequest>();
        modelBuilder.Ignore<TimeWait>();
    }

    private void ConfigureMethodIdentifier(EntityTypeBuilder<MethodIdentifier> entityTypeBuilder)
    {
        entityTypeBuilder
           .HasMany(x => x.ActiveFunctionsStates)
           .WithOne(wait => wait.ResumableFunctionIdentifier)
           .HasForeignKey(x => x.ResumableFunctionIdentifierId)
           .HasConstraintName("FK_FunctionsStates_For_ResumableFunction");

        entityTypeBuilder
            .HasMany(x => x.WaitsCreatedByFunction)
            .WithOne(wait => wait.RequestedByFunction)
            .OnDelete(DeleteBehavior.Restrict)
            .HasForeignKey(x => x.RequestedByFunctionId)
            .HasConstraintName("FK_Waits_In_ResumableFunction");

        entityTypeBuilder
            .HasMany(x => x.WaitsRequestsForMethod)
            .WithOne(wait => wait.WaitMethodIdentifier)
            .OnDelete(DeleteBehavior.Restrict)
            .HasForeignKey(x => x.WaitMethodIdentifierId)
            .HasConstraintName("FK_Waits_RequestedForMethod");

        entityTypeBuilder
            .HasIndex(x => x.MethodHash)
            .HasDatabaseName("Index_MethodHash")
            .IsUnique(true);

        entityTypeBuilder
            .Property(x => x.MethodHash)
            .HasMaxLength(16);

        entityTypeBuilder
         .Property<DateTime>(ConstantValue.CreatedProp);
    }

    private void ConfigureResumableFunctionState(EntityTypeBuilder<ResumableFunctionState> entityTypeBuilder)
    {
        entityTypeBuilder
            .HasMany(x => x.Waits)
            .WithOne(wait => wait.FunctionState)
            .HasForeignKey(x => x.FunctionStateId)
            .HasConstraintName("FK_Waits_For_FunctionState");
        entityTypeBuilder
        .Property<DateTime>(ConstantValue.LastUpdatedProp);
        entityTypeBuilder
           .Property<DateTime>(ConstantValue.CreatedProp);
        entityTypeBuilder
           .Property(x => x.StateObject)
           .HasConversion<ObjectToJsonConverter>();
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