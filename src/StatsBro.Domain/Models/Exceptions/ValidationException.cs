using System;

namespace StatsBro.Domain.Models.Exceptions
{
    public class ValidationException : Exception
    {
        public string Property { get; private set; } = null!;

        public ValidationException()
        {
        }

        public ValidationException(string property, string message): base(message)
        {
            this.Property = property;  
        }
    }
}