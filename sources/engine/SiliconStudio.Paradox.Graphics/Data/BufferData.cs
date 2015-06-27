// Copyright (c) 2014 Silicon Studio Corp. (http://siliconstudio.co.jp)
// This file is distributed under GPL v3. See LICENSE.md for details.
using SiliconStudio.Core;
using SiliconStudio.Core.Serialization.Contents;

namespace SiliconStudio.Paradox.Graphics.Data
{
    /// <summary>
    /// Content of a GPU buffer (vertex buffer, index buffer, etc...).
    /// </summary>
    [DataContract]
    [ContentSerializer(typeof(DataContentSerializerWithReuse<BufferData>))]
    public class BufferData
    {
        public BufferData()
        {
        }

        public BufferData(BufferFlags bufferFlags, byte[] content)
        {
            Content = content;
            BufferFlags = bufferFlags;
        }

        /// <summary>
        /// Gets or sets the buffer content.
        /// </summary>
        /// <value>
        /// The buffer content.
        /// </value>
        public byte[] Content { get; set; }

        /// <summary>
        /// Buffer flags describing the type of buffer.
        /// </summary>
        public BufferFlags BufferFlags { get; set; }

        /// <summary>
        /// Usage of this buffer.
        /// </summary>
        public GraphicsResourceUsage Usage { get; set; }

        /// <summary>
        /// The size of the structure (in bytes) when it represents a structured/typed buffer.
        /// </summary>
        public int StructureByteStride { get; set; }

        /// <summary>
        /// Creates a new instance of <see cref="BufferData"/> from a typed buffer.
        /// </summary>
        /// <typeparam name="T">Type of the element to store in the buffer data.</typeparam>
        /// <param name="bufferFlags">The flags indicating the type of buffer</param>
        /// <param name="content">An array of data</param>
        /// <returns>A buffer data.</returns>
        public static BufferData New<T>(BufferFlags bufferFlags, T[] content) where T : struct
        {
            var sizeOf = Utilities.SizeOf(content);
            var buffer = new byte[sizeOf];
            Utilities.Write(buffer, content, 0, content.Length);
            return new BufferData(bufferFlags, buffer);
        }
    }
}