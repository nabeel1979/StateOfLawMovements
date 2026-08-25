using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace QanoonCoalition.Web.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddExcelFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "District",
                table: "Members",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "EducationLevel",
                table: "Members",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Experiences",
                table: "Members",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "JobTitle",
                table: "Members",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Languages",
                table: "Members",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Province",
                table: "Members",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<DateOnly>(
                name: "ServiceStartDate",
                table: "Members",
                type: "date",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ServiceYears",
                table: "Members",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Skills",
                table: "Members",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Specialization",
                table: "Members",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SubDistrict",
                table: "Members",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "TrainingCourses",
                table: "Members",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "WorkPlace",
                table: "Members",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "District",
                table: "JoinRequests",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "EducationLevel",
                table: "JoinRequests",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Experiences",
                table: "JoinRequests",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "JobTitle",
                table: "JoinRequests",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Languages",
                table: "JoinRequests",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Province",
                table: "JoinRequests",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<DateOnly>(
                name: "ServiceStartDate",
                table: "JoinRequests",
                type: "date",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ServiceYears",
                table: "JoinRequests",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Skills",
                table: "JoinRequests",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Specialization",
                table: "JoinRequests",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SubDistrict",
                table: "JoinRequests",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "TrainingCourses",
                table: "JoinRequests",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "WorkPlace",
                table: "JoinRequests",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "District",
                table: "Members");

            migrationBuilder.DropColumn(
                name: "EducationLevel",
                table: "Members");

            migrationBuilder.DropColumn(
                name: "Experiences",
                table: "Members");

            migrationBuilder.DropColumn(
                name: "JobTitle",
                table: "Members");

            migrationBuilder.DropColumn(
                name: "Languages",
                table: "Members");

            migrationBuilder.DropColumn(
                name: "Province",
                table: "Members");

            migrationBuilder.DropColumn(
                name: "ServiceStartDate",
                table: "Members");

            migrationBuilder.DropColumn(
                name: "ServiceYears",
                table: "Members");

            migrationBuilder.DropColumn(
                name: "Skills",
                table: "Members");

            migrationBuilder.DropColumn(
                name: "Specialization",
                table: "Members");

            migrationBuilder.DropColumn(
                name: "SubDistrict",
                table: "Members");

            migrationBuilder.DropColumn(
                name: "TrainingCourses",
                table: "Members");

            migrationBuilder.DropColumn(
                name: "WorkPlace",
                table: "Members");

            migrationBuilder.DropColumn(
                name: "District",
                table: "JoinRequests");

            migrationBuilder.DropColumn(
                name: "EducationLevel",
                table: "JoinRequests");

            migrationBuilder.DropColumn(
                name: "Experiences",
                table: "JoinRequests");

            migrationBuilder.DropColumn(
                name: "JobTitle",
                table: "JoinRequests");

            migrationBuilder.DropColumn(
                name: "Languages",
                table: "JoinRequests");

            migrationBuilder.DropColumn(
                name: "Province",
                table: "JoinRequests");

            migrationBuilder.DropColumn(
                name: "ServiceStartDate",
                table: "JoinRequests");

            migrationBuilder.DropColumn(
                name: "ServiceYears",
                table: "JoinRequests");

            migrationBuilder.DropColumn(
                name: "Skills",
                table: "JoinRequests");

            migrationBuilder.DropColumn(
                name: "Specialization",
                table: "JoinRequests");

            migrationBuilder.DropColumn(
                name: "SubDistrict",
                table: "JoinRequests");

            migrationBuilder.DropColumn(
                name: "TrainingCourses",
                table: "JoinRequests");

            migrationBuilder.DropColumn(
                name: "WorkPlace",
                table: "JoinRequests");
        }
    }
}
