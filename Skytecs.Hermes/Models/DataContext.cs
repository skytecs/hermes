using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Skytecs.Hermes.Models
{
    public class DataContext : DbContext
    {
        public DbSet<Operation> Operations { get; set; }



        public DataContext(DbContextOptions<DataContext> options) : base(options)
        {
        }
    }
}
