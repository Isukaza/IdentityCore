﻿// <auto-generated />
using System;
using IdentityCore.DAL.MariaDb;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

#nullable disable

namespace IdentityCore.DAL.MariaDb.Migrations
{
    [DbContext(typeof(IdentityCoreDbContext))]
    [Migration("20240624214736_AddedRefreshTokenTable")]
    partial class AddedRefreshTokenTable
    {
        /// <inheritdoc />
        protected override void BuildTargetModel(ModelBuilder modelBuilder)
        {
#pragma warning disable 612, 618
            modelBuilder
                .HasAnnotation("ProductVersion", "8.0.6")
                .HasAnnotation("Relational:MaxIdentifierLength", 64);

            MySqlModelBuilderExtensions.AutoIncrementColumns(modelBuilder);

            modelBuilder.Entity("IdentityCore.DAL.Models.RefreshToken", b =>
                {
                    b.Property<Guid>("Id")
                        .HasColumnType("char(36)");

                    b.Property<DateTime>("Created")
                        .HasColumnType("datetime(6)");

                    b.Property<DateTime>("Expires")
                        .HasColumnType("datetime(6)");

                    b.Property<DateTime>("Modified")
                        .HasColumnType("datetime(6)");

                    b.Property<string>("RefToken")
                        .IsRequired()
                        .HasColumnType("longtext");

                    b.Property<Guid>("UserId")
                        .HasColumnType("char(36)");

                    b.HasKey("Id");

                    b.HasIndex("UserId");

                    b.ToTable("RefreshTokens");
                });

            modelBuilder.Entity("IdentityCore.DAL.Models.User", b =>
                {
                    b.Property<Guid>("Id")
                        .HasColumnType("char(36)");

                    b.Property<DateTime>("Created")
                        .HasColumnType("datetime(6)");

                    b.Property<string>("Email")
                        .IsRequired()
                        .HasColumnType("varchar(255)");

                    b.Property<DateTime>("Modified")
                        .HasColumnType("datetime(6)");

                    b.Property<string>("Password")
                        .IsRequired()
                        .HasMaxLength(255)
                        .HasColumnType("varchar(255)");

                    b.Property<string>("Salt")
                        .IsRequired()
                        .HasColumnType("longtext");

                    b.Property<string>("Username")
                        .IsRequired()
                        .HasMaxLength(16)
                        .HasColumnType("varchar(16)");

                    b.HasKey("Id");

                    b.HasIndex("Email")
                        .IsUnique();

                    b.ToTable("Users");
                });

            modelBuilder.Entity("IdentityCore.DAL.Models.RefreshToken", b =>
                {
                    b.HasOne("IdentityCore.DAL.Models.User", "User")
                        .WithMany("RefreshTokens")
                        .HasForeignKey("UserId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("User");
                });

            modelBuilder.Entity("IdentityCore.DAL.Models.User", b =>
                {
                    b.Navigation("RefreshTokens");
                });
#pragma warning restore 612, 618
        }
    }
}
