using System;
using System.Collections.Generic;

namespace FacebookLogic
{
    internal class Filter<T>
    {
        internal event Func<T, bool> FuncFilter;

        internal Filter(Func<T, bool> i_FuncFilter)
        {
            FuncFilter = i_FuncFilter;
        }

        internal IEnumerable<T> DoFilter(IEnumerable<T> i_Enumberable)
        {
            foreach (T obj in i_Enumberable)
            {
                if (FuncFilter.Invoke(obj))
                {
                    yield return obj;
                }
            }
        }
    }
}
