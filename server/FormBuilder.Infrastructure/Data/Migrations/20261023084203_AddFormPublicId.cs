using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FormBuilder.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddFormPublicId : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
{
    migrationBuilder.AddColumn<string>(
        name: "PublicId",
        table: "Forms",
        type: "text",
        nullable: true);

    migrationBuilder.Sql(
        """
        UPDATE "Forms"
        SET "PublicId" = REPLACE(gen_random_uuid()::text, '-', '')
        WHERE "PublicId" IS NULL;
        """);

    migrationBuilder.AlterColumn<string>(
        name: "PublicId",
        table: "Forms",
        type: "text",
        nullable: false,
        oldClrType: typeof(string),
        oldType: "text",
        oldNullable: true);
}

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "PublicId",
                table: "Forms");
        }
    }
}
