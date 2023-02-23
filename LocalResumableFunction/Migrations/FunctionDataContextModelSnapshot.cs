﻿// <auto-generated />
using System;
using LocalResumableFunction.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

#nullable disable

namespace LocalResumableFunction.Migrations
{
    [DbContext(typeof(FunctionDataContext))]
    partial class FunctionDataContextModelSnapshot : ModelSnapshot
    {
        protected override void BuildModel(ModelBuilder modelBuilder)
        {
#pragma warning disable 612, 618
            modelBuilder.HasAnnotation("ProductVersion", "7.0.2");

            modelBuilder.Entity("LocalResumableFunction.InOuts.MethodIdentifier", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("INTEGER");

                    b.Property<string>("AssemblyName")
                        .HasColumnType("TEXT");

                    b.Property<string>("ClassName")
                        .HasColumnType("TEXT");

                    b.Property<byte[]>("MethodHash")
                        .HasMaxLength(16)
                        .HasColumnType("BLOB");

                    b.Property<string>("MethodName")
                        .HasColumnType("TEXT");

                    b.Property<string>("MethodSignature")
                        .HasColumnType("TEXT");

                    b.Property<int>("Type")
                        .HasColumnType("INTEGER");

                    b.HasKey("Id");

                    b.HasIndex(new[] { "MethodHash" }, "Index_MethodHash")
                        .IsUnique();

                    b.ToTable("MethodIdentifiers");
                });

            modelBuilder.Entity("LocalResumableFunction.InOuts.ResumableFunctionState", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("INTEGER");

                    b.Property<bool>("IsCompleted")
                        .HasColumnType("INTEGER");

                    b.Property<bool>("IsInProcessing")
                        .HasColumnType("INTEGER");

                    b.Property<int>("ResumableFunctionIdentifierId")
                        .HasColumnType("INTEGER");

                    b.Property<string>("StateObject")
                        .HasColumnType("TEXT");

                    b.HasKey("Id");

                    b.HasIndex("ResumableFunctionIdentifierId");

                    b.ToTable("FunctionStates");
                });

            modelBuilder.Entity("LocalResumableFunction.InOuts.Wait", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("INTEGER");

                    b.Property<string>("Discriminator")
                        .IsRequired()
                        .HasColumnType("TEXT");

                    b.Property<int>("FunctionStateId")
                        .HasColumnType("INTEGER");

                    b.Property<bool>("IsFirst")
                        .HasColumnType("INTEGER");

                    b.Property<bool>("IsNode")
                        .HasColumnType("INTEGER");

                    b.Property<string>("Name")
                        .HasColumnType("TEXT");

                    b.Property<int?>("ParentWaitId")
                        .HasColumnType("INTEGER");

                    b.Property<int>("RequestedByFunctionId")
                        .HasColumnType("INTEGER");

                    b.Property<int>("StateAfterWait")
                        .HasColumnType("INTEGER");

                    b.Property<int>("StateBeforeWait")
                        .HasColumnType("INTEGER");

                    b.Property<int>("Status")
                        .HasColumnType("INTEGER");

                    b.Property<int>("WaitType")
                        .HasColumnType("INTEGER");

                    b.HasKey("Id");

                    b.HasIndex("FunctionStateId");

                    b.HasIndex("ParentWaitId");

                    b.HasIndex("RequestedByFunctionId");

                    b.ToTable("Waits");

                    b.HasDiscriminator<string>("Discriminator").HasValue("Wait");

                    b.UseTphMappingStrategy();
                });

            modelBuilder.Entity("LocalResumableFunction.InOuts.FunctionWait", b =>
                {
                    b.HasBaseType("LocalResumableFunction.InOuts.Wait");

                    b.HasDiscriminator().HasValue("FunctionWait");
                });

            modelBuilder.Entity("LocalResumableFunction.InOuts.MethodWait", b =>
                {
                    b.HasBaseType("LocalResumableFunction.InOuts.Wait");

                    b.Property<byte[]>("MatchIfExpressionValue")
                        .HasColumnType("BLOB")
                        .HasColumnName("MatchIfExpressionValue");

                    b.Property<bool>("NeedFunctionStateForMatch")
                        .HasColumnType("INTEGER");

                    b.Property<byte[]>("SetDataExpressionValue")
                        .HasColumnType("BLOB")
                        .HasColumnName("SetDataExpressionValue");

                    b.Property<int>("WaitMethodIdentifierId")
                        .HasColumnType("INTEGER");

                    b.HasIndex("WaitMethodIdentifierId");

                    b.HasDiscriminator().HasValue("MethodWait");
                });

            modelBuilder.Entity("LocalResumableFunction.InOuts.WaitsGroup", b =>
                {
                    b.HasBaseType("LocalResumableFunction.InOuts.Wait");

                    b.Property<byte[]>("CountExpressionValue")
                        .HasColumnType("BLOB")
                        .HasColumnName("CountExpressionValue");

                    b.HasDiscriminator().HasValue("WaitsGroup");
                });

            modelBuilder.Entity("LocalResumableFunction.InOuts.ResumableFunctionState", b =>
                {
                    b.HasOne("LocalResumableFunction.InOuts.MethodIdentifier", "ResumableFunctionIdentifier")
                        .WithMany("ActiveFunctionsStates")
                        .HasForeignKey("ResumableFunctionIdentifierId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired()
                        .HasConstraintName("FK_FunctionsStates_For_ResumableFunction");

                    b.Navigation("ResumableFunctionIdentifier");
                });

            modelBuilder.Entity("LocalResumableFunction.InOuts.Wait", b =>
                {
                    b.HasOne("LocalResumableFunction.InOuts.ResumableFunctionState", "FunctionState")
                        .WithMany("Waits")
                        .HasForeignKey("FunctionStateId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired()
                        .HasConstraintName("FK_Waits_For_FunctionState");

                    b.HasOne("LocalResumableFunction.InOuts.Wait", "ParentWait")
                        .WithMany("ChildWaits")
                        .HasForeignKey("ParentWaitId")
                        .HasConstraintName("FK_ChildWaits_For_Wait");

                    b.HasOne("LocalResumableFunction.InOuts.MethodIdentifier", "RequestedByFunction")
                        .WithMany("WaitsCreatedByFunction")
                        .HasForeignKey("RequestedByFunctionId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired()
                        .HasConstraintName("FK_Waits_In_ResumableFunction");

                    b.Navigation("FunctionState");

                    b.Navigation("ParentWait");

                    b.Navigation("RequestedByFunction");
                });

            modelBuilder.Entity("LocalResumableFunction.InOuts.MethodWait", b =>
                {
                    b.HasOne("LocalResumableFunction.InOuts.MethodIdentifier", "WaitMethodIdentifier")
                        .WithMany("WaitsRequestsForMethod")
                        .HasForeignKey("WaitMethodIdentifierId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired()
                        .HasConstraintName("FK_Waits_RequestedForMethod");

                    b.Navigation("WaitMethodIdentifier");
                });

            modelBuilder.Entity("LocalResumableFunction.InOuts.MethodIdentifier", b =>
                {
                    b.Navigation("ActiveFunctionsStates");

                    b.Navigation("WaitsCreatedByFunction");

                    b.Navigation("WaitsRequestsForMethod");
                });

            modelBuilder.Entity("LocalResumableFunction.InOuts.ResumableFunctionState", b =>
                {
                    b.Navigation("Waits");
                });

            modelBuilder.Entity("LocalResumableFunction.InOuts.Wait", b =>
                {
                    b.Navigation("ChildWaits");
                });
#pragma warning restore 612, 618
        }
    }
}
