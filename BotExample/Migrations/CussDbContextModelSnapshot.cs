﻿// <auto-generated />
using BotExample;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

#nullable disable

namespace BotExample.Migrations
{
    [DbContext(typeof(Database.CussDbContext))]
    partial class CussDbContextModelSnapshot : ModelSnapshot
    {
        protected override void BuildModel(ModelBuilder modelBuilder)
        {
#pragma warning disable 612, 618
            modelBuilder.HasAnnotation("ProductVersion", "6.0.1");

            modelBuilder.Entity("BotExample.Database+CussLog", b =>
                {
                    b.Property<int>("LogId")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("INTEGER");

                    b.Property<ulong>("ChannelId")
                        .HasColumnType("INTEGER");

                    b.Property<string>("CussWord")
                        .IsRequired()
                        .HasColumnType("TEXT");

                    b.Property<ulong>("ServerId")
                        .HasColumnType("INTEGER");

                    b.Property<ulong>("UserId")
                        .HasColumnType("INTEGER");

                    b.HasKey("LogId");

                    b.ToTable("CussLog", (string)null);
                });
#pragma warning restore 612, 618
        }
    }
}
