using System;

namespace StatsBro.Domain.Exceptions
{
    public class PaymentException : Exception
    {
        public PaymentException() : base() { }

        public PaymentException(string message) : base(message) { }
    }

    public class PaymentNotificationException : Exception
    {
        public PaymentNotificationException() : base() { }

        public PaymentNotificationException(string message) : base(message) { }
    }
}

