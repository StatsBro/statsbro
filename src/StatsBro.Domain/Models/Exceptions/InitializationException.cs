using System;

namespace StatsBro.Domain.Models.Exceptions
{
    public class InitializationException : Exception
    {
        public InitializationException(string message) : base(message) { }
    }
}

