using Microsoft.EntityFrameworkCore.Migrations;

namespace TabFlow.Migrations.Migrations;

/// <inheritdoc />
public partial class SeedInitialData : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql(@"
            -- Categories
            INSERT INTO ""categories"" (""Id"", ""Name"", ""SortOrder"", ""IsActive"", ""DefaultStationId"")
            VALUES 
            ('00000000-0000-0000-0000-000000000001', 'Yemekler', 1, true, NULL),
            ('00000000-0000-0000-0000-000000000002', 'İçecekler', 2, true, NULL),
            ('00000000-0000-0000-0000-000000000003', 'Tatlılar', 3, true, NULL)
            ON CONFLICT DO NOTHING;

            -- Stations (Kitchen, Bar, Dessert)
            INSERT INTO ""stations"" (""Id"", ""Name"", ""Code"", ""Color"", ""Type"", ""IsActive"", ""IsFallback"", ""SortOrder"")
            VALUES 
            ('00000000-0000-0000-0000-000000000010', 'Mutfak', 'KITCHEN', '#FF6B6B', 'Kitchen', true, false, 1),
            ('00000000-0000-0000-0000-000000000011', 'Bar', 'BAR', '#4ECDC4', 'Bar', true, false, 2),
            ('00000000-0000-0000-0000-000000000012', 'Tatlı', 'DESSERT', '#95E1D3', 'Dessert', true, false, 3)
            ON CONFLICT DO NOTHING;

            -- Tables
            INSERT INTO ""tables"" (""Id"", ""Label"", ""IsActive"")
            VALUES 
            ('00000000-0000-0000-0000-000000000100', 'Masa 1', true),
            ('00000000-0000-0000-0000-000000000101', 'Masa 2', true),
            ('00000000-0000-0000-0000-000000000102', 'Masa 3', true),
            ('00000000-0000-0000-0000-000000000103', 'Masa 4', true),
            ('00000000-0000-0000-0000-000000000104', 'Masa 5', true)
            ON CONFLICT DO NOTHING;

            -- Update categories with default stations
            UPDATE ""categories"" SET ""DefaultStationId"" = '00000000-0000-0000-0000-000000000010' WHERE ""Id"" = '00000000-0000-0000-0000-000000000001' AND ""DefaultStationId"" IS NULL;
            UPDATE ""categories"" SET ""DefaultStationId"" = '00000000-0000-0000-0000-000000000011' WHERE ""Id"" = '00000000-0000-0000-0000-000000000002' AND ""DefaultStationId"" IS NULL;
            UPDATE ""categories"" SET ""DefaultStationId"" = '00000000-0000-0000-0000-000000000012' WHERE ""Id"" = '00000000-0000-0000-0000-000000000003' AND ""DefaultStationId"" IS NULL;

            -- Menu Items
            INSERT INTO ""menu_items"" (""Id"", ""CategoryId"", ""Name"", ""Description"", ""Price"", ""IsAvailable"", ""StationId"")
            VALUES 
            ('00000000-0000-0000-0000-000000000200', '00000000-0000-0000-0000-000000000001', 'Hamburger', 'Dana eti, marul, domates', 150.00, true, '00000000-0000-0000-0000-000000000010'),
            ('00000000-0000-0000-0000-000000000201', '00000000-0000-0000-0000-000000000001', 'Pizza Margarita', 'Mozzarella, domates, fesleğen', 120.00, true, '00000000-0000-0000-0000-000000000010'),
            ('00000000-0000-0000-0000-000000000202', '00000000-0000-0000-0000-000000000001', 'Tavuk Döner', 'Pilav, yoğurt, salata', 100.00, true, '00000000-0000-0000-0000-000000000010'),
            ('00000000-0000-0000-0000-000000000203', '00000000-0000-0000-0000-000000000001', 'Köfte', 'Pilav, salata', 90.00, true, '00000000-0000-0000-0000-000000000010'),
            ('00000000-0000-0000-0000-000000000204', '00000000-0000-0000-0000-000000000001', 'Lahmacun', 'Ayran, salata', 60.00, true, '00000000-0000-0000-0000-000000000010'),
            ('00000000-0000-0000-0000-000000000300', '00000000-0000-0000-0000-000000000002', 'Cola', '330ml', 25.00, true, '00000000-0000-0000-0000-000000000011'),
            ('00000000-0000-0000-0000-000000000301', '00000000-0000-0000-0000-000000000002', 'Fanta', '330ml', 25.00, true, '00000000-0000-0000-0000-000000000011'),
            ('00000000-0000-0000-0000-000000000302', '00000000-0000-0000-0000-000000000002', 'Sprite', '330ml', 25.00, true, '00000000-0000-0000-0000-000000000011'),
            ('00000000-0000-0000-0000-000000000303', '00000000-0000-0000-0000-000000000002', 'Ayran', '300ml', 15.00, true, '00000000-0000-0000-0000-000000000011'),
            ('00000000-0000-0000-0000-000000000304', '00000000-0000-0000-0000-000000000002', 'Su', '500ml', 10.00, true, '00000000-0000-0000-0000-000000000011'),
            ('00000000-0000-0000-0000-000000000400', '00000000-0000-0000-0000-000000000003', 'Cheesecake', 'Fındıklı', 45.00, true, '00000000-0000-0000-0000-000000000012'),
            ('00000000-0000-0000-0000-000000000401', '00000000-0000-0000-0000-000000000003', 'Tiramisu', 'İtalyan tatlısı', 50.00, true, '00000000-0000-0000-0000-000000000012'),
            ('00000000-0000-0000-0000-000000000402', '00000000-0000-0000-0000-000000000003', 'Baklava', 'Fıstıklı', 40.00, true, '00000000-0000-0000-0000-000000000012'),
            ('00000000-0000-0000-0000-000000000403', '00000000-0000-0000-0000-000000000003', 'Dondurma', 'Vanilyalı', 25.00, true, '00000000-0000-0000-0000-000000000012')
            ON CONFLICT DO NOTHING;
        ");
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("DELETE FROM menu_items WHERE Id LIKE '00000000-0000-0000-0000-%';");
        migrationBuilder.Sql("DELETE FROM tables WHERE Id LIKE '00000000-0000-0000-0000-%';");
        migrationBuilder.Sql("DELETE FROM stations WHERE Id LIKE '00000000-0000-0000-0000-%';");
        migrationBuilder.Sql("DELETE FROM categories WHERE Id LIKE '00000000-0000-0000-0000-%';");
    }
}
