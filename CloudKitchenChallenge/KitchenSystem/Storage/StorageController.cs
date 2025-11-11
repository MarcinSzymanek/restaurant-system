using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CloudKitchenChallenge.Models;

namespace CloudKitchenChallenge.KitchenSystem.Storage
{
	// Helper class to facilitate getting storage by Temp
	public class StorageController
	{
		readonly IStorage[] storageArray;
		readonly int hotStorageIdx;
		readonly int coldStorageIdx;
		readonly int shelfIdx;

		public StorageController()
		{
			StorageBuilder builder = new StorageBuilder().
						  AddHotStorage().
						  AddColdStorage().
						  AddShelfStorage();
			storageArray = builder.Build();
		
			for (int i = 0; i < storageArray.Length; i++)
			{
				switch(storageArray[i].Temperature)
				{
					case Temp.Cold:
						coldStorageIdx = i;
						break;
					case Temp.Hot:
						hotStorageIdx = i;
						break;
					case Temp.Room:
						shelfIdx = i;
						break;
				}
			}
		}

		public IStorage GetStorage(Temp temp) =>
			temp switch
			{
				Temp.Room => storageArray[shelfIdx],
				Temp.Cold => storageArray[coldStorageIdx],
				Temp.Hot => storageArray[hotStorageIdx],
				_ => throw new ArgumentException("Invalid enum value for temp", nameof(temp))
			};

		public ShelfStorage GetShelf() => storageArray[shelfIdx] as ShelfStorage;
	}
	public class StorageBuilder
	{
		const int defaultStorageSize = 6;
		const int defaultShelfSize = 12;
		readonly List<IStorage> storageList;
		public StorageBuilder()
		{
			storageList = new List<IStorage>();
		}
		public StorageBuilder AddHotStorage(int maxStorage = defaultStorageSize)
		{
			storageList.Add(new Storage(Temp.Hot, maxStorage));
			return this;
		}
		public StorageBuilder AddColdStorage(int maxStorage = defaultStorageSize)
		{
			storageList.Add(new Storage(Temp.Cold, maxStorage));
			return this;
		}
		public StorageBuilder AddShelfStorage(int maxStorage = defaultShelfSize)
		{
			storageList.Add(new ShelfStorage(maxStorage));
			return this;
		}
		public IStorage[] Build()
		{
			return storageList.ToArray();
		}
	}

}
