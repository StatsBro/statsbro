using System;

namespace StatsBro.Domain.Models.Exceptions
{
    public class EntityNotFoundException : Exception
    {
        public EntityNotFoundException(string msg) : base(msg)
        {
        }

        public EntityNotFoundException(string msg, Exception exc) : base(msg, exc)
        {
        }
    }
}