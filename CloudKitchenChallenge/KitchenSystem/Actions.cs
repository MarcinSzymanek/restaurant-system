using CloudKitchenChallenge.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace CloudKitchenChallenge.KitchenSystem
{
	namespace Actions
	{
		/// <summary>
		/// Data class for logging and serializing actions using types
		/// </summary>
		public abstract class ActionBase
		{
			readonly long timestamp_;
			readonly string orderId;

			public abstract string Action
			{
				get;
			}
			[JsonIgnore]
			public long Timestamp
			{
				get => timestamp_;
			}
			[JsonPropertyName("Timestamp")]
			[JsonInclude]
			public long TimestampUs
			{
				get => timestamp_ * 1000;
			}
			public string ID 
			{
				get => orderId;
			}
			[JsonIgnore]
			public Temp StorageParam { get; }
			protected ActionBase(long timestamp, string id, Temp storageParam)
			{
				timestamp_ = timestamp;
				orderId = id;
				StorageParam = storageParam;
			}
		}

		public class ActionFactory
		{
			public static T Create<T>(long timestamp, string id, Temp temp) where T: ActionBase
			{
				T? action =  (T)Activator.CreateInstance(typeof(T), new object[] {timestamp, id, temp});
				if(action == null)
				{
	 				throw new NullReferenceException($"Unable to create instance of type {typeof(T).Name}");
				}
				return action;
			}
		}

		public class PlaceAction : ActionBase
		{
			public override string Action { get => "place"; }
			public PlaceAction(long timestamp, string id, Temp placedIn) : base(timestamp, id, placedIn) { } 
		}

		public class MoveAction : ActionBase
		{
			public override string Action { get => "move"; }
			public MoveAction(long timestamp, string id, Temp placedIn) : base(timestamp, id, placedIn) { }
		}
		public class PickupAction : ActionBase
		{
			public override string Action { get => "pickup"; }
			public PickupAction(long timestamp, string id, Temp pickedFrom) : base(timestamp, id, pickedFrom) { }
		}
		public class DiscardAction : ActionBase
		{
			public override string Action { get => "discard"; }
			public DiscardAction(long timestamp, string id, Temp discardedFrom) : base(timestamp, id, discardedFrom) { }
		}

	}
}
