using CloudKitchenChallenge.KitchenSystem;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;

namespace CloudKitchenChallenge.Models
{
	public enum Temp
	{
		Room,
		Hot,
		Cold
	}
	// Dataclass for receiving orders. Records come with easy json deserialization
	public record class OrderDetails
	(
		string ID,
		string Name,
		Temp Temp,
		int Freshness
	);

	// Wrapper around order details. Additionally estimates freshness at pickup time - used for sorting and discard logic
	public class Order
	{
		readonly OrderDetails details;
		readonly int shelfFreshnessAtPickup;
		
		public string ID 
		{
			get => details.ID;
		}
		public Temp Temperature
		{
			get => details.Temp;
		}
		public int ShelfFreshnessAtPickup
		{
			get => shelfFreshnessAtPickup;
		}

		public Order(OrderDetails details_, int pickupDelayMs)
		{
			details = details_;
			// If stored at a shelf, how fresh will the order be during pickup time
			shelfFreshnessAtPickup = (details.Temp == Temp.Room) ? 
				(details.Freshness * 1000) - pickupDelayMs : 
				(details.Freshness * 1000 / 2) - pickupDelayMs;
		}
	}
}
