using System;

namespace Foxy.Testing.EntityFrameworkCore
{
    /// <summary>
    /// The exception that is thrown when something went wrong in the <see cref="TestDbContextFactory{T}"/>.
    /// </summary>
    [Serializable]
    public class TestDbContextFactoryException : Exception
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="TestDbContextFactoryException"/> class.
        /// </summary>
        public TestDbContextFactoryException() { }

        /// <summary>
        /// Initializes a new instance of the <see cref="TestDbContextFactoryException"/> class.
        /// </summary>
        public TestDbContextFactoryException(string message) : base(message) { }

        /// <summary>
        /// Initializes a new instance of the <see cref="TestDbContextFactoryException"/> class.
        /// </summary>
        public TestDbContextFactoryException(string message, Exception inner) : base(message, inner) { }

        /// <summary>
        /// Initializes a new instance of the <see cref="TestDbContextFactoryException"/> class.
        /// </summary>
        protected TestDbContextFactoryException(
          System.Runtime.Serialization.SerializationInfo info,
          System.Runtime.Serialization.StreamingContext context) : base(info, context) { }
    }
}
