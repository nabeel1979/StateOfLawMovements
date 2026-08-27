using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace QanoonCoalition.Web.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddCitizenRequestsModule : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "CitizenRequestStatuses",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    ColorClass = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    DisplayOrder = table.Column<int>(type: "int", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    IsDefault = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CitizenRequestStatuses", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "DocumentTypes",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    DisplayOrder = table.Column<int>(type: "int", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DocumentTypes", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "RequestDestinations",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Type = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    DisplayOrder = table.Column<int>(type: "int", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RequestDestinations", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "CitizenRequests",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    MovementId = table.Column<int>(type: "int", nullable: false),
                    RequestCode = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    RequestDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ApplicantName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    ApplicantPhone = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    ApplicantEmail = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    ContactInformation = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    RequestSubject = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    RequestDetails = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ReceivedByMemberId = table.Column<int>(type: "int", nullable: true),
                    DestinationId = table.Column<int>(type: "int", nullable: true),
                    DestinationSubText = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    SentDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    AnswerDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    StatusId = table.Column<int>(type: "int", nullable: false),
                    CreatedByUserId = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CitizenRequests", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CitizenRequests_CitizenRequestStatuses_StatusId",
                        column: x => x.StatusId,
                        principalTable: "CitizenRequestStatuses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_CitizenRequests_Members_ReceivedByMemberId",
                        column: x => x.ReceivedByMemberId,
                        principalTable: "Members",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_CitizenRequests_Movements_MovementId",
                        column: x => x.MovementId,
                        principalTable: "Movements",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_CitizenRequests_RequestDestinations_DestinationId",
                        column: x => x.DestinationId,
                        principalTable: "RequestDestinations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_CitizenRequests_Users_CreatedByUserId",
                        column: x => x.CreatedByUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "CitizenRequestAttachments",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CitizenRequestId = table.Column<int>(type: "int", nullable: false),
                    FileName = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: false),
                    FilePath = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    ContentType = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    FileSize = table.Column<long>(type: "bigint", nullable: false),
                    DocumentTypeId = table.Column<int>(type: "int", nullable: true),
                    UploadedByUserId = table.Column<int>(type: "int", nullable: false),
                    UploadedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CitizenRequestAttachments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CitizenRequestAttachments_CitizenRequests_CitizenRequestId",
                        column: x => x.CitizenRequestId,
                        principalTable: "CitizenRequests",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_CitizenRequestAttachments_DocumentTypes_DocumentTypeId",
                        column: x => x.DocumentTypeId,
                        principalTable: "DocumentTypes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_CitizenRequestAttachments_Users_UploadedByUserId",
                        column: x => x.UploadedByUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "CitizenRequestStatusHistory",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CitizenRequestId = table.Column<int>(type: "int", nullable: false),
                    FromStatusId = table.Column<int>(type: "int", nullable: true),
                    ToStatusId = table.Column<int>(type: "int", nullable: false),
                    ChangedByUserId = table.Column<int>(type: "int", nullable: false),
                    ChangedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Notes = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CitizenRequestStatusHistory", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CitizenRequestStatusHistory_CitizenRequestStatuses_FromStatusId",
                        column: x => x.FromStatusId,
                        principalTable: "CitizenRequestStatuses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_CitizenRequestStatusHistory_CitizenRequestStatuses_ToStatusId",
                        column: x => x.ToStatusId,
                        principalTable: "CitizenRequestStatuses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_CitizenRequestStatusHistory_CitizenRequests_CitizenRequestId",
                        column: x => x.CitizenRequestId,
                        principalTable: "CitizenRequests",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_CitizenRequestStatusHistory_Users_ChangedByUserId",
                        column: x => x.ChangedByUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.InsertData(
                table: "CitizenRequestStatuses",
                columns: new[] { "Id", "ColorClass", "DisplayOrder", "IsActive", "IsDefault", "Name" },
                values: new object[,]
                {
                    { 1, "warning", 1, true, true, "مستلم" },
                    { 2, "primary", 2, true, false, "مرسل" },
                    { 3, "info", 3, true, false, "إجابة عنه" },
                    { 4, "success", 4, true, false, "منجز" }
                });

            migrationBuilder.InsertData(
                table: "DocumentTypes",
                columns: new[] { "Id", "DisplayOrder", "IsActive", "Name" },
                values: new object[,]
                {
                    { 1, 1, true, "كتاب" },
                    { 2, 2, true, "طلب" },
                    { 3, 3, true, "كتاب إرسال" },
                    { 4, 4, true, "إجابة" }
                });

            migrationBuilder.InsertData(
                table: "RequestDestinations",
                columns: new[] { "Id", "DisplayOrder", "IsActive", "Name", "Type" },
                values: new object[,]
                {
                    { 1, 1, true, "وزارة الداخلية", "وزارة" },
                    { 2, 2, true, "وزارة الخارجية", "وزارة" },
                    { 3, 3, true, "وزارة المالية", "وزارة" },
                    { 4, 4, true, "وزارة التعليم", "وزارة" },
                    { 5, 5, true, "وزارة الصحة", "وزارة" },
                    { 6, 6, true, "وزارة العدل", "وزارة" },
                    { 7, 7, true, "وزارة الكهرباء", "وزارة" },
                    { 8, 8, true, "وزارة الإعمار", "وزارة" },
                    { 9, 9, true, "وزارة الاتصالات", "وزارة" },
                    { 10, 10, true, "وزارة الموارد المائية", "وزارة" },
                    { 11, 11, true, "هيئة النزاهة", "هيئة" },
                    { 12, 12, true, "هيئة الاستثمار", "هيئة" },
                    { 13, 13, true, "مجلس القضاء الأعلى", "هيئة" },
                    { 14, 14, true, "الأمانة العامة لمجلس الوزراء", "دائرة" },
                    { 15, 15, true, "محافظة بغداد", "محافظة" },
                    { 16, 16, true, "محافظة البصرة", "محافظة" },
                    { 17, 17, true, "محافظة النجف", "محافظة" },
                    { 18, 18, true, "محافظة كربلاء", "محافظة" },
                    { 19, 19, true, "مؤسسة الشهداء", "مؤسسة" },
                    { 20, 20, true, "جهة أخرى", "أخرى" }
                });

            migrationBuilder.CreateIndex(
                name: "IX_CitizenRequestAttachments_CitizenRequestId",
                table: "CitizenRequestAttachments",
                column: "CitizenRequestId");

            migrationBuilder.CreateIndex(
                name: "IX_CitizenRequestAttachments_DocumentTypeId",
                table: "CitizenRequestAttachments",
                column: "DocumentTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_CitizenRequestAttachments_UploadedByUserId",
                table: "CitizenRequestAttachments",
                column: "UploadedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_CitizenRequests_CreatedByUserId",
                table: "CitizenRequests",
                column: "CreatedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_CitizenRequests_DestinationId",
                table: "CitizenRequests",
                column: "DestinationId");

            migrationBuilder.CreateIndex(
                name: "IX_CitizenRequests_MovementId",
                table: "CitizenRequests",
                column: "MovementId");

            migrationBuilder.CreateIndex(
                name: "IX_CitizenRequests_ReceivedByMemberId",
                table: "CitizenRequests",
                column: "ReceivedByMemberId");

            migrationBuilder.CreateIndex(
                name: "IX_CitizenRequests_RequestCode",
                table: "CitizenRequests",
                column: "RequestCode",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CitizenRequests_RequestDate",
                table: "CitizenRequests",
                column: "RequestDate");

            migrationBuilder.CreateIndex(
                name: "IX_CitizenRequests_StatusId",
                table: "CitizenRequests",
                column: "StatusId");

            migrationBuilder.CreateIndex(
                name: "IX_CitizenRequestStatuses_DisplayOrder",
                table: "CitizenRequestStatuses",
                column: "DisplayOrder");

            migrationBuilder.CreateIndex(
                name: "IX_CitizenRequestStatusHistory_ChangedByUserId",
                table: "CitizenRequestStatusHistory",
                column: "ChangedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_CitizenRequestStatusHistory_CitizenRequestId",
                table: "CitizenRequestStatusHistory",
                column: "CitizenRequestId");

            migrationBuilder.CreateIndex(
                name: "IX_CitizenRequestStatusHistory_FromStatusId",
                table: "CitizenRequestStatusHistory",
                column: "FromStatusId");

            migrationBuilder.CreateIndex(
                name: "IX_CitizenRequestStatusHistory_ToStatusId",
                table: "CitizenRequestStatusHistory",
                column: "ToStatusId");

            migrationBuilder.CreateIndex(
                name: "IX_RequestDestinations_DisplayOrder",
                table: "RequestDestinations",
                column: "DisplayOrder");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CitizenRequestAttachments");

            migrationBuilder.DropTable(
                name: "CitizenRequestStatusHistory");

            migrationBuilder.DropTable(
                name: "DocumentTypes");

            migrationBuilder.DropTable(
                name: "CitizenRequests");

            migrationBuilder.DropTable(
                name: "CitizenRequestStatuses");

            migrationBuilder.DropTable(
                name: "RequestDestinations");
        }
    }
}
