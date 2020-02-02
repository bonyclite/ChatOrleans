using DAL.Models;
using Microsoft.EntityFrameworkCore;

namespace DAL
{
    public class ChatDbContext : DbContext
    {
        public DbSet<UserModel> Users { get; set; }
        public DbSet<ChatModel> Chats { get; set; }
        public DbSet<ChatMessageModel> Messages { get; set; }

        public ChatDbContext(DbContextOptions<ChatDbContext> options) : base(options)
        {
            
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<UserModel>(item =>
            {
                item.HasKey(c => c.Id);
                
                item.HasMany(c => c.Chats)
                    .WithOne(ug => ug.User)
                    .HasForeignKey(model => model.UserId);
            });
            
            modelBuilder.Entity<ChatModel>(entity =>
            {
                entity.HasKey(c => c.Id);
                
                entity.Property(e => e.Name)
                    .IsRequired()
                    .HasMaxLength(500)
                    .IsUnicode(false);

                entity.HasOne(d => d.Owner)
                    .WithMany(p => p.SelfChats)
                    .HasForeignKey(d => d.OwnerId);
            });
            
            modelBuilder.Entity<UserChatModel>(entity =>
            {
                entity.HasKey(c => c.Id);
                
                entity.HasOne(d => d.Chat)
                    .WithMany(p => p.Users)
                    .HasForeignKey(d => d.ChatId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(d => d.User)
                    .WithMany(p => p.Chats)
                    .HasForeignKey(d => d.UserId)
                    .OnDelete(DeleteBehavior.Cascade);
            });
             
            modelBuilder.Entity<ChatMessageModel>(entity =>
            {
                entity.HasKey(c => c.Id);
                
                entity.HasOne(d => d.Chat)
                    .WithMany()
                    .HasForeignKey(d => d.ChatId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(d => d.User)
                    .WithMany()
                    .HasForeignKey(d => d.UserId)
                    .OnDelete(DeleteBehavior.Cascade);
            });
            
            base.OnModelCreating(modelBuilder);
        }
    }
}