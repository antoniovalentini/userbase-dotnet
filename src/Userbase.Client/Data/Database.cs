using System;
using System.Collections;
using System.Collections.Generic;

namespace Userbase.Client.Data
{
    public class Database
    {
        public class Item
        {
            public string ItemId { get; set; }
            public object Record { get; set; }
        }

        private class ItemIndex
        {
            public string ItemId { get; set; }
            public int SeqNo { get; set; }
            public int OperationIndex { get; set; }
        }

        private class ItemComparer : IComparer<ItemIndex>
        {
            public int Compare(ItemIndex a, ItemIndex b)
            {
                if (a == null) throw new ArgumentNullException(nameof(a));
                if (b == null) throw new ArgumentNullException(nameof(b));
                if (a.SeqNo < b.SeqNo || (a.SeqNo == b.SeqNo && a.OperationIndex < b.OperationIndex))
                    return -1;
                if (a.SeqNo > b.SeqNo || (a.SeqNo == b.SeqNo && a.OperationIndex > b.OperationIndex))
                    return 1;
                return 0;
            }
        }

        public Action ReceivedMessage { get; set; }
        public Action<List<Item>> OnChange { get; internal set; }
        public bool Init { get; set; }

        private readonly Queue _applyTransactionsQueue;
        private readonly Dictionary<string, Item> _items;
        private readonly SortedSet<ItemIndex> _itemsIndex;
        private int _lastSeqNo;
        private bool _init;
        private object _dbKey;
        private object[] _unverifiedTransactions;

        public Database(Action<List<Item>> changeHandler, Action receivedMessage)
        {
            //ChangeHandler = changeHandler;
            //ReceivedMessage = receivedMessage;

            OnChange = changeHandler;

            _items = new Dictionary<string, Item>();

            IComparer<ItemIndex> comparer = new ItemComparer();

            //this.itemsIndex = new SortedArray([], compareItems)
            _itemsIndex = new SortedSet<ItemIndex>(comparer);
            _unverifiedTransactions = new object[0];
            _lastSeqNo = 0;
            _init = false;
            _dbKey = null;
            ReceivedMessage = receivedMessage;

            // Queue that ensures 'ApplyTransactions' executes one at a time
            _applyTransactionsQueue = new Queue();
        }

        public List<Item> GetItems()
        {
            var result = new List<Item>();
            foreach (var itemIndex in _itemsIndex)
            {
                var itemId = itemIndex.ItemId;
                var record = _items[itemId].Record;
                result.Add(new Item {ItemId = itemId, Record = record});
            }
            return result;
        }
    }
}
