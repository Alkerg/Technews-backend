using Microsoft.EntityFrameworkCore;
using MongoDB.EntityFrameworkCore.Extensions;
using technews_api.Models;

namespace technews_api.Data
{
    public class NewsDbContext(DbContextOptions<NewsDbContext> options): DbContext(options)
    {
        public DbSet<News> News => Set<News>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<News>(e =>
            {
                e.ToCollection("news");
                e.HasKey(n => n.Id);
                e.Property(n => n.Id).ValueGeneratedNever();

                e.Property(n => n.ContentFormat).HasConversion<string>();
                e.OwnsOne(n => n.MainImage);

                e.HasIndex(n => n.PublishedAt);
                e.HasIndex(n => n.Url);
                e.HasIndex(n => n.Slug).IsUnique();
            });
}


    }
}
