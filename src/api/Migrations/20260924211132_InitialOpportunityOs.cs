using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OpportunityOs.Api.Migrations
{
    /// <inheritdoc />
    public partial class InitialOpportunityOs : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Goals",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    Objective = table.Column<string>(type: "TEXT", nullable: false),
                    Constraints = table.Column<string>(type: "TEXT", nullable: false),
                    SuccessCriteria = table.Column<string>(type: "TEXT", nullable: false),
                    Status = table.Column<string>(type: "TEXT", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Goals", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Opportunities",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    GoalId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Title = table.Column<string>(type: "TEXT", nullable: false),
                    Source = table.Column<string>(type: "TEXT", nullable: false),
                    WhyNow = table.Column<string>(type: "TEXT", nullable: false),
                    MoneyMechanism = table.Column<string>(type: "TEXT", nullable: false),
                    Competition = table.Column<string>(type: "TEXT", nullable: false),
                    Geography = table.Column<string>(type: "TEXT", nullable: false),
                    PayoutRules = table.Column<string>(type: "TEXT", nullable: false),
                    AiRestrictions = table.Column<string>(type: "TEXT", nullable: false),
                    Friction = table.Column<string>(type: "TEXT", nullable: false),
                    ProbabilityToFirstDollar = table.Column<int>(type: "INTEGER", nullable: false),
                    ReusableValue = table.Column<string>(type: "TEXT", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Opportunities", x => x.Id);
                    table.CheckConstraint("CK_Opportunity_ProbabilityToFirstDollar", "ProbabilityToFirstDollar >= 0 AND ProbabilityToFirstDollar <= 100");
                    table.ForeignKey(
                        name: "FK_Opportunities_Goals_GoalId",
                        column: x => x.GoalId,
                        principalTable: "Goals",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Experiments",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    OpportunityId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Hypothesis = table.Column<string>(type: "TEXT", nullable: false),
                    Action = table.Column<string>(type: "TEXT", nullable: false),
                    TimeBudgetMinutes = table.Column<int>(type: "INTEGER", nullable: false),
                    BudgetCents = table.Column<long>(type: "INTEGER", nullable: false),
                    SuccessCondition = table.Column<string>(type: "TEXT", nullable: false),
                    StopCondition = table.Column<string>(type: "TEXT", nullable: false),
                    Status = table.Column<string>(type: "TEXT", nullable: false),
                    Result = table.Column<string>(type: "TEXT", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Experiments", x => x.Id);
                    table.CheckConstraint("CK_Experiment_BudgetCents", "BudgetCents = 0");
                    table.CheckConstraint("CK_Experiment_TimeBudgetMinutes", "TimeBudgetMinutes >= 0");
                    table.ForeignKey(
                        name: "FK_Experiments_Opportunities_OpportunityId",
                        column: x => x.OpportunityId,
                        principalTable: "Opportunities",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Decisions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    ExperimentId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Outcome = table.Column<string>(type: "TEXT", nullable: false),
                    Reason = table.Column<string>(type: "TEXT", nullable: false),
                    DecidedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Decisions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Decisions_Experiments_ExperimentId",
                        column: x => x.ExperimentId,
                        principalTable: "Experiments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Evidence",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    ExperimentId = table.Column<Guid>(type: "TEXT", nullable: false),
                    RecordedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: false),
                    Views = table.Column<long>(type: "INTEGER", nullable: true),
                    Installs = table.Column<long>(type: "INTEGER", nullable: true),
                    Downloads = table.Column<long>(type: "INTEGER", nullable: true),
                    Users = table.Column<long>(type: "INTEGER", nullable: true),
                    Clicks = table.Column<long>(type: "INTEGER", nullable: true),
                    Sales = table.Column<long>(type: "INTEGER", nullable: true),
                    RevenueCents = table.Column<long>(type: "INTEGER", nullable: false),
                    RejectionOrWaitlist = table.Column<string>(type: "TEXT", nullable: false),
                    PlatformFriction = table.Column<string>(type: "TEXT", nullable: false),
                    Feedback = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Evidence", x => x.Id);
                    table.CheckConstraint("CK_Evidence_Clicks", "Clicks IS NULL OR Clicks >= 0");
                    table.CheckConstraint("CK_Evidence_Downloads", "Downloads IS NULL OR Downloads >= 0");
                    table.CheckConstraint("CK_Evidence_Installs", "Installs IS NULL OR Installs >= 0");
                    table.CheckConstraint("CK_Evidence_RevenueCents", "RevenueCents >= 0");
                    table.CheckConstraint("CK_Evidence_Sales", "Sales IS NULL OR Sales >= 0");
                    table.CheckConstraint("CK_Evidence_Users", "Users IS NULL OR Users >= 0");
                    table.CheckConstraint("CK_Evidence_Views", "Views IS NULL OR Views >= 0");
                    table.ForeignKey(
                        name: "FK_Evidence_Experiments_ExperimentId",
                        column: x => x.ExperimentId,
                        principalTable: "Experiments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Decisions_ExperimentId",
                table: "Decisions",
                column: "ExperimentId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Evidence_ExperimentId",
                table: "Evidence",
                column: "ExperimentId");

            migrationBuilder.CreateIndex(
                name: "IX_Experiments_OpportunityId",
                table: "Experiments",
                column: "OpportunityId");

            migrationBuilder.CreateIndex(
                name: "IX_Opportunities_GoalId",
                table: "Opportunities",
                column: "GoalId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Decisions");

            migrationBuilder.DropTable(
                name: "Evidence");

            migrationBuilder.DropTable(
                name: "Experiments");

            migrationBuilder.DropTable(
                name: "Opportunities");

            migrationBuilder.DropTable(
                name: "Goals");
        }
    }
}
