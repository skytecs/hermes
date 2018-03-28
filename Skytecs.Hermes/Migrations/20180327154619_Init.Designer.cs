using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;
using Skytecs.Hermes.Models;

namespace Skytecs.Hermes.Migrations
{
    [DbContext(typeof(DataContext))]
    [Migration("20180327154619_Init")]
    partial class Init
    {
        protected override void BuildTargetModel(ModelBuilder modelBuilder)
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
