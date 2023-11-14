<Query Kind="Program">
  <Connection>
    <ID>f987d055-16af-4a18-ae6b-fcacbed8667f</ID>
    <NamingServiceVersion>2</NamingServiceVersion>
    <Persist>true</Persist>
    <Server>JASDEEP-SINGH</Server>
    <AllowDateOnlyTimeOnly>true</AllowDateOnlyTimeOnly>
    <DeferDatabasePopulation>true</DeferDatabasePopulation>
    <Database>ProjectDatabase</Database>
    <DriverData>
      <LegacyMFA>false</LegacyMFA>
    </DriverData>
  </Connection>
</Query>

void Main()
{
	#region Driver
	try
	{
		//1.List all Vendors 
		//GetVendor().Dump();

		//2.Get Employees
		//GetEmployee(1).Dump();

		//3.Get Purchase order based on the vendor
		//GetVendorPurchaseOrder(1).Dump();

		//4.Fetch Stock items sold by the Vendor
		//FetchInventoryByVendor(GetVendorPurchaseOrder(1),1).Dump();

		//5.Add item to Purchase order 

		//GetVendorPurchaseOrder(1).Dump("Before ADD");
		//var itemToAdd = new ItemView
		//{
		//	StockItemID = 5578,        
		//};
		//AddItemToPurchaseOrderLine(345,itemToAdd);
		//GetVendorPurchaseOrder(1).Dump("After Add");

		//6.Remove item form PurchaseLine 

		//GetVendorPurchaseOrder(1).Dump("Before Remove");
		//RemovePurchaseOrderLine(5578);
		//GetVendorPurchaseOrder(1).Dump("After Remove");


	}
	#endregion
	#region catch all exceptions 
	catch (AggregateException ex)
	{
		foreach (var error in ex.InnerExceptions)
		{
			error.Message.Dump();
		}
	}
	catch (ArgumentNullException ex)
	{
		GetInnerException(ex).Message.Dump();
	}
	catch (Exception ex)
	{
		GetInnerException(ex).Message.Dump();
	}
	#endregion
}

private Exception GetInnerException(Exception ex)
{
	while (ex.InnerException != null)
		ex = ex.InnerException;
	return ex;
}

#region Methods
public List<VendorView> GetVendor()
{
	return Vendors
	.Select(x => new VendorView
	{
		VendorID = x.VendorID,
		VenderName = x.VendorName,
		Phone = x.Phone,
		Address = x.Address,
		City = x.City,
		Province = x.Province.Description,
		PostalCode = x.PostalCode,
	}).ToList();

}

public EmployeeView GetEmployee(int employeeID)
{
	return Employees
			.Where(e => e.EmployeeID == employeeID)
			.Select(e => new EmployeeView
			{
				EmployeeID = e.EmployeeID,
				FullName = e.FirstName + " " + e.LastName

			}).FirstOrDefault();
}

public PurchaseOrderView GetVendorPurchaseOrder(int vendorID)
{

	return PurchaseOrders
	.Where(po => po.Vendor.VendorID == vendorID)
	.Select(po => new PurchaseOrderView
	{
		Vendor = new VendorView
		{
			VendorID = po.Vendor.VendorID,
			VenderName = po.Vendor.VendorName,
			Phone = po.Vendor.Phone,
			Address = po.Vendor.Address,
			City = po.Vendor.City,
			Province = po.Vendor.Province.Description,
			PostalCode = po.Vendor.PostalCode,
		},
		PurchasingOrderID = po.PurchaseOrderID,
		Items = PurchaseOrderDetails
		.Where(i => i.PurchaseOrderID == po.PurchaseOrderID)
		.Select(i => new ItemView
		{
			StockItemID = i.StockItemID,
			Description = i.StockItem.Description,
			ROL = i.StockItem.ReOrderLevel,
			QOH = i.StockItem.QuantityOnHand,
			QOO = i.StockItem.QuantityOnOrder,
			QTO = i.StockItem.QuantityOnOrder != 0 ? 0 : Math.Abs(i.StockItem.QuantityOnHand - i.StockItem.ReOrderLevel),
			Price = i.PurchasePrice


		}).ToList(),
		GST = po.TaxAmount,
		SubTotal = po.SubTotal

	}).FirstOrDefault();
}

public List<ItemView> FetchInventoryByVendor(PurchaseOrderView purchaseOrderInfo, int vendorID)
{

	var existingStockItemIDs = purchaseOrderInfo.Items.Select(item => item.StockItemID).ToList();

	var inventoryItems = StockItems
		.Where(item => item.VendorID == vendorID && !existingStockItemIDs.Contains(item.StockItemID))
		.ToList();

	var itemViews = inventoryItems
		.Select(item => new ItemView
		{
			StockItemID = item.StockItemID,
			Description = item.Description,
			ROL = item.ReOrderLevel,
			QOH = item.QuantityOnHand,
			QOO = item.QuantityOnOrder,
			QTO = item.QuantityOnOrder != 0 ? 0 : Math.Abs(item.QuantityOnHand - item.ReOrderLevel),
			Price = item.PurchasePrice
		})
		.ToList();

	return itemViews;

}

public void AddItemToPurchaseOrderLine(int purchaseOrderID, ItemView item)
{
	var stockItemID = item.StockItemID;

	if (stockItemID == 0)
	{
		throw new Exception("Stock Id is required");
	}

	var stockItem = StockItems.FirstOrDefault(x => x.StockItemID == stockItemID);

	if (stockItem == null)
	{
		throw new Exception("Stock Item not found");
	}

	var existingPurchaseOrderDetail = PurchaseOrderDetails
		.FirstOrDefault(p => p.PurchaseOrderID == purchaseOrderID && p.StockItemID == stockItemID);

	if (existingPurchaseOrderDetail != null)
	{
		existingPurchaseOrderDetail.Quantity += 1;
	}
	else
	{
		var purchaseOrderDetail = new PurchaseOrderDetails
		{
			PurchaseOrderID = purchaseOrderID,
			StockItemID = stockItemID,
			PurchasePrice = stockItem.PurchasePrice,
		};

		PurchaseOrderDetails.Add(purchaseOrderDetail);
	}

	SaveChanges();
}

public void RemovePurchaseOrderLine(int stockItemID)
{
	if (stockItemID == 0)
	{
		throw new Exception("Item Id is required");
	}

	var purchaseOrderLine = PurchaseOrderDetails.FirstOrDefault(x => x.StockItemID == stockItemID);

	PurchaseOrderDetails.Remove(purchaseOrderLine);
	SaveChanges();

}

//public PurchaseOrderView UpdatePurchaseOrder(PurchaseOrderEditView order, bool placeOrder)
//{
//	if (order == null || order.PurchaseOrderID == 0)
//	{
//		throw new Exception("Purchase Order ID is required");
//	}
//
//	var purchaseOrder = PurchaseOrders.FirstOrDefault(x => x.PurchaseOrderID == order.PurchaseOrderID);
//
//	if (purchaseOrder == null)
//	{
//		throw new Exception("Purchase Order not found");
//	}
//
//	purchaseOrder.VendorID = order.VendorID;
//	purchaseOrder.EmployeeID = order.EmployeeID;
//	purchaseOrder.SubTotal = order.SubTotal;
//
//	foreach (var orderDetail in order.ItemDetails)
//	{
//		var purchaseOrderDetail = PurchaseOrderDetails.FirstOrDefault(x => x.PurchaseOrderDetailID == orderDetail.StockItemID);
//
//		if (purchaseOrderDetail != null)
//		{
//			purchaseOrderDetail.StockItemID = orderDetail.StockItemID;
//			purchaseOrderDetail.Quantity = orderDetail.QTO;
//			purchaseOrderDetail.Price = orderDetail.Price;
//		}
//		else
//		{
//			purchaseOrderDetail = new PurchaseOrderDetails
//			{
//				PurchaseOrderID = purchaseOrder.PurchaseOrderID,
//				StockItemID = orderDetail.StockItemID,
//				Quantity = orderDetail.QTO,
//			};
//			
//			PurchaseOrderDetails.Add(purchaseOrderDetail);
//		}
//	}
//
//	// If placeOrder is true, you can implement any additional logic here
//	if (placeOrder)
//	{
//		// Your logic for placing the order
//	}
//
//	SaveChanges();
//
//	// Return the updated purchase order (you can return a PurchaseOrderView if needed)
//	return purchaseOrder;
//}
public void DeletePurchaseOrder(int purchaseOrderID)
{
	if (purchaseOrderID == 0)
	{
		throw new Exception("Purchase Order ID is required");
	}

	var purchaseOrder = PurchaseOrders.FirstOrDefault(x => x.PurchaseOrderID == purchaseOrderID);

	if (purchaseOrder == null)
	{
		throw new Exception("Purchase Order not found");
	}

	foreach (var purchaseOrderLine in PurchaseOrderDetails.Where(x => x.PurchaseOrderID == purchaseOrderID).ToList())
	{
		PurchaseOrderDetails.Remove(purchaseOrderLine);
	}

	PurchaseOrders.Remove(purchaseOrder);
}

#endregion

#region Class/View Model   
public class VendorView
{
	public int VendorID { get; set; }
	public string VenderName { get; set; }
	public string Phone { get; set; }
	public string Address { get; set; }
	public string City { get; set; }
	public string Province { get; set; }
	public string PostalCode { get; set; }
}
public class EmployeeView
{
	public int EmployeeID { get; set; }
	public string FullName { get; set; }
}
public class PurchaseOrderView
{
	public VendorView Vendor { get; set; }
	public int PurchasingOrderID { get; set; }
	public List<ItemView> Items { get; set; }
	public decimal SubTotal { get; set; }
	public decimal GST { get; set; }
}
public class ItemView
{
	public int StockItemID { get; set; }
	public string Description { get; set; }
	public int QOH { get; set; }
	public int ROL { get; set; }
	public int QOO { get; set; }
	public int QTO { get; set; }
	public decimal Price { get; set; }

}
public class PurchaseOrderEditView
{
	public int PurchaseOrderID { get; set; }
	public int VendorID { get; set; }
	public int EmployeeID { get; set; }
	public List<ItemDetailView> ItemDetails { get; set; }
	public decimal SubTotal { get; set; }
	public decimal GST { get; set; }

}
//public class ItemDetailView
//{
//	public int StockItemID { get; set; }
//	public int QTO { get; set; }
//	public decimal Price { get; set; }
//}

public class ItemDetailView
{
	public int PurchaseOrderDetailID { get; set; } //  added Nov 14
	public int StockItemID { get; set; }
	public int QTO { get; set; }
	public decimal Price { get; set; }
	public bool RemoveFromViewFlag { get; set; } //  added Nov 14
}
#endregion