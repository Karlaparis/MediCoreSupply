using MediCoreSupply.Api.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace MediCoreSupply.Api.Data;

public static class DbSeeder
{
    public static async Task SeedAsync(MediCoreSupplyDbContext context)
    {
        if (await context.Categories.AnyAsync())
            return;

        var ppe = new Category { Name = "Personal Protective Equipment", Description = "Gloves, masks, gowns, and other protective gear" };
        var woundCare = new Category { Name = "Wound Care", Description = "Dressings, bandages, and wound treatment supplies" };
        var diagnostics = new Category { Name = "Diagnostic Equipment", Description = "Devices used for patient diagnostics" };
        var pharma = new Category { Name = "Pharmaceuticals", Description = "Prescription and over-the-counter medications" };

        context.Categories.AddRange(ppe, woundCare, diagnostics, pharma);

        var nitrileGloves = new Product
        {
            Sku = "PPE-GLV-100",
            Name = "Nitrile Exam Gloves (Box of 100)",
            UnitOfMeasure = "Box",
            UnitPrice = 8.99m,
            Category = ppe
        };
        var surgicalMasks = new Product
        {
            Sku = "PPE-MSK-050",
            Name = "Surgical Masks (Box of 50)",
            UnitOfMeasure = "Box",
            UnitPrice = 6.50m,
            Category = ppe
        };
        var isolationGowns = new Product
        {
            Sku = "PPE-GWN-010",
            Name = "Disposable Isolation Gowns (Pack of 10)",
            UnitOfMeasure = "Pack",
            UnitPrice = 24.00m,
            Category = ppe
        };
        var gauzePads = new Product
        {
            Sku = "WC-GAUZE-025",
            Name = "Sterile Gauze Pads (Pack of 25)",
            UnitOfMeasure = "Pack",
            UnitPrice = 5.25m,
            Category = woundCare
        };
        var adhesiveBandages = new Product
        {
            Sku = "WC-BAND-100",
            Name = "Adhesive Bandages (Box of 100)",
            UnitOfMeasure = "Box",
            UnitPrice = 3.75m,
            Category = woundCare
        };
        var bpMonitor = new Product
        {
            Sku = "DX-BPM-001",
            Name = "Digital Blood Pressure Monitor",
            UnitOfMeasure = "Each",
            UnitPrice = 45.00m,
            Category = diagnostics
        };
        var thermometer = new Product
        {
            Sku = "DX-THM-001",
            Name = "Infrared Forehead Thermometer",
            UnitOfMeasure = "Each",
            UnitPrice = 29.99m,
            Category = diagnostics
        };
        var amoxicillin = new Product
        {
            Sku = "RX-AMOX-500",
            Name = "Amoxicillin 500mg (Bottle of 30)",
            UnitOfMeasure = "Bottle",
            UnitPrice = 18.40m,
            RequiresPrescription = true,
            Category = pharma
        };

        context.Products.AddRange(
            nitrileGloves, surgicalMasks, isolationGowns,
            gauzePads, adhesiveBandages,
            bpMonitor, thermometer,
            amoxicillin);

        var eastWarehouse = new Warehouse { Name = "Central Distribution Center", Address = "100 Supply Chain Way, Newark, NJ 07102" };
        var westWarehouse = new Warehouse { Name = "West Coast Hub", Address = "450 Logistics Pkwy, Reno, NV 89502" };

        context.Warehouses.AddRange(eastWarehouse, westWarehouse);

        context.InventoryItems.AddRange(
            new InventoryItem { Product = nitrileGloves, Warehouse = eastWarehouse, QuantityOnHand = 500, ReorderLevel = 100, ReorderQuantity = 300 },
            new InventoryItem { Product = surgicalMasks, Warehouse = eastWarehouse, QuantityOnHand = 300, ReorderLevel = 75, ReorderQuantity = 200 },
            new InventoryItem { Product = isolationGowns, Warehouse = eastWarehouse, QuantityOnHand = 120, ReorderLevel = 30, ReorderQuantity = 100 },
            new InventoryItem { Product = gauzePads, Warehouse = eastWarehouse, QuantityOnHand = 200, ReorderLevel = 50, ReorderQuantity = 150 },
            new InventoryItem { Product = amoxicillin, Warehouse = eastWarehouse, QuantityOnHand = 80, ReorderLevel = 20, ReorderQuantity = 60 },
            new InventoryItem { Product = nitrileGloves, Warehouse = westWarehouse, QuantityOnHand = 250, ReorderLevel = 100, ReorderQuantity = 300 },
            new InventoryItem { Product = adhesiveBandages, Warehouse = westWarehouse, QuantityOnHand = 400, ReorderLevel = 100, ReorderQuantity = 250 },
            new InventoryItem { Product = bpMonitor, Warehouse = westWarehouse, QuantityOnHand = 40, ReorderLevel = 10, ReorderQuantity = 25 },
            new InventoryItem { Product = thermometer, Warehouse = westWarehouse, QuantityOnHand = 60, ReorderLevel = 15, ReorderQuantity = 40 });

        var generalHospital = new Customer
        {
            Name = "Riverside General Hospital",
            Type = CustomerType.Hospital,
            ContactName = "Dana Whitfield",
            Email = "procurement@riversidegeneral.example",
            Phone = "555-0142",
            Address = "12 Riverside Ave, Newark, NJ 07103"
        };
        var familyClinic = new Customer
        {
            Name = "Oakwood Family Clinic",
            Type = CustomerType.Clinic,
            ContactName = "Marcus Lee",
            Email = "orders@oakwoodclinic.example",
            Phone = "555-0198",
            Address = "78 Oakwood Blvd, Reno, NV 89501"
        };
        var mainStreetPharmacy = new Customer
        {
            Name = "Main Street Pharmacy",
            Type = CustomerType.Pharmacy,
            ContactName = "Priya Nair",
            Email = "purchasing@mainstreetrx.example",
            Phone = "555-0173",
            Address = "9 Main St, Newark, NJ 07104"
        };

        context.Customers.AddRange(generalHospital, familyClinic, mainStreetPharmacy);

        var order1 = new Order
        {
            OrderNumber = "ORD-1001",
            OrderDate = DateTime.UtcNow.AddDays(-5),
            Status = OrderStatus.Delivered,
            ShippingAddress = generalHospital.Address,
            Customer = generalHospital
        };
        order1.OrderItems.Add(new OrderItem { Product = nitrileGloves, Quantity = 20, UnitPrice = nitrileGloves.UnitPrice });
        order1.OrderItems.Add(new OrderItem { Product = surgicalMasks, Quantity = 15, UnitPrice = surgicalMasks.UnitPrice });

        var order2 = new Order
        {
            OrderNumber = "ORD-1002",
            OrderDate = DateTime.UtcNow.AddDays(-2),
            Status = OrderStatus.Shipped,
            ShippingAddress = familyClinic.Address,
            Customer = familyClinic
        };
        order2.OrderItems.Add(new OrderItem { Product = bpMonitor, Quantity = 3, UnitPrice = bpMonitor.UnitPrice });
        order2.OrderItems.Add(new OrderItem { Product = thermometer, Quantity = 5, UnitPrice = thermometer.UnitPrice });

        var order3 = new Order
        {
            OrderNumber = "ORD-1003",
            OrderDate = DateTime.UtcNow,
            Status = OrderStatus.Pending,
            ShippingAddress = mainStreetPharmacy.Address,
            Customer = mainStreetPharmacy
        };
        order3.OrderItems.Add(new OrderItem { Product = amoxicillin, Quantity = 10, UnitPrice = amoxicillin.UnitPrice });

        context.Orders.AddRange(order1, order2, order3);

        await context.SaveChangesAsync();
    }
}
