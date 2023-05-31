using Gidsiks.ProcessTimeChecker.WorkerService.Database.Tables;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Gidsiks.ProcessTimeChecker.WorkerService.Database
{
	public class PTCheckerDbContext : DbContext
	{
		public PTCheckerDbContext(DbContextOptions options) : base(options)
		{
		}


		public DbSet<WatchedApp> WatchedApp { get; set; }
		public DbSet<WatchedAppEvent> WatchedAppEvent { get; set; }
		public DbSet<ActivityEvents> ActivityEvents { get; set; }


		protected override void OnModelCreating(ModelBuilder modelBuilder)
		{
			modelBuilder.Entity<WatchedAppEvent>()
				.HasOne(p => p.WatchedApp)
				.WithMany(t => t.WatchedAppEvents)
				.HasForeignKey(p => p.WatchedAppId);
		}

		protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder) { }

	}
}
