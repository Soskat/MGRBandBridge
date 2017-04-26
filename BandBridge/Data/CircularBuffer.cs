using System.Text;

namespace BandBridge.Data
{
    /// <summary>
    /// Circular buffer. In case of the end of free space, old data will be ovewritten.
    /// </summary>
    public class CircularBuffer
    {
        #region Fields
        /// <summary>
        /// Table represents the buffer.
        /// </summary>
        private int[] buffer;

        /// <summary>
        /// The index of currently active buffer element.
        /// </summary>
        private int iterator;
        #endregion

        #region Constructors
        /// <summary>
        /// Creates a new instance of class <see cref="CircularBuffer"/>.
        /// </summary>
        /// <param name="capacity">Capacity of the buffer</param>
        public CircularBuffer(int capacity)
        {
            buffer = new int[capacity];
            iterator = 0;
        }
        #endregion

        #region Methods
        /// <summary>
        /// Adds new value to the buffer.
        /// </summary>
        /// <param name="obj">Value to add</param>
        public void Add(int obj)
        {
            buffer[iterator] = obj;
            iterator = ++iterator % buffer.Length;
        }

        /// <summary>
        /// Returns the average of values currently stored in buffer.
        /// </summary>
        /// <returns>The average of values stored in buffer</returns>
        public int GetAverage()
        {
            int sum = 0, itemsCount = 0;
            foreach (int obj in buffer)
            {
                if(obj > 0)
                {
                    sum += obj;
                    itemsCount++;
                }
            }
            return sum / itemsCount;
        }

        /// <summary>
        /// Writes all buffer elements in form of: [a1 | a2 | ... an | ]
        /// </summary>
        /// <returns>CircularBuffer in string format</returns>
        public override string ToString()
        {
            StringBuilder sb = new StringBuilder("[ ");
            foreach(int item in buffer)
            {
                sb.Append(item + " | ");
            }
            sb.Append("]");
            return sb.ToString();
        }
        #endregion
    }
}
