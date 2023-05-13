using EcoQuest.Models.Entities;
using Microsoft.EntityFrameworkCore;

namespace EcoQuest.Models
{
    public partial class eco_questContext : DbContext
    {
        public eco_questContext() { }
        public eco_questContext(DbContextOptions<eco_questContext> options) : base(options) { }

        public virtual DbSet<CmdaExec> CmdaExecs { get; set; } = null!;
        public virtual DbSet<FlywaySchemaHistory> FlywaySchemaHistories { get; set; } = null!;
        public virtual DbSet<Game> Games { get; set; } = null!;
        public virtual DbSet<GameBoard> GameBoards { get; set; } = null!;
        public virtual DbSet<GameBoardsProduct> GameBoardsProducts { get; set; } = null!;
        public virtual DbSet<Product> Products { get; set; } = null!;
        public virtual DbSet<Question> Questions { get; set; } = null!;
        public virtual DbSet<Statistic> Statistics { get; set; } = null!;
        public virtual DbSet<User> Users { get; set; } = null!;

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder) { }
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<CmdaExec>(entity =>
            {
                entity.HasNoKey();
                entity.ToTable("cmda_exec");
                entity.Property(e => e.CmdaOutput).HasColumnName("cmda_output");
            });

            modelBuilder.Entity<FlywaySchemaHistory>(entity =>
            {
                entity.HasKey(e => e.InstalledRank).HasName("flyway_schema_history_pk");
                entity.ToTable("flyway_schema_history", "\"$user\", public");
                entity.HasIndex(e => e.Success, "flyway_schema_history_s_idx");
                entity.Property(e => e.InstalledRank).ValueGeneratedNever().HasColumnName("installed_rank");
                entity.Property(e => e.Checksum).HasColumnName("checksum");
                entity.Property(e => e.Description).HasMaxLength(200).HasColumnName("description");
                entity.Property(e => e.ExecutionTime).HasColumnName("execution_time");
                entity.Property(e => e.InstalledBy).HasMaxLength(100).HasColumnName("installed_by");
                entity.Property(e => e.InstalledOn).HasColumnType("timestamp without time zone").HasColumnName("installed_on").HasDefaultValueSql("now()");
                entity.Property(e => e.Script).HasMaxLength(1000).HasColumnName("script");
                entity.Property(e => e.Success).HasColumnName("success");
                entity.Property(e => e.Type).HasMaxLength(20).HasColumnName("type");
                entity.Property(e => e.Version).HasMaxLength(50).HasColumnName("version");
            });

            modelBuilder.Entity<Game>(entity =>
            {
                entity.ToTable("games");
                entity.HasIndex(e => e.GameId, "games_game_id_unique").IsUnique();
                entity.Property(e => e.GameId).ValueGeneratedNever().HasColumnName("game_id");
                entity.Property(e => e.CurrentQuestionId).HasColumnName("current_question_id");
                entity.Property(e => e.Date).HasColumnType("character varying").HasColumnName("date");
                entity.Property(e => e.Message).HasColumnType("character varying").HasColumnName("message");
                entity.Property(e => e.Name).HasColumnType("character varying").HasColumnName("name");
                entity.Property(e => e.State).HasColumnType("json").HasColumnName("state");
                entity.Property(e => e.UserId).HasColumnName("user_id");
                entity.HasOne(d => d.User).WithMany(p => p.Games).HasForeignKey(d => d.UserId).HasConstraintName("games_fkey");
            });

            modelBuilder.Entity<GameBoard>(entity =>
            {
                entity.ToTable("game_boards");
                entity.HasIndex(e => e.GameBoardId, "game_boards_game_board_id_unique").IsUnique();
                entity.Property(e => e.GameBoardId).HasColumnName("game_board_id").UseIdentityAlwaysColumn();
                entity.Property(e => e.Name).HasColumnType("character varying").HasColumnName("name");
                entity.Property(e => e.NumFields).HasColumnName("num_fields");
                entity.Property(e => e.UserId).HasColumnName("user_id");
                entity.HasOne(d => d.User).WithMany(p => p.GameBoards).HasForeignKey(d => d.UserId).HasConstraintName("game_boards_fkey");
                entity.HasMany(d => d.Questions).WithMany(p => p.GameBoards).UsingEntity<Dictionary<string, object>>(
                        "GameBoardsQuestion",
                        l => l.HasOne<Question>().WithMany().HasForeignKey("QuestionId").HasConstraintName("game_boards_questions_question_id_fkey"),
                        r => r.HasOne<GameBoard>().WithMany().HasForeignKey("GameBoardId").HasConstraintName("game_boards_questions_game_board_id_fkey"),
                        j =>
                        {
                            j.HasKey("GameBoardId", "QuestionId").HasName("game_boards_questions_pkey");
                            j.ToTable("game_boards_questions");
                            j.IndexerProperty<long>("GameBoardId").HasColumnName("game_board_id");
                            j.IndexerProperty<long>("QuestionId").HasColumnName("question_id");
                        });
            });

            modelBuilder.Entity<GameBoardsProduct>(entity =>
            {
                entity.HasKey(e => new { e.GameBoardId, e.ProductId }).HasName("game_boards_products_pkey");
                entity.ToTable("game_boards_products");
                entity.Property(e => e.GameBoardId).HasColumnName("game_board_id");
                entity.Property(e => e.ProductId).HasColumnName("product_id");
                entity.Property(e => e.NumOfRepeating).HasColumnName("num_of_repeating");
                entity.HasOne(d => d.GameBoard).WithMany(p => p.GameBoardsProducts).HasForeignKey(d => d.GameBoardId).HasConstraintName("game_boards_products_game_board_id_fkey");
                entity.HasOne(d => d.Product).WithMany(p => p.GameBoardsProducts).HasForeignKey(d => d.ProductId).HasConstraintName("game_boards_products_product_id_fkey");
            });

            modelBuilder.Entity<Product>(entity =>
            {
                entity.ToTable("products");
                entity.HasIndex(e => e.Name, "products_name_unique").IsUnique();
                entity.HasIndex(e => e.ProductId, "products_product_id_unique").IsUnique();
                entity.Property(e => e.ProductId).HasColumnName("product_id").UseIdentityAlwaysColumn();
                entity.Property(e => e.Colour).HasColumnType("character varying").HasColumnName("colour");
                entity.Property(e => e.Logo).HasColumnType("character varying").HasColumnName("logo");
                entity.Property(e => e.Name).HasColumnType("character varying").HasColumnName("name");
                entity.Property(e => e.Round).HasColumnName("round");
            });

            modelBuilder.Entity<Question>(entity =>
            {
                entity.ToTable("questions");
                entity.HasIndex(e => e.QuestionId, "questions_question_id_unique").IsUnique();
                entity.Property(e => e.QuestionId).HasColumnName("question_id").UseIdentityAlwaysColumn();
                entity.Property(e => e.Answers).HasColumnType("character varying").HasColumnName("answers");
                entity.Property(e => e.LastEditDate).HasColumnType("character varying").HasColumnName("last_edit_date");
                entity.Property(e => e.Media).HasColumnType("character varying").HasColumnName("media");
                entity.Property(e => e.ProductId).HasColumnName("product_id");
                entity.Property(e => e.ShortText).HasColumnType("character varying").HasColumnName("short_text");
                entity.Property(e => e.Text).HasColumnType("character varying").HasColumnName("text");
                entity.Property(e => e.Type).HasColumnType("character varying").HasColumnName("type");
                entity.HasOne(d => d.Product).WithMany(p => p.Questions).HasForeignKey(d => d.ProductId).HasConstraintName("questions_fkey");
            });

            modelBuilder.Entity<Statistic>(entity =>
            {
                entity.HasKey(e => e.RecordId).HasName("statistics_pkey");
                entity.ToTable("statistics");
                entity.HasIndex(e => e.RecordId, "statistics_record_id_unique").IsUnique();
                entity.Property(e => e.RecordId).HasColumnName("record_id").UseIdentityAlwaysColumn();
                entity.Property(e => e.Date).HasColumnType("character varying").HasColumnName("date");
                entity.Property(e => e.Duration).HasColumnType("character varying").HasColumnName("duration");
                entity.Property(e => e.FirstName).HasColumnType("character varying").HasColumnName("first name");
                entity.Property(e => e.LastName).HasColumnType("character varying").HasColumnName("last name");
                entity.Property(e => e.Login).HasColumnType("character varying").HasColumnName("login");
                entity.Property(e => e.Patronymic).HasColumnType("character varying").HasColumnName("patronymic");
                entity.Property(e => e.Results).HasColumnType("json").HasColumnName("results");
                entity.Property(e => e.UserId).HasColumnName("user_id");
            });

            modelBuilder.Entity<User>(entity =>
            {
                entity.ToTable("users");
                entity.HasIndex(e => e.Login, "users_login_unique").IsUnique();
                entity.HasIndex(e => e.UserId, "users_user_id_unique").IsUnique();
                entity.Property(e => e.UserId).HasColumnName("user_id").UseIdentityAlwaysColumn();
                entity.Property(e => e.FirstName).HasColumnType("character varying").HasColumnName("first name");
                entity.Property(e => e.LastName).HasColumnType("character varying").HasColumnName("last name");
                entity.Property(e => e.Login).HasColumnType("character varying").HasColumnName("login");
                entity.Property(e => e.Password).HasColumnType("character varying").HasColumnName("password");
                entity.Property(e => e.Patronymic).HasColumnType("character varying").HasColumnName("patronymic");
                entity.Property(e => e.Role).HasColumnType("character varying").HasColumnName("role");
                entity.Property(e => e.Status).HasColumnType("character varying").HasColumnName("status");
            });

            OnModelCreatingPartial(modelBuilder);
        }
        partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
    }
}