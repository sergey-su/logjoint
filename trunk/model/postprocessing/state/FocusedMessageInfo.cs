using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace LogJoint.Postprocessing.StateInspector
{
	public class FocusedMessageInfo
	{
		readonly IMessage focusedMessage;

		public FocusedMessageInfo(IMessage focusedMessage)
		{
			this.focusedMessage = focusedMessage;
		}

		public IMessage FocusedMessage
		{
			get { return focusedMessage; }
		}

		class ListOnMessagesCollection : IList<IMessage>
		{
			IMessagesCollection collection;

			public ListOnMessagesCollection(IMessagesCollection collection)
			{
				this.collection = collection;
			}

			#region IList<IMessage> Members

			public int IndexOf(IMessage item)
			{
				throw new NotImplementedException();
			}

			public void Insert(int index, IMessage item)
			{
				throw new NotImplementedException();
			}

			public void RemoveAt(int index)
			{
				throw new NotImplementedException();
			}

			public IMessage this[int index]
			{
				get
				{
					foreach (IndexedMessage m in collection.Forward(index, index + 1))
						return m.Message;
					throw new IndexOutOfRangeException();
				}
				set
				{
					throw new NotImplementedException();
				}
			}

			#endregion

			#region ICollection<IMessage> Members

			public void Add(IMessage item)
			{
				throw new NotImplementedException();
			}

			public void Clear()
			{
				throw new NotImplementedException();
			}

			public bool Contains(IMessage item)
			{
				throw new NotImplementedException();
			}

			public void CopyTo(IMessage[] array, int arrayIndex)
			{
				throw new NotImplementedException();
			}

			public int Count
			{
				get { return collection.Count; }
			}

			public bool IsReadOnly
			{
				get { throw new NotImplementedException(); }
			}

			public bool Remove(IMessage item)
			{
				throw new NotImplementedException();
			}

			#endregion

			#region IEnumerable<IMessage> Members

			public IEnumerator<IMessage> GetEnumerator()
			{
				throw new NotImplementedException();
			}

			#endregion

			#region IEnumerable Members

			System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
			{
				throw new NotImplementedException();
			}

			#endregion
		};
	}
}
