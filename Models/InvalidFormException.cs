using System;
using System.Runtime.Serialization;

namespace ThothTrainer.Models
{
    [Serializable]
    internal class InvalidFormException : Exception
    {
        public InvalidFormException()
        {
        }

        public InvalidFormException(string message) : base(message)
        {
        }

        public InvalidFormException(string message, Exception innerException) : base(message, innerException)
        {
        }

        protected InvalidFormException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }
    }
}