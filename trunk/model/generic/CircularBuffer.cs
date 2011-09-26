using System;
using System.Collections.Generic;

namespace LogJoint.Generic
{
	public class CircularBuffer<T>
	{
		T[] buffer;
		int capacity;
		int count;
		int head;
		int tail;

		public CircularBuffer(int capacity)
		{
			if (capacity < 0)
				throw new ArgumentException("capacity must be greater than or equal to zero.", "capacity");

			this.capacity = capacity;
			count = 0;
			head = 0;
			tail = 0;
			buffer = new T[capacity];
		}

		public int Count
		{
			get { return count; }
		}

		public void Push(T item)
		{
			if (count == capacity)
				throw new InvalidOperationException("CircularBuffer is full");

			buffer[tail] = item;
			if (++tail == capacity)
				tail = 0;
			count++;
		}

		public T Pop()
		{
			if (count == 0)
				throw new InvalidOperationException("CircularBuffer is empty");

			T item = buffer[head];
			if (++head == capacity)
				head = 0;
			count--;
			return item;
		}
	}
}
