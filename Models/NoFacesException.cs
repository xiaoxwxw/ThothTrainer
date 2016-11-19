using System;
using System.Runtime.Serialization;

namespace ThothTrainer.Models
{
    [Serializable]
    internal class NoFacesException : Exception
    {
        public NoFacesException()
        {

        }

        public NoFacesException(string message) : base(message)
        {

        }
        

        public NoFacesException(string message, Exception innerException) : base(message, innerException)
        {

        }

        protected NoFacesException(SerializationInfo info, StreamingContext context) : base(info, context)
        {

        }
    }
}
