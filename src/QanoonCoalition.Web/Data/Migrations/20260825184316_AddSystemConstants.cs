using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace QanoonCoalition.Web.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddSystemConstants : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "SystemConstants",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Category = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Value = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    DisplayOrder = table.Column<int>(type: "int", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SystemConstants", x => x.Id);
                });

            migrationBuilder.InsertData(
                table: "SystemConstants",
                columns: new[] { "Id", "Category", "DisplayOrder", "IsActive", "Value" },
                values: new object[,]
                {
                    { 1, "EducationLevel", 2, true, "أمي" },
                    { 2, "EducationLevel", 3, true, "يقرأ ويكتب" },
                    { 3, "EducationLevel", 4, true, "ابتدائية" },
                    { 4, "EducationLevel", 5, true, "متوسطة" },
                    { 5, "EducationLevel", 6, true, "إعدادية" },
                    { 6, "EducationLevel", 7, true, "دبلوم" },
                    { 7, "EducationLevel", 8, true, "بكالوريوس" },
                    { 8, "EducationLevel", 9, true, "دبلوم عالي" },
                    { 9, "EducationLevel", 10, true, "ماجستير" },
                    { 10, "EducationLevel", 11, true, "دكتوراه" },
                    { 11, "BenefitField", 12, true, "تنظيمي وإداري" },
                    { 12, "BenefitField", 13, true, "إعلامي" },
                    { 13, "BenefitField", 14, true, "قانوني" },
                    { 14, "BenefitField", 15, true, "مالي ومحاسبي" },
                    { 15, "BenefitField", 16, true, "طبي وصحي" },
                    { 16, "BenefitField", 17, true, "هندسي وفني" },
                    { 17, "BenefitField", 18, true, "تربوي وتعليمي" },
                    { 18, "BenefitField", 19, true, "تقنية المعلومات" },
                    { 19, "BenefitField", 20, true, "علاقات عامة" },
                    { 20, "BenefitField", 21, true, "لوجستي وخدمي" },
                    { 21, "BenefitField", 22, true, "تحريري وجماهيري" },
                    { 22, "BenefitField", 23, true, "بحوث ودراسات" },
                    { 23, "BenefitField", 24, true, "أخرى" },
                    { 24, "ManagerTitle", 25, true, "رئيس" },
                    { 25, "ManagerTitle", 26, true, "نائب رئيس" },
                    { 26, "ManagerTitle", 27, true, "أمين سر" },
                    { 27, "ManagerTitle", 28, true, "عضو مجلس" },
                    { 28, "ManagerTitle", 29, true, "موظف" },
                    { 29, "ManagerTitle", 30, true, "منسق" },
                    { 30, "ManagerTitle", 31, true, "متطوع" }
                });

            migrationBuilder.CreateIndex(
                name: "IX_SystemConstants_Category_Value",
                table: "SystemConstants",
                columns: new[] { "Category", "Value" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "SystemConstants");
        }
    }
}
