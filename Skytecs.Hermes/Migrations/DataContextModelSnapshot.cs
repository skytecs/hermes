using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;
using Skytecs.Hermes.Models;

namespace Skytecs.Hermes.Migrations
{
    [DbContext(typeof(DataContext))]
    partial class DataContextModelSnapshot : ModelSnapshot
    {
        protected override void BuildModel(ModelBuilder modelBuilder)
        {
            modelBuilder
                .HasAnnotation("ProductVersion", "1.1.5");

            modelBuilder.Entity("Skytecs.Hermes.Models.Operation", b =>
                {
                    b.Property<long>("Id")
                        .ValueGeneratedOnAdd();

                    b.Property<long>("ClinicOperationId");

                    b.Property<DateTime?>("Confirmed");

                    b.Property<string>("Method");

                    b.Property<DateTime>("Received");

                    b.HasKey("Id");

                    b.ToTable("Operations");
                });
        }
    }
}
