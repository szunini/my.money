using System;

namespace my.money.application.Assets.Commands.AddQuote
{
    public class NotFoundException : Exception
    {
        public NotFoundException(string message) : base(message) { }
    }
}
