using System;
using System.Collections.Generic;
using System.Text;

namespace my.money.domain.Common.Primitives
{
    public abstract record DomainEvent(DateTime OccurredOnUtc);
}
