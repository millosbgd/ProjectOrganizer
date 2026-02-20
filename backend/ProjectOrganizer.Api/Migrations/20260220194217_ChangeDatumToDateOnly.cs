using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ProjectOrganizer.Api.Migrations
{
    /// <inheritdoc />
    public partial class ChangeDatumToDateOnly : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "CheckListItems",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Opis = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    Kompleksnost = table.Column<decimal>(type: "decimal(18,2)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CheckListItems", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "CodebookEntities",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CodebookEntities", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "DocumentNumbering",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Year = table.Column<int>(type: "int", nullable: false),
                    DocumentType = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    LastNumber = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DocumentNumbering", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ImplementationModels",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Naziv = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    Opis = table.Column<string>(type: "nvarchar(400)", maxLength: 400, nullable: true),
                    Aktivan = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ImplementationModels", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Klijenti",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Naziv = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Pib = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    MaticniBroj = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    Adresa = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: true),
                    Grad = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    Zemlja = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Klijenti", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Users",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Auth0Id = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    Email = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    Role = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    LastLogin = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Users", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "UserSettings",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    OpenAiApiKey = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    OpenAiModel = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    DevOpsPersonalAccessToken = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserSettings", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Codebooks",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    EntityTypeId = table.Column<int>(type: "int", nullable: false),
                    Code = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Value = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    OrderIndex = table.Column<int>(type: "int", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Codebooks", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Codebooks_CodebookEntities_EntityTypeId",
                        column: x => x.EntityTypeId,
                        principalTable: "CodebookEntities",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ImplementationItems",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ImplementationModelId = table.Column<int>(type: "int", nullable: false),
                    Naziv = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Detalji = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ImplementationItems", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ImplementationItems_ImplementationModels_ImplementationModelId",
                        column: x => x.ImplementationModelId,
                        principalTable: "ImplementationModels",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Projekti",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    BrojProjekta = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Datum = table.Column<DateOnly>(type: "date", nullable: false),
                    Naziv = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: false),
                    Aktivan = table.Column<bool>(type: "bit", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    KlijentId = table.Column<int>(type: "int", nullable: false),
                    CreatedBy = table.Column<int>(type: "int", nullable: true),
                    ImplementationModelId = table.Column<int>(type: "int", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    DevOpsOrganization = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    DevOpsProject = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    DevOpsAreaPath = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    DevOpsIterationPath = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Projekti", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Projekti_ImplementationModels_ImplementationModelId",
                        column: x => x.ImplementationModelId,
                        principalTable: "ImplementationModels",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_Projekti_Klijenti_KlijentId",
                        column: x => x.KlijentId,
                        principalTable: "Klijenti",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Projekti_Users_CreatedBy",
                        column: x => x.CreatedBy,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "ImplementationItemCheckListItems",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ImplementationItemId = table.Column<int>(type: "int", nullable: false),
                    CheckListItemId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ImplementationItemCheckListItems", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ImplementationItemCheckListItems_CheckListItems_CheckListItemId",
                        column: x => x.CheckListItemId,
                        principalTable: "CheckListItems",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ImplementationItemCheckListItems_ImplementationItems_ImplementationItemId",
                        column: x => x.ImplementationItemId,
                        principalTable: "ImplementationItems",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Dokumenti",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ProjekatId = table.Column<int>(type: "int", nullable: false),
                    NazivFajla = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    TipFajla = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    BlobUrl = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    Velicina = table.Column<long>(type: "bigint", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Dokumenti", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Dokumenti_Projekti_ProjekatId",
                        column: x => x.ProjekatId,
                        principalTable: "Projekti",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Notes",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ProjekatId = table.Column<int>(type: "int", nullable: false),
                    Opis = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Notes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Notes_Projekti_ProjekatId",
                        column: x => x.ProjekatId,
                        principalTable: "Projekti",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ProjectImplementationItems",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ImplementationModelId = table.Column<int>(type: "int", nullable: false),
                    ProjectId = table.Column<int>(type: "int", nullable: false),
                    ImplementationItemId = table.Column<int>(type: "int", nullable: false),
                    Napomena = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: true),
                    Zavrseno = table.Column<bool>(type: "bit", nullable: false),
                    ZavrsenoDatum = table.Column<DateTime>(type: "datetime2", nullable: true),
                    KlijentPotvrdio = table.Column<bool>(type: "bit", nullable: false),
                    KlijentPotvrdioDatum = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProjectImplementationItems", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ProjectImplementationItems_ImplementationItems_ImplementationItemId",
                        column: x => x.ImplementationItemId,
                        principalTable: "ImplementationItems",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ProjectImplementationItems_ImplementationModels_ImplementationModelId",
                        column: x => x.ImplementationModelId,
                        principalTable: "ImplementationModels",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ProjectImplementationItems_Projekti_ProjectId",
                        column: x => x.ProjectId,
                        principalTable: "Projekti",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ProjectPermissions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<int>(type: "int", nullable: false),
                    ProjekatId = table.Column<int>(type: "int", nullable: false),
                    PermissionLevel = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProjectPermissions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ProjectPermissions_Projekti_ProjekatId",
                        column: x => x.ProjekatId,
                        principalTable: "Projekti",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ProjectPermissions_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Aktivnosti",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Opis = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Detalji = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: false),
                    Datum = table.Column<DateTime>(type: "datetime2", nullable: false),
                    StartUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    EndUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Status = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Vrsta = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Bau = table.Column<bool>(type: "bit", nullable: false),
                    ProjekatId = table.Column<int>(type: "int", nullable: true),
                    ProjectImplementationItemId = table.Column<int>(type: "int", nullable: true),
                    CreatedBy = table.Column<int>(type: "int", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Aktivnosti", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Aktivnosti_ProjectImplementationItems_ProjectImplementationItemId",
                        column: x => x.ProjectImplementationItemId,
                        principalTable: "ProjectImplementationItems",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Aktivnosti_Projekti_ProjekatId",
                        column: x => x.ProjekatId,
                        principalTable: "Projekti",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Aktivnosti_Users_CreatedBy",
                        column: x => x.CreatedBy,
                        principalTable: "Users",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "ProjectImplementationItemCheckLists",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ProjectImplementationItemId = table.Column<int>(type: "int", nullable: false),
                    CheckListItemId = table.Column<int>(type: "int", nullable: false),
                    Zavrsen = table.Column<bool>(type: "bit", nullable: false),
                    ZavrsenDatum = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Procenat = table.Column<decimal>(type: "decimal(5,2)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProjectImplementationItemCheckLists", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ProjectImplementationItemCheckLists_CheckListItems_CheckListItemId",
                        column: x => x.CheckListItemId,
                        principalTable: "CheckListItems",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ProjectImplementationItemCheckLists_ProjectImplementationItems_ProjectImplementationItemId",
                        column: x => x.ProjectImplementationItemId,
                        principalTable: "ProjectImplementationItems",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "DevOpsTasksCandidates",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    AktivnostId = table.Column<int>(type: "int", nullable: false),
                    UserId = table.Column<int>(type: "int", nullable: false),
                    Title = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    AcceptanceCriteria = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Priority = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    Estimation = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    OrderIndex = table.Column<int>(type: "int", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DevOpsTasksCandidates", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DevOpsTasksCandidates_Aktivnosti_AktivnostId",
                        column: x => x.AktivnostId,
                        principalTable: "Aktivnosti",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_DevOpsTasksCandidates_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Aktivnosti_CreatedBy",
                table: "Aktivnosti",
                column: "CreatedBy");

            migrationBuilder.CreateIndex(
                name: "IX_Aktivnosti_ProjectImplementationItemId",
                table: "Aktivnosti",
                column: "ProjectImplementationItemId");

            migrationBuilder.CreateIndex(
                name: "IX_Aktivnosti_ProjekatId",
                table: "Aktivnosti",
                column: "ProjekatId");

            migrationBuilder.CreateIndex(
                name: "IX_Aktivnosti_Status",
                table: "Aktivnosti",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_CodebookEntities_Name",
                table: "CodebookEntities",
                column: "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Codebooks_EntityTypeId",
                table: "Codebooks",
                column: "EntityTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_Codebooks_EntityTypeId_Code",
                table: "Codebooks",
                columns: new[] { "EntityTypeId", "Code" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Codebooks_EntityTypeId_IsActive",
                table: "Codebooks",
                columns: new[] { "EntityTypeId", "IsActive" });

            migrationBuilder.CreateIndex(
                name: "IX_DevOpsTasksCandidates_AktivnostId",
                table: "DevOpsTasksCandidates",
                column: "AktivnostId");

            migrationBuilder.CreateIndex(
                name: "IX_DevOpsTasksCandidates_UserId",
                table: "DevOpsTasksCandidates",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_Dokumenti_ProjekatId",
                table: "Dokumenti",
                column: "ProjekatId");

            migrationBuilder.CreateIndex(
                name: "IX_ImplementationItemCheckListItems_CheckListItemId",
                table: "ImplementationItemCheckListItems",
                column: "CheckListItemId");

            migrationBuilder.CreateIndex(
                name: "IX_ImplementationItemCheckListItems_ImplementationItemId",
                table: "ImplementationItemCheckListItems",
                column: "ImplementationItemId");

            migrationBuilder.CreateIndex(
                name: "IX_ImplementationItems_ImplementationModelId",
                table: "ImplementationItems",
                column: "ImplementationModelId");

            migrationBuilder.CreateIndex(
                name: "IX_ImplementationModels_Aktivan",
                table: "ImplementationModels",
                column: "Aktivan");

            migrationBuilder.CreateIndex(
                name: "IX_Klijenti_Naziv",
                table: "Klijenti",
                column: "Naziv");

            migrationBuilder.CreateIndex(
                name: "IX_Notes_ProjekatId",
                table: "Notes",
                column: "ProjekatId");

            migrationBuilder.CreateIndex(
                name: "IX_ProjectImplementationItemCheckLists_CheckListItemId",
                table: "ProjectImplementationItemCheckLists",
                column: "CheckListItemId");

            migrationBuilder.CreateIndex(
                name: "IX_ProjectImplementationItemCheckLists_ProjectImplementationItemId",
                table: "ProjectImplementationItemCheckLists",
                column: "ProjectImplementationItemId");

            migrationBuilder.CreateIndex(
                name: "IX_ProjectImplementationItems_ImplementationItemId",
                table: "ProjectImplementationItems",
                column: "ImplementationItemId");

            migrationBuilder.CreateIndex(
                name: "IX_ProjectImplementationItems_ImplementationModelId",
                table: "ProjectImplementationItems",
                column: "ImplementationModelId");

            migrationBuilder.CreateIndex(
                name: "IX_ProjectImplementationItems_ProjectId",
                table: "ProjectImplementationItems",
                column: "ProjectId");

            migrationBuilder.CreateIndex(
                name: "IX_ProjectPermissions_ProjekatId",
                table: "ProjectPermissions",
                column: "ProjekatId");

            migrationBuilder.CreateIndex(
                name: "IX_ProjectPermissions_UserId",
                table: "ProjectPermissions",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_Projekti_BrojProjekta",
                table: "Projekti",
                column: "BrojProjekta",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Projekti_CreatedBy",
                table: "Projekti",
                column: "CreatedBy");

            migrationBuilder.CreateIndex(
                name: "IX_Projekti_ImplementationModelId",
                table: "Projekti",
                column: "ImplementationModelId");

            migrationBuilder.CreateIndex(
                name: "IX_Projekti_KlijentId",
                table: "Projekti",
                column: "KlijentId");

            migrationBuilder.CreateIndex(
                name: "IX_Projekti_Status",
                table: "Projekti",
                column: "Status");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Codebooks");

            migrationBuilder.DropTable(
                name: "DevOpsTasksCandidates");

            migrationBuilder.DropTable(
                name: "DocumentNumbering");

            migrationBuilder.DropTable(
                name: "Dokumenti");

            migrationBuilder.DropTable(
                name: "ImplementationItemCheckListItems");

            migrationBuilder.DropTable(
                name: "Notes");

            migrationBuilder.DropTable(
                name: "ProjectImplementationItemCheckLists");

            migrationBuilder.DropTable(
                name: "ProjectPermissions");

            migrationBuilder.DropTable(
                name: "UserSettings");

            migrationBuilder.DropTable(
                name: "CodebookEntities");

            migrationBuilder.DropTable(
                name: "Aktivnosti");

            migrationBuilder.DropTable(
                name: "CheckListItems");

            migrationBuilder.DropTable(
                name: "ProjectImplementationItems");

            migrationBuilder.DropTable(
                name: "ImplementationItems");

            migrationBuilder.DropTable(
                name: "Projekti");

            migrationBuilder.DropTable(
                name: "ImplementationModels");

            migrationBuilder.DropTable(
                name: "Klijenti");

            migrationBuilder.DropTable(
                name: "Users");
        }
    }
}
