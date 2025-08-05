using Auth.Domain.Contracts;
using Auth.Domain.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using System;

namespace Auth.Data.Context
{
	public class ApplicationDbContext : IdentityDbContext<
		UserEntity,
		RoleEntity,
		string,
		IdentityUserClaim<string>,
		UserRoleEntity,
		UserLoginEntity,
		IdentityRoleClaim<string>,
		IdentityUserToken<string>>
	{
		public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
		{
		}

		public DbSet<ActivationCodeEntity> UserActivationCodes { get; set; }
		public DbSet<DeviceEntity> Devices { get; set; }

		protected override void OnModelCreating(ModelBuilder builder)
		{
			base.OnModelCreating(builder);

			foreach (var entity in builder.Model.GetEntityTypes())
			{
				entity.SetTableName(entity.GetTableName().Replace("AspNet", string.Empty));
			}

			builder.HasPostgresExtension("unaccent");

			builder.Entity<UserEntity>(users =>
			{
				users.Property(x => x.CreatedAt)
					.HasDefaultValueSql("now() at time zone 'utc'");

				users.HasMany(x => x.Claims)
					.WithOne()
					.HasForeignKey(x => x.UserId)
					.IsRequired()
					.OnDelete(DeleteBehavior.Cascade);

				users.HasOne(b => b.ActivationCode)
					.WithOne(i => i.User)
					.HasForeignKey<ActivationCodeEntity>(b => b.UserId)
					.IsRequired();

				users.HasOne(u => u.UserLogin)
					.WithOne(i => i.User)
					.HasForeignKey<UserLoginEntity>(b => b.UserId);

				users.ToTable("Users").Property(p => p.Id).HasColumnName("UserId");
			});

			builder.Entity<UserRoleEntity>(entity =>
			{
				entity.ToTable("UserRoles");

				entity.HasKey(n => new { n.UserId, n.RoleId });

				entity.HasOne(n => n.User)
					.WithMany(n => n.UserRoles)
					.HasForeignKey(n => n.UserId)
					.OnDelete(DeleteBehavior.Cascade);

				entity.HasOne(n => n.Role)
					.WithMany(n => n.UserRoles)
					.HasForeignKey(n => n.RoleId)
					.OnDelete(DeleteBehavior.Cascade);
			});

			builder.Entity<UserLoginEntity>(entity =>
			{
				entity.ToTable("UserLogins");

				entity.HasKey(l => new { l.LoginProvider, l.ProviderKey });
			});

			builder.Entity<UserStatusEntity>(entity =>
			{
				entity.ToTable("UserStatus");

				entity.HasMany(s => s.Users)
					.WithOne(u => u.UserStatus)
					.HasForeignKey(u => u.UserStatusId)
					.OnDelete(DeleteBehavior.Cascade);

			});

			builder.Entity<DeviceEntity>(entity =>
			{
				entity.ToTable("Devices");

				entity.HasKey(n => new { n.UserId, n.DeviceType });
				entity.HasIndex(n => new { n.UserId, n.DeviceType });

				entity.HasOne(n => n.User)
					.WithMany(n => n.Devices)
					.HasForeignKey(n => n.UserId)
					.OnDelete(DeleteBehavior.Cascade);
			});

			builder.Entity<IdentityUserClaim<string>>().ToTable("UserClaims");
			builder.Entity<IdentityUserToken<string>>().ToTable("UserTokens");
			builder.Entity<IdentityRoleClaim<string>>().ToTable("RoleClaims");
		}
	}
}
